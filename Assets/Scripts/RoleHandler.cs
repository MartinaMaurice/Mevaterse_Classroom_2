using UnityEngine;
using TMPro;
using Photon.Pun;

public class RoleHandler : MonoBehaviour
{
    public TMP_Dropdown roleDropdown;
    public TMP_InputField playerNameInput;
    public TMP_InputField roomNameInput;
    public GameObject connectButton;

    public GameObject courseCreationUI;  // Reference to the Course Creation UI
    public GameObject initialUI;  // Reference to the initial UI
    private string selectedRole;

    void Start()
    {
        courseCreationUI.SetActive(false);

        // Setup listener for role selection.
        roleDropdown.onValueChanged.AddListener(delegate { OnRoleSelected(); });
    }

    public void OnRoleSelected()
    {
        selectedRole = roleDropdown.options[roleDropdown.value].text;
        Debug.Log($"Selected role: {selectedRole}");
    }

    public void OnConnectClicked()
    {
        string playerName = playerNameInput.text;
        string roomName = roomNameInput.text;

        PhotonNetwork.NickName = playerName;

        if (selectedRole == "Instructor")
        {
            // Instructors go to course creation UI
            if (PhotonNetwork.IsConnected)
            {
                courseCreationUI.SetActive(true);
                initialUI.SetActive(false);
            }
            else
            {
                Debug.LogError("Not connected to Photon. Please wait...");
            }
        }
        else if (selectedRole == "Student")
        {
            if (PhotonNetwork.IsConnected)
            {
                Debug.Log($"Attempting to join room with ID: {roomName}");
                FindObjectOfType<ConnectToServer>().JoinRoom(roomName);
            }
            else
            {
                Debug.LogError("Not connected to Photon. Please wait...");
            }
        }
    }
}
