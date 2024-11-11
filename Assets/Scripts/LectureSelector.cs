using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using Firebase.Firestore;
using System.Threading.Tasks;

public class LectureSelector : MonoBehaviour
{
    public TMP_Dropdown lectureDropdown;  // TMP_Dropdown for TextMeshPro
    public BoardController boardController;  // Reference to the BoardController to load slides on the board
    public TabletManager tabletManager;  // Reference to TabletManager

    private FirebaseFirestore db;
    private string imagesBasePath;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        imagesBasePath = Path.Combine(Application.dataPath, "Resources", "Images");
        PopulateLectureDropdown();
    }

    async void PopulateLectureDropdown()
    {
        // Fetch folder names from local directory
        List<string> localFolderNames = GetLocalFolderNames();

        // Fetch quiz titles from Firestore
        List<string> firestoreQuizTitles = await FetchQuizTitlesFromFirestore();

        // Combine both lists
        List<string> combinedOptions = new List<string>();
        combinedOptions.AddRange(localFolderNames);  // Local folders
        combinedOptions.AddRange(firestoreQuizTitles);  // Firestore quizzes

        // Populate dropdown with combined options
        lectureDropdown.ClearOptions();
        lectureDropdown.AddOptions(combinedOptions);

        // Set the dropdown's value change event
        lectureDropdown.onValueChanged.AddListener(delegate { OnLectureSelected(lectureDropdown); });
    }

    List<string> GetLocalFolderNames()
    {
        List<string> folderNames = new List<string>();

        // Get all folders directly under the Resources/Images directory
        if (Directory.Exists(imagesBasePath))
        {
            foreach (string dir in Directory.GetDirectories(imagesBasePath))
            {
                string dirName = Path.GetFileName(dir);

                // Add folders that start with Lecture, Assignment, Quiz, or Exercise
                if (dirName.StartsWith("Lecture") || dirName.StartsWith("Assignment") || dirName.StartsWith("Quiz") || dirName.StartsWith("Exercise"))
                {
                    folderNames.Add(dirName);
                }
            }
        }
        else
        {
            Debug.LogError("Images base path not found: " + imagesBasePath);
        }

        return folderNames;
    }

    async Task<List<string>> FetchQuizTitlesFromFirestore()
    {
        List<string> quizTitles = new List<string>();
        List<string> quizIds = new List<string>();

        // Fetch quizzes from Firestore
        QuerySnapshot quizSnapshot = await db.Collection("quizzes").GetSnapshotAsync();

        int quizNumber = 1;
        foreach (DocumentSnapshot document in quizSnapshot.Documents)
        {
            if (document.Exists)
            {
                string quizTitle = $"Quiz {quizNumber}";  // You can use another field as the title if available
                quizTitles.Add(quizTitle);
                quizIds.Add(document.Id);

                quizNumber++;
            }
        }

        return quizTitles;
    }

    void OnLectureSelected(TMP_Dropdown dropdown)
    {
        string selectedItem = dropdown.options[dropdown.value].text;
        Debug.Log("Selected item: " + selectedItem);

        if (selectedItem.StartsWith("Quiz"))
        {
            // Use the index to retrieve the corresponding quiz ID
            int quizIndex = selectedIndex - (lectureDropdown.options.Count - quizTitles.Count);
            if (quizIndex >= 0 && quizIndex < quizIds.Count)
            {
                string quizId = quizIds[quizIndex];
                tabletManager.SetQuizId(quizId);  // Set the quiz ID in TabletManager
                tabletManager.SetLectureType("Quiz"); // Set lecture type as "Quiz"

                Debug.Log("Quiz ID for selected item: " + quizId);
            }
            else
            {
                Debug.LogError("Quiz ID not found for selected item: " + selectedItem);
            }
        }
        else if (selectedItem.StartsWith("Exercise"))
        {
            tabletManager.SetLectureType("Exercise"); // Set lecture type as "Exercise"
            boardController.LoadSlidesForLecture(selectedItem);

            Debug.Log("Exercise selected.");
        }
        else if (selectedItem.StartsWith("Assignment"))
        {
            tabletManager.SetLectureType("Assignment"); // Set lecture type as "Assignment"
            boardController.LoadSlidesForLecture(selectedItem);

            Debug.Log("Assignment selected.");
        }
        else if (selectedItem.StartsWith("Lecture"))
        {
            tabletManager.SetLectureType("Lecture"); // Set lecture type as "Lecture"
            boardController.LoadSlidesForLecture(selectedItem);

            Debug.Log("Lecture selected.");
        }
        else
        {
            // Local folder (Lecture, Assignment, etc.) selected
            if (boardController != null)
            {
                boardController.LoadSlidesForLecture(selectedItem);
            }
            else
            {
                Debug.LogError("BoardController is not assigned in the Inspector.");
            }
        }
    }
}