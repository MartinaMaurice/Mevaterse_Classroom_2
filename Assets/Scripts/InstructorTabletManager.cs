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
        // RetrieveStudentsWithExercises   ();
        submitUserIDButton.onClick.AddListener(OnSubmitUserID);
        // Dynamically assign the Main Camera to the Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas.worldCamera = Camera.main; // Assign the Main Camera
        }

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
        Transform contentTransform = studentListPanel.transform.Find("StudentsScrollView/Viewport/Content");
        Debug.Log(contentTransform != null ? "Content transform found!" : "Content transform NOT found.");

        if (contentTransform != null)
        {
            foreach (Transform child in contentTransform)
            {
                Destroy(child.gameObject); // Clean up existing buttons
            }

            db.Collection("users").GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    QuerySnapshot userSnapshots = task.Result;

                    foreach (DocumentSnapshot userDoc in userSnapshots.Documents)
                    {
                        string userId = userDoc.Id;
                        string userName = userDoc.ContainsField("name") ? userDoc.GetValue<string>("name") : "Unknown User";

                        // Fetch the Exercise collection for the user
                        db.Collection("users").Document(userId).Collection("Exercise")
                            .GetSnapshotAsync()
                            .ContinueWithOnMainThread(exerciseTask =>
                            {
                                if (exerciseTask.IsCompletedSuccessfully)
                                {
                                    QuerySnapshot exercises = exerciseTask.Result; // Correct way to access result

                                    if (exercises.Documents.Count() > 0)
                                    {
                                        // Create one button per student
                                        GameObject studentButton = Instantiate(studentButtonPrefab, contentTransform);
                                        TMP_Text studentNameText = studentButton.GetComponentInChildren<TMP_Text>();

                                        if (studentNameText != null)
                                        {
                                            studentNameText.text = userName;
                                        }

                                        // Fetch the first exercise
                                        DocumentSnapshot firstExercise = exercises.Documents.FirstOrDefault();
                                        string code = firstExercise.ContainsField("code")
                                            ? firstExercise.GetValue<string>("code")
                                            : "No code found";
                                        string output = firstExercise.ContainsField("output")
                                            ? firstExercise.GetValue<string>("output")
                                            : "No output found";

                                        // Add listener to button
                                        studentButton.GetComponent<Button>()
                                            .onClick.AddListener(() => OnGradeButtonClicked(userId, firstExercise.Id, code, output));

                                        Debug.Log($"Button created for {userName} with code: {code} and output: {output}");
                                    }
                                    else
                                    {
                                        Debug.Log($"No exercises found for {userName}.");
                                    }
                                }
                                else
                                {
                                    Debug.LogError($"Failed to fetch Exercise collection for user: {userId}, Error: {exerciseTask.Exception}");
                                }
                            });
                    }
                }
                else
                {
                    Debug.LogError("Failed to retrieve users.");
                }
            });
        }
    }




    private void OnGradeButtonClicked(string userId, string exerciseId, string code, string output)
    {
        selectedUserId = userId;
        selectedExerciseId = exerciseId;

        gradePanel.SetActive(true);
        studentListPanel.SetActive(false);

        if (inputCodeText != null) inputCodeText.text = code;
        if (outputText != null) outputText.text = output;

        Debug.Log($"Displaying Code: {code}, Output: {output}");
    }


    public void OnSaveGradeButtonClicked()
    {
        string grade = gradeInputField?.text;

        if (!string.IsNullOrEmpty(grade) && !string.IsNullOrEmpty(selectedUserId) && !string.IsNullOrEmpty(selectedExerciseId))
        {
            Dictionary<string, object> gradeData = new Dictionary<string, object>
        {
            { "score", grade }
        };

            db.Collection("users").Document(selectedUserId)
                .Collection("Exercise").Document(selectedExerciseId)
                .SetAsync(gradeData, SetOptions.MergeAll)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        Debug.Log("Score saved successfully.");
                        gradePanel.SetActive(false);
                        studentListPanel.SetActive(true);
                    }
                    else
                    {
                        Debug.LogError("Failed to save score: " + task.Exception);
                    }
                });
        }
        else
        {
            Debug.LogError("Missing grade, user ID, or exercise ID.");
        }
    }


}
