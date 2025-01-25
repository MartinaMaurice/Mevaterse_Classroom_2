using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.UI;
using System;

public class LeaderboardManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown categoryDropdown;
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private TextMeshProUGUI[] rankTexts; // For Rank
    [SerializeField] private TextMeshProUGUI[] nameTexts; // For Name
    [SerializeField] private TextMeshProUGUI[] scoreTexts; // For Score
    [SerializeField] private Button[] addButtons;
    [SerializeField] private Button[] subtractButtons;
    [SerializeField] private TextMeshProUGUI searchRankText; // For Search Rank
    [SerializeField] private TextMeshProUGUI searchNameText; // For Search Name
    [SerializeField] private TextMeshProUGUI searchScoreText; // For Search Score
    [SerializeField] private Button searchAddButton;
    [SerializeField] private Button searchSubtractButton;

    private FirebaseFirestore db;
    private string searchStudentId;
    private string userId; // User ID
    private ActivityStatsManager statsManager;

    private List<StudentResult> studentResults = new List<StudentResult>();

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        InitializeDropdown();
        statsManager = FindObjectOfType<ActivityStatsManager>();

        for (int i = 0; i < addButtons.Length; i++)
        {
            int index = i;
            addButtons[i].onClick.AddListener(() => AdjustScore(index, 1));
            subtractButtons[i].onClick.AddListener(() => AdjustScore(index, -1));
        }

        searchAddButton.onClick.AddListener(() => AdjustSearchResultScore(1));
        searchSubtractButton.onClick.AddListener(() => AdjustSearchResultScore(-1));

        searchInputField.onEndEdit.AddListener(SearchStudentById);

        // Get userId from PlayerPrefs
        userId = PlayerPrefs.GetString("UserID", null);
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("User ID not found.");
            return;
        }

        // Ensure buttons are always visible
        UpdateButtonVisibility();
    }

    private void UpdateButtonVisibility()
    {
        foreach (Button button in addButtons)
        {
            if (button != null) button.gameObject.SetActive(true);
        }

        foreach (Button button in subtractButtons)
        {
            if (button != null) button.gameObject.SetActive(true);
        }

        if (searchAddButton != null) searchAddButton.gameObject.SetActive(true);
        if (searchSubtractButton != null) searchSubtractButton.gameObject.SetActive(true);

        Debug.Log("Button visibility updated: Always visible.");
    }

    private void InitializeDropdown()
    {
        List<string> options = new List<string> { "Choose", "Open" };
        categoryDropdown.ClearOptions();
        categoryDropdown.AddOptions(options);
        categoryDropdown.onValueChanged.AddListener(OnCategorySelected);
    }

    private void OnCategorySelected(int index)
    {
        string selectedCategory = categoryDropdown.options[index].text;

        statsManager.IncrementActivity("Leaderboard_opened");

        if (selectedCategory == "Open")
        {
            FetchTopStudents();
        }
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
                rankTexts[i].text = (i + 1).ToString(); // Rank
                nameTexts[i].text = results[i].Name;   // Name
                scoreTexts[i].text = results[i].Score.ToString(); // Score

                rankTexts[i].gameObject.SetActive(true);
                nameTexts[i].gameObject.SetActive(true);
                scoreTexts[i].gameObject.SetActive(true);
            }
            else
            {
                rankTexts[i].gameObject.SetActive(false);
                nameTexts[i].gameObject.SetActive(false);
                scoreTexts[i].gameObject.SetActive(false);
            }
        }
    }

    private void AdjustScore(int index, int amount)
    {
        if (index >= studentResults.Count)
        {
            Debug.LogError("Index out of bounds in AdjustScore function.");
            return;
        }

        string studentId = studentResults[index].Id;
        statsManager.IncrementActivity("Score_Adjusted");
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
        statsManager.IncrementActivity("search used in leaderboard");
        DocumentReference userDocRef = db.Collection("users").Document(studentId);
        userDocRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                string studentName = task.Result.ContainsField("name") ? task.Result.GetValue<string>("name") : studentId;
                int totalScore = task.Result.ContainsField("total_score") ? task.Result.GetValue<int>("total_score") : 0;

                searchStudentId = studentId;
                searchRankText.text = "Search"; // Static rank for search
                searchNameText.text = studentName; // Name
                searchScoreText.text = totalScore.ToString(); // Score

                searchRankText.gameObject.SetActive(true);
                searchNameText.gameObject.SetActive(true);
                searchScoreText.gameObject.SetActive(true);
            }
            else
            {
                searchRankText.text = "-";
                searchNameText.text = "Not Found";
                searchScoreText.text = "-";

                searchRankText.gameObject.SetActive(true);
                searchNameText.gameObject.SetActive(true);
                searchScoreText.gameObject.SetActive(true);
            }
        });
    }

    private void AdjustSearchResultScore(int amount)
    {
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
