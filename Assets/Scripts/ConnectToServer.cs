using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;
using System.Collections.Generic;


public static class SerializableColor
{
    public static byte[] Serialize(object obj)
    {
        Color32 color = (Color32)obj;
        return new byte[] { color.r, color.g, color.b, color.a };
    }

    public static object Deserialize(byte[] bytes)
    {
        return new Color32(bytes[0], bytes[1], bytes[2], bytes[3]);
    }
}

public class ConnectToServer : MonoBehaviourPunCallbacks
{
    public TMP_InputField nameInputField;
    public TMP_InputField roomNameInputField;
    public TMP_Dropdown roleDropdown;

    public GameObject initialGUI;
    public GameObject loggedGUI;
    public GameObject courseCreationUI;


    public GameObject errorMessage;

    public Button hostButton;
    public Button clientButton;
    public Button quitButton;
    public Button leaveButton;
    public Button editButton;

    public AudioSource buttonClick;

    public PhotonView player;
    private string selectedRole;
    private bool isRejoining = false;
    private CourseManager courseManager = new CourseManager();  // Initialize CourseManager



    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Already connected to Photon. Joining Lobby...");
            PhotonNetwork.JoinLobby();  // Ensure you are always in the lobby
        }
        else
        {
            Debug.Log("Connecting to Photon...");
            PhotonNetwork.ConnectUsingSettings();
        }

        loggedGUI.SetActive(false);
        errorMessage.SetActive(false);

        roleDropdown.onValueChanged.AddListener(delegate { OnRoleSelected(); });
        clientButton.onClick.AddListener(ConnectWithRole);
        hostButton.onClick.AddListener(CreateCourseRoom);
        quitButton.onClick.AddListener(() => Application.Quit());
        leaveButton.onClick.AddListener(() => { buttonClick.Play(); LeaveRoom(); });
        editButton.onClick.AddListener(() => SceneManager.LoadScene("CharacterEditor"));
    }
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server");
        PhotonNetwork.JoinLobby();  // Ensure we are in the lobby to see available rooms
        hostButton.enabled = true;
        clientButton.enabled = true;
    }

    private void OnRoleSelected()
    {
        selectedRole = roleDropdown.options[roleDropdown.value].text;
        Debug.Log($"Selected Role: {selectedRole}");
    }

    private void ConnectWithRole()
    {
        string playerName = nameInputField.text;
        string roomID = roomNameInputField.text;

        if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(roomID))
        {
            ShowErrorMessage("Player Name and Room ID are required.");
            return;
        }

        PhotonNetwork.NickName = playerName;

        if (selectedRole == "Instructor")
        {
            courseCreationUI.SetActive(true);
        }
        else if (selectedRole == "Student")
        {
            Debug.Log($"Attempting to join room with ID: {roomID}");

            // Ensure we are connected to the lobby to access available rooms
            if (PhotonNetwork.InLobby)
            {
                if (courseManager.CourseExists(roomID))
                {
                    PhotonNetwork.JoinRoom(roomID);
                }
                else
                {
                    ShowErrorMessage("Course not found. Please check the Room ID.");
                }
            }
            else
            {
                Debug.LogWarning("Not in lobby. Joining lobby...");
                PhotonNetwork.JoinLobby();  // Ensure we join the lobby before attempting to join a room
            }
        }
    }


    public override void OnJoinedRoom()
    {
        Debug.Log($"Successfully joined room: {PhotonNetwork.CurrentRoom.Name}");

        Hashtable roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
        if (roomProperties.TryGetValue("CourseName", out object courseName) &&
            roomProperties.TryGetValue("CourseID", out object courseId))
        {
            Debug.Log($"Joined Course: {courseName} (ID: {courseId})");
        }
        else
        {
            Debug.LogWarning("No course data found in the room.");
        }

        initialGUI.SetActive(false);
        loggedGUI.SetActive(true);

        GameObject myPlayer = PhotonNetwork.Instantiate(player.name, Vector3.zero, Quaternion.identity);
        myPlayer.GetComponent<PlayerController>().SetRole(selectedRole);

        Transform playerCam = GameObject.FindWithTag("MainCamera")?.transform;
        if (playerCam != null)
        {
            CameraController followScript = playerCam.GetComponent<CameraController>();
            if (followScript != null)
            {
                followScript.target = myPlayer;
            }
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Failed to join room. Return Code: {returnCode}, Message: {message}");
        ShowErrorMessage("Room not found. Please check the Room ID.");
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"Room created: {PhotonNetwork.CurrentRoom.Name}");
    }


    private void CreateCourseRoom()
    {
        string courseName = nameInputField.text;
        string courseID = roomNameInputField.text;

        if (string.IsNullOrEmpty(courseName) || string.IsNullOrEmpty(courseID))
        {
            ShowErrorMessage("Course Name and Course ID are required.");
            return;
        }
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 20,
            IsVisible = true,  // Keep room visible in the lobby
            IsOpen = true,  // Allow others to join the room
            CleanupCacheOnLeave = false,  // Keep the room available even if empty
            CustomRoomProperties = new Hashtable { { "CourseName", courseName }, { "CourseID", courseID } },
            CustomRoomPropertiesForLobby = new string[] { "CourseName", "CourseID" }
        };

        PhotonNetwork.CreateRoom(courseID, roomOptions);
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left the room.");
        ResetUI();
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();  // Leave the room properly
            isRejoining = true;  // Set flag to rejoin lobby after leaving
        }
        else
        {
            ResetUI();
        }
    }

    private void ResetUI()
    {
        initialGUI.SetActive(true);
        loggedGUI.SetActive(false);

        nameInputField.text = "";
        roomNameInputField.text = "";
        errorMessage.SetActive(false);
        LogAllCourses();  // Log all saved courses when resetting UI

    }
    private void LogAllCourses()
    {
        List<Course> courses = courseManager.LoadAllCourses();
        if (courses.Count == 0)
        {
            Debug.Log("No saved courses found.");
        }
        else
        {
            Debug.Log("Logging all saved courses:");
            foreach (Course course in courses)
            {
                Debug.Log($"Course Name: {course.CourseName}, ID: {course.CourseID}");
            }
        }
    }


    private void ShowErrorMessage(string message)
    {
        errorMessage.SetActive(true);
        errorMessage.GetComponent<TMP_Text>().text = message;
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"{otherPlayer.NickName} left the room.");
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"{newMasterClient.NickName} is now the master client.");
    }

    private IEnumerator<object> WaitForConnectionAndCreateRoom()
    {
        while (!PhotonNetwork.IsConnectedAndReady)
        {
            yield return null;  // Wait until connected and ready
        }

        CreateCourseRoom();  // Now safe to create the room
    }


    private void OnHostButtonClicked()
    {
        StartCoroutine(WaitForConnectionAndCreateRoom());
    }

}