using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.UI;
using System;

public class LeaderboardManager : MonoBehaviour
{
     [SerializeField] private StatisticsManager statisticsManager;

    [SerializeField] private TMP_Dropdown categoryDropdown;
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private TextMeshProUGUI[] rankTexts;
    [SerializeField] private Button[] addButtons;
    [SerializeField] private Button[] subtractButtons;
    [SerializeField] private TextMeshProUGUI searchResultText;
    [SerializeField] private Button searchAddButton;
    [SerializeField] private Button searchSubtractButton;

      private FirebaseFirestore db;
    private string searchStudentId;
    private List<StudentResult> studentResults = new List<StudentResult>();
    private string userRole = "Student"; // Default role

public event Action<string, string> OnActionTriggered;

   private void TriggerAction(string actionName)
    {
        OnActionTriggered?.Invoke(this.GetType().Name, actionName);
    }
    
   private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        InitializeDropdown();

        for (int i = 0; i < addButtons.Length; i++)
        {
            int index = i;
            addButtons[i].onClick.AddListener(() => AdjustScore(index, 1));
            subtractButtons[i].onClick.AddListener(() => AdjustScore(index, -1));
        }

        searchAddButton.onClick.AddListener(() => AdjustSearchResultScore(1));
        searchSubtractButton.onClick.AddListener(() => AdjustSearchResultScore(-1));

        searchInputField.onEndEdit.AddListener(SearchStudentById);
    }
      public void SetUserRole(string role)
    {
        userRole = role;
        Debug.Log($"User role set in LeaderboardManager: {userRole}");
        UpdateButtonVisibility();
    }
     private void UpdateButtonVisibility()
    {
        bool isInstructor = userRole == "Instructor";

        foreach (Button button in addButtons)
        {
            if (button != null) button.gameObject.SetActive(isInstructor);
        }

        foreach (Button button in subtractButtons)
        {
            if (button != null) button.gameObject.SetActive(isInstructor);
        }

        if (searchAddButton != null) searchAddButton.gameObject.SetActive(isInstructor);
        if (searchSubtractButton != null) searchSubtractButton.gameObject.SetActive(isInstructor);
    }

    private void InitializeDropdown()
    {
        List<string> options = new List<string> { "Choose", "Total Score" };
        categoryDropdown.ClearOptions();
        categoryDropdown.AddOptions(options);
        categoryDropdown.onValueChanged.AddListener(OnCategorySelected);
    }

    private void OnCategorySelected(int index)
    {
        string selectedCategory = categoryDropdown.options[index].text;

        if (selectedCategory == "Total Score")
        {
            FetchTopStudents();
        }
                TriggerAction("Leadership board opened");

    }

    private async void FetchUserRole()
    {
        string userId = PlayerPrefs.GetString("UserID", null);

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("User ID not found. Ensure the user is logged in.");
            return;
        }

        Debug.Log("Fetching user role...");
        DocumentReference userDocRef = db.Collection("users").Document(userId);
        DocumentSnapshot snapshot = await userDocRef.GetSnapshotAsync();

        if (snapshot.Exists && snapshot.ContainsField("role"))
        {
            userRole = snapshot.GetValue<string>("role");
            Debug.Log($"User role fetched: {userRole}");
        }
        else
        {
            Debug.LogError("User role not found in the database. Defaulting to 'Student'.");
            userRole = "Student"; // Default role
        }

        CheckUserRole();
    }

    private void CheckUserRole()
    {
        bool isInstructor = userRole == "Instructor";
        Debug.Log($"User role: {userRole}. Is Instructor: {isInstructor}");

        // Enable/Disable Add/Subtract buttons based on role
        foreach (Button button in addButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(isInstructor);
            }
        }

        foreach (Button button in subtractButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(isInstructor);
            }
        }

        // Enable/Disable search Add/Subtract buttons
        if (searchAddButton != null) searchAddButton.gameObject.SetActive(isInstructor);
        if (searchSubtractButton != null) searchSubtractButton.gameObject.SetActive(isInstructor);
    }

    private async void FetchTopStudents()
    {
        QuerySnapshot snapshot = await db.Collection("users").GetSnapshotAsync();

        studentResults.Clear();

        foreach (DocumentSnapshot userDoc in snapshot.Documents)
        {
            if (userDoc.Exists)
            {
                string studentName = userDoc.ContainsField("name") ? userDoc.GetValue<string>("name") : userDoc.Id;
                int totalScore = userDoc.ContainsField("total_score") ? userDoc.GetValue<int>("total_score") : 0;

                studentResults.Add(new StudentResult(userDoc.Id, studentName, totalScore));
            }
        }

        DisplayTopStudents(studentResults);
    }

    private void DisplayTopStudents(List<StudentResult> results)
    {
        results.Sort((a, b) => b.Score.CompareTo(a.Score));

        for (int i = 0; i < rankTexts.Length; i++)
        {
            if (i < results.Count)
            {
                rankTexts[i].text = $"Rank {i + 1}: {results[i].Name}, Score: {results[i].Score}";
                rankTexts[i].gameObject.SetActive(true);

                if (userRole == "Instructor")
                {
                    if (addButtons[i] != null) addButtons[i].gameObject.SetActive(true);
                    if (subtractButtons[i] != null) subtractButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    if (addButtons[i] != null) addButtons[i].gameObject.SetActive(false);
                    if (subtractButtons[i] != null) subtractButtons[i].gameObject.SetActive(false);
                }
            }
            else
            {
                rankTexts[i].gameObject.SetActive(false);
                if (addButtons[i] != null) addButtons[i].gameObject.SetActive(false);
                if (subtractButtons[i] != null) subtractButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void AdjustScore(int index, int amount)
    {
        if (userRole != "Instructor")
        {
            Debug.LogWarning("Only instructors can adjust scores.");
            return;
        }

        if (index >= studentResults.Count)
        {
            Debug.LogError("Index out of bounds in AdjustScore function.");
            return;
        }

        string studentId = studentResults[index].Id;

        DocumentReference userDocRef = db.Collection("users").Document(studentId);
        userDocRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                int currentScore = task.Result.ContainsField("total_score") ? task.Result.GetValue<int>("total_score") : 0;
                int newScore = currentScore + amount;

                userDocRef.UpdateAsync("total_score", newScore).ContinueWithOnMainThread(updateTask =>
                {
                    if (updateTask.IsCompleted)
                    {
                        Debug.Log($"Updated total score for {studentId} to {newScore}");
                        FetchTopStudents();
                    }
                    else
                    {
                        Debug.LogError("Error updating total score: " + updateTask.Exception);
                    }
                });
            }
            else
            {
                Debug.LogError($"User document not found for ID: {studentId}");
            }
        });
    }
    private void SearchStudentById(string studentId)
{
    if (string.IsNullOrEmpty(studentId))
    {
        Debug.LogWarning("Search input is empty.");
        return;
    }

    DocumentReference userDocRef = db.Collection("users").Document(studentId);
    userDocRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
    {
        if (task.IsCompleted && task.Result.Exists)
        {
            string studentName = task.Result.ContainsField("name") ? task.Result.GetValue<string>("name") : studentId;
            int totalScore = task.Result.ContainsField("total_score") ? task.Result.GetValue<int>("total_score") : 0;

            searchStudentId = studentId;
            searchResultText.text = $"Name: {studentName} Total Score: {totalScore}";
            searchResultText.gameObject.SetActive(true);

            // Ensure buttons are only visible for instructors
            bool isInstructor = userRole == "Instructor";
            Debug.Log($"User role during search: {userRole}. Is Instructor: {isInstructor}");

            searchAddButton.gameObject.SetActive(isInstructor);
            searchSubtractButton.gameObject.SetActive(isInstructor);
        }
        else
        {
            searchResultText.text = "Student not found.";
            searchResultText.gameObject.SetActive(true);

            // Hide buttons if no student is found
            searchAddButton.gameObject.SetActive(false);
            searchSubtractButton.gameObject.SetActive(false);
        }
    });
            TriggerAction("Search Input Entered");

}

   private void AdjustSearchResultScore(int amount)
{
    if (userRole != "Instructor")
    {
        Debug.LogWarning("Only instructors can adjust scores.");
        return;
    }

    if (string.IsNullOrEmpty(searchStudentId))
    {
        Debug.LogWarning("No student selected for score adjustment.");
        return;
    }

    DocumentReference userDocRef = db.Collection("users").Document(searchStudentId);
    userDocRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
    {
        if (task.IsCompleted && task.Result.Exists)
        {
            int currentScore = task.Result.ContainsField("total_score") ? task.Result.GetValue<int>("total_score") : 0;
            int newScore = Mathf.Max(0, currentScore + amount);

            userDocRef.UpdateAsync("total_score", newScore).ContinueWithOnMainThread(updateTask =>
            {
                if (updateTask.IsCompleted)
                {
                    Debug.Log($"Updated total score for {searchStudentId} to {newScore}");
                    FetchTopStudents();
                    SearchStudentById(searchStudentId);
                }
                else
                {
                    Debug.LogError("Error updating total score: " + updateTask.Exception);
                }
            });
        }
        else
        {
            Debug.LogError("Failed to retrieve document for score adjustment.");
        }
    });
}

    private class StudentResult
    {
        public string Id { get; }
        public string Name { get; }
        public int Score { get; }

        public StudentResult(string id, string name, int score)
        {
            Id = id;
            Name = name;
            Score = score;
        }
    }
}
