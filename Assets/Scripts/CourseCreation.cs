using UnityEngine;
using TMPro;
using Photon.Pun;
using ExitGames.Client.Photon;  // Required for custom properties
using Photon.Realtime;

public class CourseCreation : MonoBehaviour
{
    public TMP_InputField courseNameInput;
    public TMP_InputField courseIdInput;
    public GameObject courseCreationUI;
    public GameObject initialGUI;
    private FirestoreManager firestoreManager;
    private CourseManager courseManager = new CourseManager();  // Initialize CourseManager

    void Start()
    {
        firestoreManager = FindObjectOfType<FirestoreManager>();
    }

    public void OnHostButtonClicked()
    {
        string courseName = courseNameInput.text;
        string courseId = courseIdInput.text;

        if (string.IsNullOrEmpty(courseName) || string.IsNullOrEmpty(courseId))
        {
            Debug.LogWarning("Course name or ID cannot be empty!");
            return;
        }

        // Save the new course locally
        Course newCourse = new Course(courseName, courseId);
        FindObjectOfType<CourseManager>().AddCourse(newCourse);  // Save the course locally

        // Save the new course to Firestore
        firestoreManager.SaveCourse(newCourse);

        // Create Photon Room
        Hashtable roomProperties = new Hashtable
        {
            { "CourseName", courseName },
            { "CourseID", courseId }
        };

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 20,
            IsVisible = true,  // Keep room visible in the lobby
            IsOpen = true,  // Allow others to join the room
            CleanupCacheOnLeave = false,  // Keep the room available even if empty
            CustomRoomProperties = new Hashtable { { "CourseName", courseName }, { "CourseID", courseId } },
            CustomRoomPropertiesForLobby = new string[] { "CourseName", "CourseID" }
        };

        PhotonNetwork.CreateRoom(courseId, roomOptions);
        courseCreationUI.SetActive(false);
        Debug.Log($"Created room with Course: {courseName} (ID: {courseId}).");
    }

    // Function to handle the new "Connect" button
    public void OnConnectButtonClicked()
    {
        string courseId = courseIdInput.text;

        if (string.IsNullOrEmpty(courseId))
        {
            Debug.LogWarning("Course ID cannot be empty!");
            return;
        }

        // Check if the course exists in the local course manager
        if (courseManager.CourseExists(courseId))
        {
            Debug.Log($"Course with ID {courseId} found. Attempting to join the room.");

            // Attempt to join the room instead of creating it
            PhotonNetwork.JoinRoom(courseId);

            // Hide the initial UI and show the logged-in UI if required
            initialGUI.SetActive(false);
            courseCreationUI.SetActive(false); // Ensure this hides the course creation UI
        }
        else
        {
            Debug.LogWarning("Course not found in the local data. Please check the Course ID.");
        }
    }

}
