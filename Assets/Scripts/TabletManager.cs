using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;

public class TabletManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField userIDInputField;
    [SerializeField] private GameObject userIDPanel;
    [SerializeField] private GameObject quizPanel;

    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Button[] answerButtons;
    [SerializeField] private TextMeshProUGUI scoreText; // Text element to display the final score
    [SerializeField] private Button submitButton; // Reference to the submit button

    private FirebaseFirestore db;
    private string userId;
    private List<Dictionary<string, object>> questions;
    private int currentQuestionIndex = 0;
    private int score = 0; // Tracks the score/grade
    private string selectedQuizId;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        quizPanel.SetActive(false);
        userIDPanel.SetActive(true);
        scoreText.gameObject.SetActive(false); // Hide score text initially
    }

    public void SetQuizId(string quizId)
    {
        selectedQuizId = quizId;
    }

    public void OnUserIDSubmit()
    {
        userId = userIDInputField.text;
        if (!string.IsNullOrEmpty(userId))
        {
            ValidateUserID(userId);
        }
        else
        {
            Debug.LogError("User ID cannot be empty.");
        }
    }

    void ValidateUserID(string userId)
    {
        db.Collection("users").Document(userId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                Debug.Log("User exists. Loading quiz...");
                userIDPanel.SetActive(false);
                quizPanel.SetActive(true);
                LoadQuizFromFirestore(selectedQuizId);
            }
            else
            {
                Debug.LogError("User does not exist. Please enter a valid user ID.");
            }
        });
    }

    void LoadQuizFromFirestore(string quizId)
    {
        if (string.IsNullOrEmpty(quizId))
        {
            Debug.LogError("Quiz ID is not set. Cannot load quiz.");
            return;
        }

        db.Collection("quizzes").Document(quizId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                var quizData = task.Result.ToDictionary();
                questions = new List<Dictionary<string, object>>();

                if (quizData.ContainsKey("material"))
                {
                    var material = quizData["material"] as List<object>;
                    foreach (var item in material)
                    {
                        questions.Add(item as Dictionary<string, object>);
                    }

                    currentQuestionIndex = 0;
                    DisplayQuestion(currentQuestionIndex);
                    Debug.Log("Quiz loaded successfully.");
                }
                else
                {
                    Debug.LogError("No 'material' field found in the quiz document.");
                }
            }
            else
            {
                Debug.LogError("Quiz not found or error retrieving quiz.");
            }
        });
    }

    void DisplayQuestion(int questionIndex)
    {
        if (questionIndex < questions.Count)
        {
            var questionData = questions[questionIndex];

            if (questionData.TryGetValue("question", out var questionTextValue))
            {
                questionText.text = questionTextValue.ToString();
            }

            if (questionData.TryGetValue("choices", out var choicesValue) && choicesValue is List<object> choices)
            {
                for (int i = 0; i < answerButtons.Length; i++)
                {
                    if (i < choices.Count)
                    {
                        var answerTextTMP = answerButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        answerTextTMP.text = choices[i].ToString();
                        answerButtons[i].gameObject.SetActive(true); // Show the button
                    }
                    else
                    {
                        answerButtons[i].gameObject.SetActive(false); // Hide unused buttons
                    }
                }
            }
            else
            {
                Debug.LogError("Choices not found or are not in the expected format.");
            }
        }
        else
        {
            EndQuiz(); // End the quiz when all questions are answered
        }
    }

    public void OnAnswerSelected(int selectedAnswerIndex)
    {
        if (questions == null || currentQuestionIndex >= questions.Count)
        {
            Debug.LogError("Questions data is not loaded or question index is out of range.");
            return;
        }

        var questionData = questions[currentQuestionIndex];

        // Check if the selected answer is correct
        if (questionData.TryGetValue("answer", out var correctAnswer))
        {
            var selectedAnswerText = answerButtons[selectedAnswerIndex].GetComponentInChildren<TextMeshProUGUI>().text;
            if (selectedAnswerText == correctAnswer.ToString())
            {
                Debug.Log("Correct Answer");
                score++; // Increment score if correct
            }
            else
            {
                Debug.Log("Incorrect Answer");
            }
        }
        else
        {
            Debug.LogError("Correct answer not found in question data.");
        }

        // Move to the next question
        currentQuestionIndex++;
        if (currentQuestionIndex < questions.Count)
        {
            DisplayQuestion(currentQuestionIndex);
        }
        else
        {
            EndQuiz();
        }
    }

    void EndQuiz()
    {
        // Hide all answer buttons
        foreach (var button in answerButtons)
        {
            button.gameObject.SetActive(false);
        }

        // Display the final score
        scoreText.gameObject.SetActive(true);
        scoreText.text = "Your Score: " + score;
        questionText.text = "Quiz is done";
        // Change submit button to exit button
        submitButton.GetComponentInChildren<TextMeshProUGUI>().text = "Exit";
        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(ExitQuiz);

        SaveQuizResults(); // Save the quiz results to Firestore
    }

    void ExitQuiz()
    {
        quizPanel.SetActive(false);  // Hide quiz panel
        userIDPanel.SetActive(true); // Show user ID panel
        scoreText.gameObject.SetActive(false); // Hide the score text

        // Reset for a new quiz
        currentQuestionIndex = 0;
        score = 0;

        // Reset submit button for the next quiz
        submitButton.GetComponentInChildren<TextMeshProUGUI>().text = "Submit";
        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(() => Debug.Log("Please select an answer.")); // Placeholder until answer is selected
    }

 void SaveQuizResults()
{
    // Define the data to save for this specific quiz result
    Dictionary<string, object> resultData = new Dictionary<string, object>
    {
        { "quiz_id", selectedQuizId },
        { "quiz_completed", true },
        { "score", score },
        { "activity_name", "Quiz" } // Adjust as needed, e.g., based on activity type
    };

    // Save individual activity result in the user's "activity_results" subcollection
    db.Collection("users").Document(userId).Collection("activity_results").Document(selectedQuizId).SetAsync(resultData, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
    {
        if (task.IsCompleted)
        {
            Debug.Log("Activity result successfully saved for user.");
            
            // After saving individual activity result, update the user's total score
            UpdateTotalScore();
        }
        else
        {
            Debug.LogError("Error saving activity result: " + task.Exception);
        }
    });
}

// Method to update the user's total score
void UpdateTotalScore()
{
    // Reference to the user's main document
    DocumentReference userDocRef = db.Collection("users").Document(userId);

    // Retrieve the existing total score
    userDocRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
    {
        if (task.IsCompleted && task.Result.Exists)
        {
            int existingTotalScore = 0;

            // Check if the "total_score" field exists and retrieve its value
            if (task.Result.ContainsField("total_score"))
            {
                existingTotalScore = task.Result.GetValue<int>("total_score");
            }

            // Calculate the new total score by adding the current quiz score
            int newTotalScore = existingTotalScore + score;

            // Update the "total_score" field in the user's main document
            userDocRef.UpdateAsync("total_score", newTotalScore).ContinueWithOnMainThread(updateTask =>
            {
                if (updateTask.IsCompleted)
                {
                    Debug.Log("Total score successfully updated.");
                }
                else
                {
                    Debug.LogError("Error updating total score: " + updateTask.Exception);
                }
            });
        }
        else
        {
            Debug.LogError("Error retrieving user's total score: " + task.Exception);
        }
    });
}

}