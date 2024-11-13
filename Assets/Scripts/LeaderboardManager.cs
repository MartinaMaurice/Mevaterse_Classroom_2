using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Threading.Tasks;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
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

        for (int i = 0; i < rankTexts.Length && i < results.Count; i++)
        {
            rankTexts[i].text = $"Rank {i + 1}: {results[i].Name}, Score: {results[i].Score}<3";
            rankTexts[i].gameObject.SetActive(true);
            addButtons[i].gameObject.SetActive(true);
            subtractButtons[i].gameObject.SetActive(true);
        }

        for (int i = results.Count; i < rankTexts.Length; i++)
        {
            rankTexts[i].gameObject.SetActive(false);
            addButtons[i].gameObject.SetActive(false);
            subtractButtons[i].gameObject.SetActive(false);
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
                        FetchTopStudents(); // Refresh leaderboard
                        if (searchStudentId == studentId) SearchStudentById(searchStudentId); // Refresh search result if it matches
                    }
                    else
                    {
                        Debug.LogError("Error updating total score: " + updateTask.Exception);
                    }
                });
            }
            else
            {
                Debug.LogError($"User document not found or failed to retrieve for ID: {studentId}");
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
                searchResultText.text = $"ID: {studentId}\nName: {studentName}\nTotal Score: {totalScore}";
                searchResultText.gameObject.SetActive(true);

                searchAddButton.gameObject.SetActive(true);
                searchSubtractButton.gameObject.SetActive(true);
            }
            else
            {
                searchResultText.text = "Student not found.";
                searchResultText.gameObject.SetActive(true);

                searchAddButton.gameObject.SetActive(false);
                searchSubtractButton.gameObject.SetActive(false);
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
                int newScore = currentScore + amount;

                userDocRef.UpdateAsync("total_score", newScore).ContinueWithOnMainThread(updateTask =>
                {
                    if (updateTask.IsCompleted)
                    {
                        Debug.Log($"Updated total score for {searchStudentId} to {newScore}");
                        FetchTopStudents(); // Refresh leaderboard
                        SearchStudentById(searchStudentId); // Refresh search result
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
