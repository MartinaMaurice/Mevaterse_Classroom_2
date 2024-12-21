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


public class InstructorToolkit : MonoBehaviour
{
    public GameObject toolkitPanel;
    public Button addUserButton;  // New button for adding users from Excel

    public GameObject initialUI;
    public Button addQuizButton;
    public Button addSlidesButton;
    public Button closeButton;

    public Button closeButtonDeb;

    public Button addDebuggingButton;

    public Button assignmentButton;

    public Button exerciseButton;

    private FirebaseFirestore db;

    public GameObject slideDisplayPanel;

    public GameObject debuggingPanel;
    public string pythonPath;

    void Start()
    {
        toolkitPanel.SetActive(false);
        db = FirebaseFirestore.DefaultInstance;
        pythonPath = @"C:\Users\Martina\OneDrive\Documents\GitHub\Mevaterse_Classroom_2\python\Scripts\python.exe";
        UnityDebug.Log($"Python Path: {pythonPath}");
        VerifyPythonEnvironment();
        addQuizButton.onClick.AddListener(AddQuiz);
        addSlidesButton.onClick.AddListener(AddSlides);
        addDebuggingButton.onClick.AddListener(OpenDebuggingPanel);

        closeButton.onClick.AddListener(CloseToolkit);
        closeButtonDeb.onClick.AddListener(CloseDebuggingPanel);


        assignmentButton.onClick.AddListener(() => AddDebuggingMaterial("Assignment"));
        exerciseButton.onClick.AddListener(() => AddDebuggingMaterial("Exercise"));

        // futureOptionButton.onClick.AddListener(FutureFeature);
        addUserButton.onClick.AddListener(AddUsersFromExcel);
    }
    void VerifyPythonEnvironment()
    {
        if (!File.Exists(pythonPath))
        {
            UnityDebug.LogError($"Python executable not found at {pythonPath}");
        }
    }
    public void ToggleToolkit()
    {
        UnityDebug.Log($"Python Path: {pythonPath}");

        bool isActive = toolkitPanel.activeSelf;
        if (!isActive)
        {
            toolkitPanel.SetActive(true);
            initialUI.SetActive(false);  // Hide the initial UI
        }
        else
        {
            CloseToolkit();
        }
    }

    void CloseToolkit()
    {
        // Hide the toolkit and return to the initial UI
        toolkitPanel.SetActive(false);
        initialUI.SetActive(true);  // Show the initial UI
    }

    void CloseDebuggingPanel()
    {
        debuggingPanel.SetActive(false);
        toolkitPanel.SetActive(true);
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

        // Check if any images were generated
        if (slidePaths != null && slidePaths.Count > 0)
        {
            UnityEditor.AssetDatabase.Refresh(); // Force Unity to refresh assets

            // Display slides and save them to Firestore, but only once
            DisplaySlides(slidePaths);
            SaveSlidesToFirestore(lectureNumber, slidePaths);  // Ensure this is only called once
        }
        else
        {
            UnityEngine.Debug.LogError("No slides were generated.");
        }
    }

    void SaveSlidesToFirestore(int lectureNumber, List<string> slidePaths)
    {
        string lectureId = "Lecture_" + lectureNumber;

        // Check if the lecture data already exists in Firestore to prevent duplicates
        db.Collection("Lectures").Document(lectureId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Result.Exists)
                {
                    UnityEngine.Debug.Log($"Lecture {lectureId} already exists in Firestore. Skipping save.");
                }
                else
                {
                    // Prepare data to save
                    Dictionary<string, object> lectureData = new Dictionary<string, object>
                    {
                    { "lectureNumber", lectureNumber },
                    { "slidePaths", slidePaths } // Saving the list of paths directly
                    };

                    // Save to Firestore
                    db.Collection("Lectures").Document(lectureId).SetAsync(lectureData).ContinueWithOnMainThread(saveTask =>
                    {
                        if (saveTask.IsCompleted)
                        {
                            UnityEngine.Debug.Log($"Lecture {lectureId} and slide paths saved successfully in Firestore.");
                        }
                        else
                        {
                            UnityEngine.Debug.LogError($"Error saving lecture data to Firestore: {saveTask.Exception}");
                        }
                    });
                }
            }
            else
            {
                UnityEngine.Debug.LogError("Error checking Firestore document existence.");
            }
        });
    }

    List<string> RunPythonScript(string pdfPath, string outputFolder)
    {
        List<string> slidePaths = new List<string>();
        string scriptPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Assets/convert_pdf.py"));

        UnityEngine.Debug.Log($"Python Path: {pythonPath}");
        UnityEngine.Debug.Log($"Script Path: {scriptPath}");
        UnityEngine.Debug.Log($"PDF Path: {pdfPath}");
        UnityEngine.Debug.Log($"Output Folder: {outputFolder}");

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"\"{scriptPath}\" \"{pdfPath}\" \"{outputFolder}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(scriptPath)
        };

        try
        {
            using (Process process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                UnityEngine.Debug.Log($"Python Output: {output}");
                if (!string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError($"Python Error: {error}");
                }

                process.WaitForExit();

                // Parse valid paths from Python output
                foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmedLine = line.Trim();
                    if (System.IO.File.Exists(trimmedLine))
                    {
                        slidePaths.Add(trimmedLine);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"File not found in Python output: {trimmedLine}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to run Python script: {e.Message}");
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
   // Reads user data from the selected Excel file
List<Dictionary<string, object>> ReadUserExcelFile(string filePath)
{
    List<Dictionary<string, object>> userData = new List<Dictionary<string, object>>();

    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
    {
        using (var reader = ExcelReaderFactory.CreateReader(stream))
        {
            // Skip header row
            if (!reader.Read())
            {
                UnityDebug.LogError("Excel file is empty or missing headers.");
                return userData; // Return empty list if no headers
            }

            while (reader.Read())
            {
                // Check if the row has at least 4 columns
                if (reader.FieldCount < 4)
                {
                    UnityDebug.LogWarning($"Skipping row with insufficient columns. Found {reader.FieldCount}, expected at least 4.");
                    continue;
                }

                // Check if the required fields are not null
                if (reader.GetValue(0) == null || reader.GetValue(1) == null || reader.GetValue(2) == null || reader.GetValue(3) == null)
                {
                    UnityDebug.LogWarning("Skipping row with missing data. One or more required fields are null.");
                    continue;
                }

                var userEntry = new Dictionary<string, object>
                {
                    { "name", reader.GetValue(0)?.ToString() },
                    { "id", reader.GetValue(1)?.ToString() },
                    { "course_id", reader.GetValue(2)?.ToString() },
                    { "role", reader.GetValue(3)?.ToString() }
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