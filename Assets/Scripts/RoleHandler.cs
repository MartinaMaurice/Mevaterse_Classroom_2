using UnityEngine;
using TMPro;
using Photon.Pun;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.UI;

public class RoleHandler : MonoBehaviour
{
    public TMP_InputField playerNameInput;
    public TMP_InputField roomNameInput;
    public TMP_InputField userIdInput; // User ID input field for validation
    public TMP_InputField courseIdInput; // Course ID input field for validation

    public GameObject initialUI;

    public GameObject debuggingPanel;
    public GameObject toolkitButton;
    public GameObject lectureSelector; // Reference to the LectureSelector GameObject

    private FirebaseFirestore db;
    private string userRole; // To store the role (Instructor or Student)
    private string userId;   // Store the validated userId

    void Start()
    {
        debuggingPanel.SetActive(false);
        lectureSelector.SetActive(false); // Default state is inactive
        toolkitButton.SetActive(true); // Always visible

        db = FirebaseFirestore.DefaultInstance;
    }

    // Called when the "Connect" button is clicked
    public void OnConnectClicked()
    {
        string playerName = playerNameInput.text;
        string roomName = roomNameInput.text;
        userId = userIdInput.text;
        string courseId = courseIdInput.text;

        PhotonNetwork.NickName = playerName;

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("User ID cannot be empty.");
            return;
        }

        if (PhotonNetwork.IsConnected)
        {
            ValidateUserRole(userId, isValid =>
            {
                if (isValid)
                {
                    ValidateUserAccess(userId, courseId, hasAccess =>
                    {
                        if (hasAccess)
                        {
                            Debug.Log($"User with role {userRole} validated for the course.");

                            // Notify LeaderboardManager about the role
                            LeaderboardManager leaderboardManager = FindObjectOfType<LeaderboardManager>();
                            if (leaderboardManager != null)
                            {
                                leaderboardManager.SetUserRole(userRole);
                            }

                            // Enter the room with the user's role
                            EnterRoomWithRole(roomName);
                        }
                        else
                        {
                            Debug.LogError("User does not have access to the course.");
                        }
                    });
                }
                else
                {
                    Debug.LogError("Invalid user ID. Cannot determine role.");
                }
            });
        }
        else
        {
            Debug.LogError("Not connected to Photon. Please wait...");
        }
    }

    // Validate the user's role based on their userId
    private void ValidateUserRole(string userId, System.Action<bool> callback)
    {
        db.Collection("users").Document(userId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists && snapshot.ContainsField("role"))
                {
                    userRole = snapshot.GetValue<string>("role");
                    callback(userRole == "Instructor" || userRole == "Student");
                }
                else
                {
                    Debug.LogError("User ID not found or role missing in the database.");
                    callback(false);
                }
            }
            else
            {
                Debug.LogError("Failed to validate user role: " + task.Exception);
                callback(false);
            }
        });
    }

    // Check if the user has access to a specific course
    private void ValidateUserAccess(string userId, string courseId, System.Action<bool> callback)
    {
        db.Collection("users").Document(userId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists && snapshot.ContainsField("course_id"))
                {
                    string storedCourseId = snapshot.GetValue<string>("course_id");
                    callback(string.IsNullOrEmpty(courseId) || storedCourseId == courseId);
                }
                else
                {
                    callback(false);
                }
            }
            else
            {
                Debug.LogError("Failed to validate user access: " + task.Exception);
                callback(false);
            }
        });
    }

    public void EnterRoomWithRole(string roomName)
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
