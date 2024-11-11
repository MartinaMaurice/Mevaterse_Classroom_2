using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase.Firestore;
using System.Threading.Tasks;

public class LeaderboardManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown categoryDropdown;  // Assign this to your Dropdown object in Unity Inspector
    [SerializeField] private TextMeshProUGUI[] rankTexts;    // Drag Text objects for ranks 1-5 in the Inspector

    private FirebaseFirestore db;

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        InitializeDropdown();
    }

    private void InitializeDropdown()
    {
        List<string> options = new List<string> { "Choose","Quizzes", "Exercises" };
        categoryDropdown.ClearOptions();
        categoryDropdown.AddOptions(options);

        // Set the listener for when an option is selected
        categoryDropdown.onValueChanged.AddListener(OnCategorySelected);
    }

    private void OnCategorySelected(int index)
    {
        string selectedCategory = categoryDropdown.options[index].text;

        if (selectedCategory == "Quizzes")
        {
            FetchTopStudents("quiz_results");
        }
        else if (selectedCategory == "Exercises")
        {
            FetchTopStudents("exercise_results");
        }
    }

   private async void FetchTopStudents(string collectionName)
{
    QuerySnapshot snapshot = await db.Collection("users").GetSnapshotAsync();

    List<StudentResult> studentResults = new List<StudentResult>();

    foreach (DocumentSnapshot userDoc in snapshot.Documents)
    {
        if (userDoc.Exists)
        {
            // Retrieve the user's name from the document
            string studentName = userDoc.ContainsField("name") ? userDoc.GetValue<string>("name") : userDoc.Id;

            QuerySnapshot resultsSnapshot = await userDoc.Reference.Collection(collectionName).GetSnapshotAsync();
            foreach (DocumentSnapshot resultDoc in resultsSnapshot.Documents)
            {
                if (resultDoc.Exists && resultDoc.ContainsField("score"))
                {
                    int score = resultDoc.GetValue<int>("score");
                    studentResults.Add(new StudentResult(studentName, score));
                }
            }
        }
    }

    DisplayTopStudents(studentResults);
}

    private void DisplayTopStudents(List<StudentResult> studentResults)
    {
        studentResults.Sort((a, b) => b.Score.CompareTo(a.Score));  // Sort by score descending

        for (int i = 0; i < rankTexts.Length && i < studentResults.Count; i++)
        {
            rankTexts[i].text = $"Rank {i + 1}: {studentResults[i].Name}, Score: {studentResults[i].Score}";
        }
    }

    private class StudentResult
    {
        public string Name { get; }
        public int Score { get; }

        public StudentResult(string name, int score)
        {
            Name = name;
            Score = score;
        }
    }
}
