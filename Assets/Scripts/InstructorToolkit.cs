using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using System.Collections.Generic;
using System.IO;
using ExcelDataReader;
using Firebase.Extensions;
using SFB;

public class InstructorToolkit : MonoBehaviour
{
    public GameObject toolkitPanel;

    public GameObject initialUI;
    public Button addQuizButton;
    public Button addSlidesButton;
    public Button closeButton;  // Close button to return to initial UI

    private FirebaseFirestore db;

    // public Button futureOptionButton; 

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
    // void AddSlides()
    // {
    //     Debug.Log("Instructor wants to add PDF slides.");

    //     string filePath = StandaloneFileBrowser.OpenFilePanel("Select PDF", "", "pdf", false)[0];
    //     if (!string.IsNullOrEmpty(filePath))
    //     {
    //         List<Texture2D> slides = ExtractPDFPagesAsImages(filePath);
    //         SaveSlidesToFirestore(slides);
    //         SaveSlidesToLocal(slides, "lecture_pdf");
    //     }
    // }

    // // Extract PDF pages as images using iTextSharp
    // public List<Texture2D> ExtractPDFPagesAsImages(string filePath)
    // {
    //     List<Texture2D> slideImages = new List<Texture2D>();
    //     PdfReader reader = new PdfReader(filePath);

    //     for (int pageIndex = 1; pageIndex <= reader.NumberOfPages; pageIndex++)
    //     {
    //         // Render each page as an image
    //         PdfDictionary pageDict = reader.GetPageN(pageIndex);
    //         PdfDictionary resources = (PdfDictionary)PdfReader.GetPdfObject(pageDict.Get(PdfName.RESOURCES));
    //         PdfDictionary xObject = (PdfDictionary)PdfReader.GetPdfObject(resources.Get(PdfName.XOBJECT));

    //         if (xObject != null)
    //         {
    //             foreach (PdfName name in xObject.Keys)
    //             {
    //                 PdfObject obj = xObject.Get(name);
    //                 if (obj.IsIndirect())
    //                 {
    //                     PdfDictionary imgDict = (PdfDictionary)PdfReader.GetPdfObject(obj);
    //                     PdfName subtype = (PdfName)PdfReader.GetPdfObject(imgDict.Get(PdfName.SUBTYPE));
    //                     if (PdfName.IMAGE.Equals(subtype))
    //                     {
    //                         int xrefIndex = ((PRIndirectReference)obj).Number;
    //                         PdfObject pdfObj = reader.GetPdfObject(xrefIndex);
    //                         PdfStream pdfStream = (PdfStream)pdfObj;
    //                         byte[] imgBytes = PdfReader.GetStreamBytesRaw((PRStream)pdfStream);

    //                         if (imgBytes != null)
    //                         {
    //                             Texture2D slideImage = new Texture2D(2, 2);
    //                             slideImage.LoadImage(imgBytes);
    //                             slideImages.Add(slideImage);
    //                         }
    //                     }
    //                 }
    //             }
    //         }
    //     }

    //     reader.Close();
    //     return slideImages;
    // }

    // // Save slides to Firestore
    // void SaveSlidesToFirestore(List<Texture2D> slides)
    // {
    //     string slidesId = db.Collection("slides").Document().Id; // Auto-generate slides ID
    //     string userId = PlayerPrefs.GetString("UserID");

    //     Dictionary<string, object> slidesDoc = new Dictionary<string, object>
    //     {
    //         { "user_id", userId },
    //         { "slides_id", slidesId },
    //         { "lecture_number", 1 },  // This would increment accordingly
    //         { "slides", slides.Count }  // Just save the number of slides for now, Firebase doesn't store binary images.
    //     };

    //     db.Collection("slides").Document(slidesId).SetAsync(slidesDoc).ContinueWithOnMainThread(task =>
    //     {
    //         if (task.IsCompleted)
    //         {
    //             Debug.Log("Slides successfully added to Firestore.");
    //         }
    //         else
    //         {
    //             Debug.LogError("Error adding slides: " + task.Exception);
    //         }
    //     });
    // }

    // // Save slides to local storage
    // void SaveSlidesToLocal(List<Texture2D> slides, string lectureFolder)
    // {
    //     string folderPath = System.IO.Path.Combine(Application.dataPath, "Resources/Images", PlayerPrefs.GetString("UserID"), lectureFolder);
    //     if (!Directory.Exists(folderPath))
    //     {
    //         Directory.CreateDirectory(folderPath);
    //     }

    //     for (int i = 0; i < slides.Count; i++)
    //     {
    //         byte[] bytes = slides[i].EncodeToPNG();
    //         File.WriteAllBytes(System.IO.Path.Combine(folderPath, $"slide_{i + 1}.png"), bytes);
    //     }

    //     Debug.Log($"Slides saved locally to: {folderPath}");
    // }


    void AddSlides()
    {
        Debug.Log("Instructor wants to add slides.");
    }
}
