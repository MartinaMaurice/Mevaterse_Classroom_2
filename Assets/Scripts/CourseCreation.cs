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

    private CourseManager courseManager = new CourseManager();  // Initialize CourseManager

    public void OnHostButtonClicked()
    {
        string courseName = courseNameInput.text;
        string courseId = courseIdInput.text;

        if (string.IsNullOrEmpty(courseName) || string.IsNullOrEmpty(courseId))
        {
            Debug.LogWarning("Course name or ID cannot be empty!");
            return;
        }

        // Save the new course
        Course newCourse = new Course(courseName, courseId);
        FindObjectOfType<CourseManager>().AddCourse(newCourse);  // Save the course

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
}
