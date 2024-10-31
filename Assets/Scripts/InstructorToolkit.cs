using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using System.Collections.Generic;
using System.IO;
using ExcelDataReader;
using Firebase.Extensions;
using SFB;
using System; // Make sure to include this for IntPtr


public class InstructorToolkit : MonoBehaviour
{
    public GameObject toolkitPanel;

    public GameObject initialUI;
    public Button addQuizButton;
    public Button addSlidesButton;
    public Button closeButton;  // Close button to return to initial UI

    private FirebaseFirestore db;

    public GameObject slideDisplayPanel; // Assign in the Inspector
    int pageCount = 5; // Set this to the number of images you expect

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
        Debug.Log("Instructor wants to add a quiz.");

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
                Debug.Log("Quiz successfully added to Firestore.");
            }
            else
            {
                Debug.LogError("Error adding quiz: " + task.Exception);
            }
        });
    }
    void AddSlides()
    {
        Debug.Log("Instructor wants to add slides.");

        // Open file dialog to select PDF
        string filePath = StandaloneFileBrowser.OpenFilePanel("Select PDF", "", "pdf", false)[0];
        if (!string.IsNullOrEmpty(filePath))
        {
            string lectureName = "Lecture_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"); // Unique folder name
            string folderPath = Path.Combine(Application.persistentDataPath, "Images", lectureName);

            Directory.CreateDirectory(folderPath); // Ensure the directory exists

            // Call ConvertPDFToImages with both parameters
            List<string> slidePaths = ConvertPDFToImages(filePath, folderPath);

            // Save slides to Firebase
            SaveSlidesToFirestore(slidePaths, lectureName);

            // Save slides locally
            List<Texture2D> slidesTextures = new List<Texture2D>();
            foreach (string path in slidePaths)
            {
                slidesTextures.Add(LoadImageFromPath(path));
            }
            SaveSlidesToLocal(slidesTextures, lectureName);

            // Display slides
            DisplaySlides(slidePaths);
        }
    }


    // Simulate creating placeholder images as slides
    List<string> ConvertPDFToImages(string pdfFilePath, string saveFolderPath)
    {
        PDFToImage.Initialize(); // Initialize PDFium

        List<string> slidePaths = new List<string>();
        IntPtr document = PDFToImage.FPDF_LoadDocument(pdfFilePath, null);
        int pageCount = PDFToImage.FPDF_GetPageCount(document);

        for (int i = 0; i < pageCount; i++)
        {
            Texture2D slideImage = PDFToImage.RenderPage(pdfFilePath, i, 1024, 1024); // Adjust dimensions as needed
            if (slideImage != null)
            {
                string slidePath = Path.Combine(saveFolderPath, $"slide_{i + 1}.png");
                byte[] imageBytes = slideImage.EncodeToPNG();
                File.WriteAllBytes(slidePath, imageBytes);

                slidePaths.Add(slidePath);
            }
        }

        PDFToImage.FPDF_CloseDocument(document); // Close document after processing
        PDFToImage.Destroy(); // Cleanup PDFium

        return slidePaths;
    }


    // Save slide references to Firestore
    void SaveSlidesToFirestore(List<string> slidePaths, string lectureName)
    {
        string slidesId = db.Collection("slides").Document().Id;
        string userId = PlayerPrefs.GetString("UserID");

        Dictionary<string, object> slidesDoc = new Dictionary<string, object>
        {
            { "user_id", userId },
            { "slides_id", slidesId },
            { "lecture_name", lectureName },
            { "slides", slidePaths }
        };

        db.Collection("slides").Document(slidesId).SetAsync(slidesDoc).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Slides successfully added to Firestore.");
            }
            else
            {
                Debug.LogError("Error adding slides: " + task.Exception);
            }
        });
    }
    void SaveSlidesToLocal(List<Texture2D> slides, string lectureFolder)
    {
        // Define the folder path in Assets/Resources/Images
        string folderPath = Path.Combine(Application.dataPath, "Resources/Images", lectureFolder);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath); // Create directory if it doesn't exist
        }

        for (int i = 0; i < slides.Count; i++)
        {
            byte[] bytes = slides[i].EncodeToPNG();
            string filePath = Path.Combine(folderPath, $"slide_{i + 1}.png");
            File.WriteAllBytes(filePath, bytes); // Save each slide as a PNG
        }

        Debug.Log($"Slides saved locally to: {folderPath}");
    }

    // Display slides in Unity UI
    void DisplaySlides(List<string> slidePaths)
    {
        // Clear previous slides in UI display
        foreach (Transform child in slideDisplayPanel.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (string path in slidePaths)
        {
            Texture2D slideImage = LoadImageFromPath(path);
            GameObject slideObj = new GameObject("Slide");
            slideObj.transform.SetParent(slideDisplayPanel.transform);

            Image slideUIImage = slideObj.AddComponent<Image>();
            slideUIImage.sprite = Sprite.Create(slideImage, new Rect(0, 0, slideImage.width, slideImage.height), new Vector2(0.5f, 0.5f));
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
