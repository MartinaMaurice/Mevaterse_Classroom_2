using Firebase.Firestore;
using Firebase.Extensions; // Add this to use ContinueWithOnMainThread
using UnityEngine;
using System.Collections.Generic;

public class FirestoreManager : MonoBehaviour
{
    FirebaseFirestore db;
    public List<string> availableCourses = new List<string>();  // List to store available course IDs

    void Start()
    {
        // Get Firestore instance
        db = FirebaseFirestore.DefaultInstance;

        // Initialize the collections without adding data
        InitializeFirestoreCollections();
        LoadCoursesFromFirestore();

    }

    void InitializeFirestoreCollections()
    {
        // Reference collections to ensure they are initialized
        db.Collection("Users");        // Users collection
        db.Collection("Courses");      // Courses collection
        db.Collection("Quizzes");      // Quizzes collection
        db.Collection("Grades");       // Grades collection
        db.Collection("LeadershipBoard"); // Leadership Board collection
        db.Collection("Slides");

        // Log success message
        Debug.Log("Firestore collections initialized for future use.");
    }

    // Save user data to Firestore
    public void SaveUser(string userId, string role)
    {
        Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "userId", userId },
            { "role", role }
        };

        db.Collection("Users").Document(userId).SetAsync(userData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("User data saved successfully.");
            }
            else
            {
                Debug.LogError("Failed to save user data: " + task.Exception);
            }
        });
    }

    // Save course data to Firestore
    public void SaveCourse(Course newCourse)
    {
        db.Collection("Courses").Document(newCourse.CourseID).SetAsync(new
        {
            CourseName = newCourse.CourseName,
            CourseID = newCourse.CourseID
        }).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Course saved to Firestore");
            }
            else
            {
                Debug.LogError("Failed to save course to Firestore: " + task.Exception);
            }
        });
    }

    public void LoadCoursesFromFirestore()
    {
        // Fetch courses from the "Courses" collection in Firestore
        db.Collection("Courses").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                QuerySnapshot snapshot = task.Result;
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    string courseID = document.Id;  // Assuming CourseID is the document ID
                    availableCourses.Add(courseID);  // Add courseID to the list
                    Debug.Log($"Course ID retrieved: {courseID}");
                }

                Debug.Log($"Total courses loaded: {availableCourses.Count}");
            }
            else
            {
                Debug.LogError("Failed to retrieve courses from Firestore: " + task.Exception);
            }
        });
    }

    // Check if a course exists in the available courses list
    public bool CourseExistsInFirestore(string courseId)
    {
        return availableCourses.Contains(courseId);
    }
}
