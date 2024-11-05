using UnityEngine;
using TMPro;  // For handling TextMeshPro inputs.
using Photon.Pun;  // Include Photon namespace to access PhotonNetwork.
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

public class RoleHandler : MonoBehaviour
{
    public TMP_Dropdown roleDropdown;
    public TMP_InputField playerNameInput;
    public TMP_InputField roomNameInput;

    public GameObject connectButton;
    public GameObject courseCreationUI;
    public GameObject initialUI;

    public GameObject debuggingPanel;

    public GameObject toolkitButton;

    private FirebaseFirestore db;
    private string selectedRole;
    private string userId;

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

    // This function is called when the "Select Role" button is clicked
    public void OnSelectRoleClicked()
    {
        string playerName = playerNameInput.text;

        // Ensure that player name and role are not empty
        if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(selectedRole))
        {
            Debug.LogError("Player name and role must be selected.");
            return;
        }

        // Save the user to Firestore when the "Select Role" button is clicked
        userId = db.Collection("users").Document().Id;
        SaveUserToFirestore(playerName, selectedRole);
    }

    public void OnConnectClicked()
    {
        string playerName = playerNameInput.text;
        string roomName = roomNameInput.text;

        PhotonNetwork.NickName = playerName;

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
                // Check if the course exists in Firestore before attempting to join
                CheckCourseExistsInFirestore(roomName, exists =>
                {
                    if (exists)
                    {
                        Debug.Log($"Course with ID {roomName} found. Attempting to join room.");
                        FindObjectOfType<ConnectToServer>().JoinRoom(roomName);
                    }
                    else
                    {
                        Debug.LogError("Course not found in Firestore. Please check the Course ID.");
                    }
                });
            }
            else
            {
                Debug.LogError("Not connected to Photon. Please wait...");
            }
        }
    }
    private void CheckCourseExistsInFirestore(string courseId, System.Action<bool> callback)
    {
        Debug.Log($"Attempting to find course with ID: {courseId}");

        db.Collection("Courses").Document(courseId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    Debug.Log("Course found in Firestore.");
                    callback(true); // Course exists
                }
                else
                {
                    Debug.LogWarning("Course does not exist in Firestore.");
                    callback(false); // Course does not exist
                }
            }
            else
            {
                Debug.LogError("Failed to check course in Firestore: " + task.Exception);
                callback(false);
            }
        });
    }


    // Function to save the user information to Firestore
    void SaveUserToFirestore(string name, string role)
    {
        Dictionary<string, object> userDoc = new Dictionary<string, object>
        {
            { "name", name },
            { "role", role },
            { "id", userId }
        };

        db.Collection("users").Document(userId).SetAsync(userDoc).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("User successfully added to Firestore.");
                PlayerPrefs.SetString("UserID", userId); // Save user ID locally for future use
            }
            else
            {
                Debug.LogError("Error adding user: " + task.Exception);
            }
        });
    }
}
