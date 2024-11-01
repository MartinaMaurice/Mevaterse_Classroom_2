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

    public GameObject initialUI;
    public Button addQuizButton;
    public Button addSlidesButton;
    public Button closeButton;  // Close button to return to initial UI

    private FirebaseFirestore db;

    public GameObject slideDisplayPanel; // Assign in the Inspector
    public string pythonPath = "C:\\Users\\DaWitchBtch\\AppData\\Local\\Programs\\Python\\Python311\\python.exe";

    void Start()
    {
        toolkitPanel.SetActive(false);
        db = FirebaseFirestore.DefaultInstance;

        addQuizButton.onClick.AddListener(AddQuiz);
        addSlidesButton.onClick.AddListener(AddSlides);
        closeButton.onClick.AddListener(CloseToolkit);  // Assign the close button listener

        // futureOptionButton.onClick.AddListener(FutureFeature);
    }

    public void ToggleToolkit()
    {
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
            { "name", "New Quiz" },  // You can replace this with quiz name input
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

}
