using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using System.Linq;
using Firebase.Extensions;
public class InstructorTabletManager : MonoBehaviour
{
    [SerializeField] private GameObject userIDPanel;             // Panel for entering User ID
    [SerializeField] private TMP_InputField userIDInputField;    // Input field for User ID
    [SerializeField] private Button submitUserIDButton;          // Button to submit User ID

    [SerializeField] private GameObject studentListPanel;        // Panel displaying list of students
    [SerializeField] private GameObject gradePanel;              // Panel for grading exercise details
    [SerializeField] private GameObject studentButtonPrefab;     // Prefab for each student button

    [SerializeField] private TMP_Text inputCodeText;             // Text to display student's input code
    [SerializeField] private TMP_Text outputText;                // Text to display student's output
    [SerializeField] private TMP_InputField gradeInputField;     // Input field for entering grade
    [SerializeField] private Button saveGradeButton;             // Button to save the grade

    private FirebaseFirestore db;                                // Firestore instance
    private string selectedUserId;                               // Selected student's ID
    private string selectedExerciseId;                           // Selected exercise ID
    private string instructorId;                                 // Instructor's ID

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        userIDPanel.SetActive(true);
        studentListPanel.SetActive(false);
        gradePanel.SetActive(false);

        submitUserIDButton.onClick.AddListener(OnSubmitUserID);
    }

    private void OnSubmitUserID()
    {
        instructorId = userIDInputField.text;
        if (string.IsNullOrEmpty(instructorId))
        {
            Debug.LogError("User ID cannot be empty.");
            return;
        }

        db.Collection("users").Document(instructorId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DocumentSnapshot document = task.Result;
                if (document.Exists && document.ContainsField("role") && document.GetValue<string>("role") == "Instructor")
                {
                    Debug.Log("Instructor verified.");
                    userIDPanel.SetActive(false);
                    studentListPanel.SetActive(true); // Ensure student list panel is active
                    gradePanel.SetActive(false);

                    RetrieveStudentsWithExercises();
                }
                else
                {
                    Debug.LogError("User ID not found or user is not an instructor.");
                }
            }
            else
            {
                Debug.LogError("Failed to retrieve document for user ID: " + instructorId + ". Error: " + task.Exception);
            }
        });
    }

    private void RetrieveStudentsWithExercises()
    {
        Debug.Log("Retrieving students with exercises...");

        // Find the Content Transform for Student Buttons
        Transform contentTransform = studentListPanel.transform.Find("StudentsScrollView/Viewport/Content");
        if (contentTransform == null)
        {
            Debug.LogError("Content transform not found. Please check your hierarchy.");
            return;
        }

        // Clear existing buttons to avoid duplicates
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        // Query Firestore to retrieve all users
        db.Collection("users")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    QuerySnapshot userSnapshots = task.Result;
                    int studentCount = 0;

                    foreach (DocumentSnapshot userDoc in userSnapshots.Documents)
                    {
                        string userId = userDoc.Id;
                        string userName = userDoc.ContainsField("name") ? userDoc.GetValue<string>("name") : "Unknown User";

                        db.Collection("users").Document(userId).Collection("exercises")
                            .GetSnapshotAsync()
                            .ContinueWithOnMainThread(exerciseTask =>
                            {
                                if (exerciseTask.IsCompletedSuccessfully && exerciseTask.Result.Count() > 0)
                                {
                                    studentCount++;
                                    Debug.Log("Instantiating button for student: " + userName);

                                    // Instantiate the student button prefab under Content
                                    GameObject studentButton = Instantiate(studentButtonPrefab, contentTransform);
                                    if (studentButton == null)
                                    {
                                        Debug.LogError("Failed to instantiate student button.");
                                        return;
                                    }

                                    TMP_Text studentNameText = studentButton.GetComponentInChildren<TMP_Text>();
                                    if (studentNameText == null)
                                    {
                                        Debug.LogError("TMP_Text component missing on student button prefab.");
                                    }
                                    else
                                    {
                                        // Set the text for the student’s name
                                        studentNameText.text = userName;
                                    }

                                    // Add a listener to handle grading
                                    studentButton.GetComponent<Button>().onClick.AddListener(() => OnGradeButtonClicked(userId));

                                    Debug.Log("Button instantiated with text: " + userName);
                                }
                            });
                    }

                    if (studentCount == 0)
                    {
                        Debug.Log("No students with exercises found.");
                    }
                    else
                    {
                        Debug.Log("Total students with exercises found: " + studentCount);
                    }
                }
                else
                {
                    Debug.LogError("Failed to retrieve users: " + task.Exception);
                }
            });
    }

    private void OnGradeButtonClicked(string userId)
    {
        selectedUserId = userId;
        gradePanel.SetActive(true);
        studentListPanel.SetActive(false);

        db.Collection("users").Document(userId).Collection("exercises")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    QuerySnapshot snapshot = task.Result;

                    foreach (DocumentSnapshot exerciseDoc in snapshot.Documents)
                    {
                        string inputCode = exerciseDoc.ContainsField("code") ? exerciseDoc.GetValue<string>("code") : "No code available";
                        string output = exerciseDoc.ContainsField("output") ? exerciseDoc.GetValue<string>("output") : "No output available";
                        selectedExerciseId = exerciseDoc.Id;

                        inputCodeText.text = inputCode;
                        outputText.text = output;
                    }
                }
                else
                {
                    Debug.LogError("Failed to retrieve exercises for user: " + task.Exception);
                }
            });
    }


    public void OnSaveGradeButtonClicked()
    {
        string grade = gradeInputField.text;

        if (!string.IsNullOrEmpty(grade) && !string.IsNullOrEmpty(selectedUserId) && !string.IsNullOrEmpty(selectedExerciseId))
        {
            Dictionary<string, object> gradeData = new Dictionary<string, object>
            {
                { "grade", grade }
            };

            db.Collection("users").Document(selectedUserId).Collection("exercises").Document(selectedExerciseId)
                .SetAsync(gradeData, SetOptions.MergeAll)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                    {
                        Debug.Log("Grade saved successfully.");
                        gradePanel.SetActive(false);
                        studentListPanel.SetActive(true);
                    }
                    else
                    {
                        Debug.LogError("Failed to save grade: " + task.Exception);
                    }
                });
        }
        else
        {
            Debug.LogError("Grade or user information is missing.");
        }
    }
}
