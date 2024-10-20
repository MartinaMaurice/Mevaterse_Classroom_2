using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
{
    private FirebaseAuth auth;
    private FirestoreManager firestoreManager;

    void Start()
    {
        firestoreManager = FindObjectOfType<FirestoreManager>();

        // Check Firebase dependencies and initialize
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance;
                Debug.Log("Firebase Initialized Successfully");

                // Save user to Firestore
                FirebaseUser user = auth.CurrentUser;
                if (user != null)
                {
                    // Assuming "Instructor" or "Student" as role, you can modify this as needed
                    string role = "Student"; // or "Instructor" depending on the logic
                    firestoreManager.SaveUser(user.UserId, role);
                }
            }
            else
            {
                Debug.LogError("Firebase Initialization Failed: " + task.Exception);
            }
        });
    }
}
