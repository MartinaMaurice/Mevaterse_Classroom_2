using UnityEngine;
using TMPro;
using Photon.Pun;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.UI;

public class RoleHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField userIdInput;
    [SerializeField] private TMP_InputField courseIdInput;
    [SerializeField] private GameObject initialUI;
    [SerializeField] private GameObject debuggingPanel;
    [SerializeField] private GameObject toolkitButton;
    [SerializeField] private GameObject lectureSelector;

    private FirebaseFirestore db;
    private string userRole;

    void Start()
    {
        if (debuggingPanel == null || toolkitButton == null || lectureSelector == null)
        {
            Debug.LogError("UI references are not assigned in the Inspector.");
            return;
        }

        debuggingPanel.SetActive(false);
        lectureSelector.SetActive(false);
        toolkitButton.SetActive(true);

        db = FirebaseFirestore.DefaultInstance;
    }

    public void OnConnectClicked()
    {
        string playerName = playerNameInput.text;
        string roomName = roomNameInput.text;
        string userId = userIdInput.text;
        string courseId = courseIdInput.text;

        if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Player name or User ID cannot be empty.");
            return;
        }

        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("Not connected to Photon. Please wait...");
            return;
        }

        PhotonNetwork.NickName = playerName;

        ValidateUserRole(userId, isValid =>
        {
            if (!isValid)
            {
                Debug.LogError("Invalid user ID. Cannot determine role.");
                return;
            }

            ValidateUserAccess(userId, courseId, hasAccess =>
            {
                if (!hasAccess)
                {
                    Debug.LogError("User does not have access to the course.");
                    return;
                }

                Debug.Log($"User validated with role {userRole}.");
                EnterRoomWithRole(roomName);
            });
        });
    }

    private void ValidateUserRole(string userId, System.Action<bool> callback)
    {
        db.Collection("users").Document(userId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Failed to validate user role: " + task.Exception);
                callback(false);
                return;
            }

            if (task.Result.Exists && task.Result.ContainsField("role"))
            {
                userRole = task.Result.GetValue<string>("role");
                callback(userRole == "Instructor" || userRole == "Student");
            }
            else
            {
                Debug.LogError("User ID not found or role missing in the database.");
                callback(false);
            }
        });
    }

    private void ValidateUserAccess(string userId, string courseId, System.Action<bool> callback)
    {
        db.Collection("users").Document(userId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Failed to validate user access: " + task.Exception);
                callback(false);
                return;
            }

            if (task.Result.Exists && task.Result.ContainsField("course_id"))
            {
                string storedCourseId = task.Result.GetValue<string>("course_id");
                callback(string.IsNullOrEmpty(courseId) || storedCourseId == courseId);
            }
            else
            {
                callback(false);
            }
        });
    }

    private void EnterRoomWithRole(string roomName)
    {
        if (userRole == "Instructor")
        {
            Debug.Log("Instructor joining the room.");
            lectureSelector.SetActive(true);
        }
        else if (userRole == "Student")
        {
            Debug.Log("Student joining the room.");
            lectureSelector.SetActive(false);
        }

        FindObjectOfType<ConnectToServer>().JoinRoom(roomName);
    }
}
