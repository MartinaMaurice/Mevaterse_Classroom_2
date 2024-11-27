using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using System.Collections.Generic;
using UnityDebug = UnityEngine.Debug;
using System.Diagnostics;
using System.IO;
using Firebase.Extensions;
using SFB;
using System;
using ExcelDataReader;

using TMPro;

public class InstructorToolkit : MonoBehaviour
{
    public GameObject toolkitPanel;
    public Button addUserButton;  // New button for adding users from Excel

  public GameObject idPromptPanel;     // Panel for entering ID
    public TMP_InputField idInputField;  // Input field for entering ID (TextMeshPro)
    public Button idSubmitButton;        // Button to submit ID
    public TextMeshProUGUI idFeedbackText; // Text to display feedback (TextMeshPro)

    public GameObject initialUI;
    public Button addQuizButton;
    public Button addSlidesButton;
    public Button closeButton;
    public Button idcloseButton;

    public Button closeButtonDeb;

    public Button addDebuggingButton;

    public Button assignmentButton;

    public Button exerciseButton;

    private FirebaseFirestore db;

    public GameObject slideDisplayPanel;

    public GameObject debuggingPanel;
    public string pythonPath = "C:\\Users\\DaWitchBtch\\AppData\\Local\\Programs\\Python\\Python311\\python.exe";
    private bool isInstructorVerified = false; // Track if the user has passed ID validation

     void Start()
    {
        // Ensure panels are inactive/active based on initialization
        toolkitPanel.SetActive(false);
        idPromptPanel.SetActive(false);
        initialUI.SetActive(true);

        db = FirebaseFirestore.DefaultInstance;

        // Assign button listeners
        idSubmitButton.onClick.AddListener(CheckInstructorID);
        closeButton.onClick.AddListener(CloseToolkit);
        idcloseButton.onClick.AddListener(CloseIdPrompt);
    }
  void OpenIDPrompt()
    {
        idPromptPanel.SetActive(true);
        toolkitPanel.SetActive(false);
        initialUI.SetActive(false);
        idFeedbackText.text = ""; // Clear any previous feedback
        UnityDebug.Log("Opened ID prompt panel for validation.");
    }

     public void CheckInstructorID()
    {
        string enteredID = idInputField.text.Trim();
        if (string.IsNullOrEmpty(enteredID))
        {
            idFeedbackText.text = "Please enter a valid ID.";
            UnityDebug.LogError("ID input is empty.");
            return;
        }

        db.Collection("users").Document(enteredID).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                var snapshot = task.Result;
                if (snapshot.ContainsField("role"))
                {
                    string role = snapshot.GetValue<string>("role");
                    if (role == "Instructor")
                    {
                        UnityDebug.Log("Access granted: Instructor ID verified.");
                        idFeedbackText.text = "Access granted. Welcome!";
                        idPromptPanel.SetActive(false);
                        toolkitPanel.SetActive(true);
                    }
                    else
                    {
                        UnityDebug.Log("Access denied: Not an instructor.");
                        idFeedbackText.text = "Access denied. You are not an instructor.";
                    }
                }
                else
                {
                    UnityDebug.LogError("Role field missing in user document.");
                    idFeedbackText.text = "Invalid ID. Role information not found.";
                }
            }
            else
            {
                UnityDebug.LogError("ID not found in Firestore.");
                idFeedbackText.text = "ID not found. Please try again.";
            }
        });
    }
       public void ToggleToolkit()
    {
        if (isInstructorVerified)
        {
            // If already verified, directly show the toolkit panel
            toolkitPanel.SetActive(true);
            initialUI.SetActive(false);
        }
        else
        {
            // Otherwise, show the ID prompt panel
            OpenIDPrompt();
        }
    }
    void CloseToolkit()
    {
        // Hide the toolkit and return to the initial UI
        toolkitPanel.SetActive(false);
        initialUI.SetActive(true);  // Show the initial UI
                isInstructorVerified = false; // Reset verification for next time
 
    }

    void CloseDebuggingPanel()
    {
        debuggingPanel.SetActive(false);
        toolkitPanel.SetActive(true);
    }
     public void CloseIdPrompt()
    {
        idPromptPanel.SetActive(false);
        initialUI.SetActive(true);
    }
    void AddQuiz()
    {
        UnityDebug.Log("Instructor wants to add a quiz.");

        string filePath = StandaloneFileBrowser.OpenFilePanel("Select Quiz Excel", "", "xlsx", false)[0]; // File browser for Excel file
        if (!string.IsNullOrEmpty(filePath))
        {
            List<Dictionary<string, object>> quizData = ReadExcelFile(filePath);
            SaveQuizToFirestore(quizData);
        }
    }

    List<Dictionary<string, object>> ReadExcelFile(string filePath)
    {
        List<Dictionary<string, object>> quizData = new List<Dictionary<string, object>>();

        using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                // Skip the header row
                reader.Read();

                while (reader.Read())
                {
                    var quizEntry = new Dictionary<string, object>
                {
                    { "question", reader.GetValue(0)?.ToString() },
                    { "choices", new List<string>
                        {
                            reader.GetValue(1)?.ToString(),
                            reader.GetValue(2)?.ToString(),
                            reader.GetValue(3)?.ToString(),
                            reader.GetValue(4)?.ToString(),
                        }
                    },
                    { "answer", reader.GetValue(5)?.ToString() }
                };
                    quizData.Add(quizEntry);
                }
            }
        }

        return quizData;
    }

    void SaveQuizToFirestore(List<Dictionary<string, object>> quizData)
    {
        string quizId = db.Collection("quizzes").Document().Id; // Auto-generate quiz ID
        string userId = PlayerPrefs.GetString("UserID"); // Assuming user ID is saved in PlayerPrefs after login

        Dictionary<string, object> quizDoc = new Dictionary<string, object>
        {
            { "user_id", userId },
            { "quiz_id", quizId },
            { "name", "Quiz" },  // You can replace this with quiz name input
            { "material", quizData } // Add the parsed Excel data
        };

        db.Collection("quizzes").Document(quizId).SetAsync(quizDoc).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                UnityDebug.Log("Quiz successfully added to Firestore.");
            }
            else
            {
                UnityDebug.LogError("Error adding quiz: " + task.Exception);
            }
        });
    }
    int GetNextLectureNumberFromDirectory()
    {
        string imagesPath = Path.Combine(Application.dataPath, "Resources", "Images");
        if (!Directory.Exists(imagesPath))
        {
            Directory.CreateDirectory(imagesPath);
            return 1;
        }

        int maxLectureNumber = 0;
        foreach (string dir in Directory.GetDirectories(imagesPath))
        {
            string dirName = Path.GetFileName(dir);
            if (dirName.StartsWith("Lecture_"))
            {
                if (int.TryParse(dirName.Substring("Lecture_".Length), out int lectureNumber))
                {
                    maxLectureNumber = Mathf.Max(maxLectureNumber, lectureNumber);
                }
            }
        }

        return maxLectureNumber + 1;
    }

    void AddSlides()
    {
        UnityEngine.Debug.Log("Instructor wants to add slides.");
        int lectureNumber = GetNextLectureNumberFromDirectory();
        string lectureFolderName = "Lecture_" + lectureNumber;
        string outputFolder = Path.Combine(Application.dataPath, "Resources", "Images", lectureFolderName);
        UnityEngine.Debug.Log("Output folder: " + outputFolder);
        // Create the directory if it doesn't exist
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // Select PDF and run Python script
        string pdfPath = StandaloneFileBrowser.OpenFilePanel("Select PDF", "", "pdf", false)[0];
        List<string> slidePaths = RunPythonScript(pdfPath, outputFolder);

        // Display slides if any images were generated
        if (slidePaths != null && slidePaths.Count > 0)
        {
            DisplaySlides(slidePaths);
            SaveSlidesToFirestore(lectureNumber, slidePaths);

        }
        else
        {
            UnityEngine.Debug.LogError("Failed to generate slides using the Python script.");
        }
    }
    void SaveSlidesToFirestore(int lectureNumber, List<string> slidePaths)
    {
        // Create a unique document ID for the lecture
        string lectureId = "Lecture_" + lectureNumber;

        // Prepare data to save
        Dictionary<string, object> lectureData = new Dictionary<string, object>
    {
        { "lectureNumber", lectureNumber },
        { "slidePaths", slidePaths } // Saving the list of paths directly
    };

        // Reference to Firestore and add the document
        db.Collection("Lectures").Document(lectureId).SetAsync(lectureData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                UnityEngine.Debug.Log("Lecture " + lectureId + " and slide paths saved successfully in Firestore.");
            }
            else
            {
                UnityEngine.Debug.LogError("Error saving lecture data to Firestore: " + task.Exception);
            }
        });
    }
    List<string> RunPythonScript(string pdfPath, string outputFolder)
    {
        List<string> slidePaths = new List<string>();

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"\"{Application.dataPath}/convert_pdf.py\" \"{pdfPath}\" \"{outputFolder}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (File.Exists(line.Trim()))
                    {
                        slidePaths.Add(line.Trim());
                    }
                }
            }

            string error = process.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(error))
            {
                UnityEngine.Debug.LogError("Python error: " + error);
            }

            process.WaitForExit();
        }

        return slidePaths;
    }

    void DisplaySlides(List<string> slidePaths)
    {
        foreach (Transform child in slideDisplayPanel.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (string path in slidePaths)
        {
            Texture2D slideImage = LoadImageFromPath(path);
            if (slideImage != null)
            {
                GameObject slideObj = new GameObject("Slide");
                slideObj.transform.SetParent(slideDisplayPanel.transform);

                Image slideUIImage = slideObj.AddComponent<Image>();
                slideUIImage.sprite = Sprite.Create(slideImage, new Rect(0, 0, slideImage.width, slideImage.height), new Vector2(0.5f, 0.5f));
            }
        }
    }

    Texture2D LoadImageFromPath(string filePath)
    {
        byte[] imageData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);
        return texture;
    }

    void OpenDebuggingPanel()
    {
        debuggingPanel.SetActive(true);
        toolkitPanel.SetActive(false);
    }

    void AddDebuggingMaterial(string type)
    {
        debuggingPanel.SetActive(false); // Close the debugging panel

        // Get the next number for the folder (e.g., Assignment_1, Exercise_1)
        int nextNumber = GetNextNumberFromDirectory(type);
        string folderName = $"{type}_{nextNumber}"; // Folder name with type and number
        string outputFolder = Path.Combine(Application.dataPath, "Resources", "Images", folderName); // Correct path structure

        // Create the directory if it doesn't exist
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // Select PDF and convert
        string pdfPath = StandaloneFileBrowser.OpenFilePanel("Select PDF", "", "pdf", false)[0];
        List<string> debuggingPaths = RunPythonScript(pdfPath, outputFolder);

        // Display images and save to Firestore
        if (debuggingPaths != null && debuggingPaths.Count > 0)
        {
            DisplaySlides(debuggingPaths);
            SaveDebuggingToFirestore(folderName, debuggingPaths); // Save with the folder name

            // After adding debugging material, return to the initial UI
            CloseToolkit();
        }
        else
        {
            UnityDebug.LogError("Failed to generate debugging material using the Python script.");
        }
    }
    int GetNextNumberFromDirectory(string type)
    {
        string basePath = Path.Combine(Application.dataPath, "Resources", "Images");
        int maxNumber = 0;

        if (Directory.Exists(basePath))
        {
            foreach (string dir in Directory.GetDirectories(basePath))
            {
                string dirName = Path.GetFileName(dir);
                if (dirName.StartsWith(type + "_"))
                {
                    if (int.TryParse(dirName.Substring((type + "_").Length), out int number))
                    {
                        maxNumber = Mathf.Max(maxNumber, number);
                    }
                }
            }
        }
        else
        {
            Directory.CreateDirectory(basePath);
        }

        return maxNumber + 1;
    }

    void SaveDebuggingToFirestore(string folderName, List<string> paths)
    {
        string docId = folderName; // Use the folder name as the document ID

        Dictionary<string, object> data = new Dictionary<string, object>
    {
        { "type", folderName.StartsWith("Assignment") ? "Assignment" : "Exercise" },
        { "paths", paths }
    };

        db.Collection("DebuggingMaterials").Document(docId).SetAsync(data).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                UnityDebug.Log($"{folderName} materials saved successfully in Firestore.");
            }
            else
            {
                UnityDebug.LogError("Error saving debugging materials to Firestore: " + task.Exception);
            }
        });
    }
    void AddUsersFromExcel()
    {
        string filePath = StandaloneFileBrowser.OpenFilePanel("Select User Excel File", "", "xlsx", false)[0];

        if (!string.IsNullOrEmpty(filePath))
        {
            List<Dictionary<string, object>> userData = ReadUserExcelFile(filePath);
            SaveUsersToFirestore(userData);
        }
    }

    // Reads user data from the selected Excel file
    List<Dictionary<string, object>> ReadUserExcelFile(string filePath)
    {
        List<Dictionary<string, object>> userData = new List<Dictionary<string, object>>();

        using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                // Skip header row
                reader.Read();
                while (reader.Read())
                {
                    var userEntry = new Dictionary<string, object>
                    {
                        { "name", reader.GetValue(0)?.ToString() },
                        { "id", reader.GetValue(1)?.ToString() },
                        { "course_id", reader.GetValue(2)?.ToString() },
                        {"role", reader.GetValue(3)?.ToString() }
                    };
                    userData.Add(userEntry);
                }
            }
        }
        return userData;
    }

    // Saves the parsed user data to Firestore
    void SaveUsersToFirestore(List<Dictionary<string, object>> userData)
    {
        foreach (var user in userData)
        {
            string userId = user["id"].ToString();
            db.Collection("users").Document(userId).SetAsync(user, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    UnityEngine.Debug.Log("User successfully added to Firestore: " + userId);
                }
                else
                {
                    UnityEngine.Debug.LogError("Error adding user: " + task.Exception);
                }
            });
        }
    }
}
