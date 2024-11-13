using UnityEngine;
using TMPro;
using Photon.Pun;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

public class RoleHandler : MonoBehaviour
{
    public TMP_Dropdown roleDropdown;
    public TMP_InputField playerNameInput;
    public TMP_InputField roomNameInput;
    public TMP_InputField userIdInput;      // User ID input field for validation
    public TMP_InputField courseIdInput;    // Course ID input field for validation

    public GameObject connectButton;
    public GameObject courseCreationUI;
    public GameObject initialUI;

    public GameObject debuggingPanel;
    public GameObject toolkitButton;

    private FirebaseFirestore db;
    private string selectedRole;

    void Start()
    {
        courseCreationUI.SetActive(false);
        toolkitButton.SetActive(false); 
        debuggingPanel.SetActive(false);  
        roleDropdown.value = 0;  // Assuming 0 is Instructor and 1 is Student
        selectedRole = "Student";  // Default to student

        db = FirebaseFirestore.DefaultInstance;

        // Role selection will only update the selectedRole variable, no saving
        roleDropdown.onValueChanged.AddListener(delegate { OnRoleSelected(); });
    }

    public void OnRoleSelected()
    {
        // Update the selectedRole variable based on dropdown selection
        selectedRole = roleDropdown.options[roleDropdown.value].text;

        // Show or hide the toolkit button based on the role selected
        if (selectedRole == "Instructor")
        {
            toolkitButton.SetActive(true);
        }
        else
        {
            toolkitButton.SetActive(false);
        }
    }

    // This function is called when the "Connect" button is clicked
    public void OnConnectClicked()
    {
        string playerName = playerNameInput.text;
        string roomName = roomNameInput.text;
        string userId = userIdInput.text;
        string courseId = courseIdInput.text;

        PhotonNetwork.NickName = playerName;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(courseId))
        {
            Debug.LogError("User ID and Course ID cannot be empty.");
            return;
        }

        if (selectedRole == "Instructor")
        {
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
                // Validate if the student has access to the specified course
                ValidateUserAccess(userId, courseId, isValid =>
                {
                    if (isValid)
                    {
                        Debug.Log("User validated for the course. Proceeding to connect.");
                        // Attempt to join the specified room
                        FindObjectOfType<ConnectToServer>().JoinRoom(roomName);
                    }
                    else
                    {
                        Debug.LogError("User does not have access to the course.");
                    }
                });
            }
            else
            {
                Debug.LogError("Not connected to Photon. Please wait...");
            }
        }
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
                    callback(storedCourseId == courseId);
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
}
