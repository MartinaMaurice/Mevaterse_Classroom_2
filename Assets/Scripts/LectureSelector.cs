using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using Firebase.Firestore;
using System.Threading.Tasks;

public class LectureSelector : MonoBehaviour
{
    public TMP_Dropdown lectureDropdown; // TMP_Dropdown for TextMeshPro
    public BoardController boardController; // Reference to the BoardController to load slides on the board
    public TabletManager tabletManager; // Reference to TabletManager
    public IDEManager iDEManager; // Reference to IDEManager for Exercise/Assignment logic

    private FirebaseFirestore db;
    private string imagesBasePath;

    // Lists to store quiz display names and IDs
    private List<string> quizTitles = new List<string>();
    private List<string> quizIds = new List<string>();

    void Start()
    {
        // Ensure Firebase and other components are properly initialized
        if (lectureDropdown == null || tabletManager == null || boardController == null)
        {
            Debug.LogError("One or more references are missing. Please assign all references in the Inspector.");
            return;
        }

        db = FirebaseFirestore.DefaultInstance;
        imagesBasePath = Path.Combine(Application.dataPath, "Resources", "Images");

        PopulateLectureDropdown();
    }

    /// <summary>
    /// Populates the lecture dropdown with a combination of local folder names and quizzes from Firestore.
    /// </summary>
    async void PopulateLectureDropdown()
    {
        // Fetch local folder names
        List<string> localFolderNames = GetLocalFolderNames();

        // Fetch quiz titles from Firestore
        await FetchQuizTitlesFromFirestore();

        // Combine both lists
        List<string> combinedOptions = new List<string>();
        combinedOptions.AddRange(localFolderNames);
        combinedOptions.AddRange(quizTitles);

        // Populate the dropdown
        lectureDropdown.ClearOptions();
        lectureDropdown.AddOptions(combinedOptions);

        // Assign a value change listener
        lectureDropdown.onValueChanged.AddListener(delegate { OnLectureSelected(lectureDropdown); });
    }

    /// <summary>
    /// Retrieves local folder names from Resources/Images.
    /// </summary>
    /// <returns>List of local folder names.</returns>
    List<string> GetLocalFolderNames()
    {
        List<string> folderNames = new List<string>();

        // Ensure the base path exists
        if (Directory.Exists(imagesBasePath))
        {
            foreach (string dir in Directory.GetDirectories(imagesBasePath))
            {
                string dirName = Path.GetFileName(dir);

                // Include folders starting with specific prefixes
                if (dirName.StartsWith("Lecture") || dirName.StartsWith("Assignment") || dirName.StartsWith("Quiz") || dirName.StartsWith("Exercise"))
                {
                    folderNames.Add(dirName);
                }
            }
        }
        else
        {
            Debug.LogWarning("Images base path not found: " + imagesBasePath);
        }

        return folderNames;
    }

    /// <summary>
    /// Fetches quiz titles and IDs from Firestore and stores them for use.
    /// </summary>
    async Task FetchQuizTitlesFromFirestore()
    {
        try
        {
            QuerySnapshot quizSnapshot = await db.Collection("quizzes").GetSnapshotAsync();

            quizTitles.Clear();
            quizIds.Clear();

            foreach (DocumentSnapshot document in quizSnapshot.Documents)
            {
                if (document.Exists)
                {
                    string quizTitle = document.ContainsField("title") ? document.GetValue<string>("title") : $"Quiz {quizTitles.Count + 1}";
                    quizTitles.Add(quizTitle);
                    quizIds.Add(document.Id);
                }
            }

            if (quizTitles.Count == 0)
            {
                Debug.LogWarning("No quizzes found in Firestore.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error fetching quizzes from Firestore: " + ex.Message);
        }
    }

    /// <summary>
    /// Handles lecture selection from the dropdown.
    /// </summary>
    /// <param name="dropdown">The TMP_Dropdown object.</param>
    void OnLectureSelected(TMP_Dropdown dropdown)
    {
        if (dropdown == null || dropdown.options.Count == 0)
        {
            Debug.LogError("Dropdown is empty or null.");
            return;
        }

        string selectedItem = dropdown.options[dropdown.value].text;
        int selectedIndex = dropdown.value;

        Debug.Log("Selected item: " + selectedItem);

        if (selectedItem.StartsWith("Quiz"))
        {
            // Calculate quiz index relative to combined options
            int quizIndex = selectedIndex - (dropdown.options.Count - quizTitles.Count);

            if (quizIndex >= 0 && quizIndex < quizIds.Count)
            {
                string quizId = quizIds[quizIndex];

                // Assign quiz ID to the static array in TabletManager
                TabletManager.SelectedQuizArray[0] = quizId;

                // Set lecture type in TabletManager
                tabletManager.SetLectureType("Quiz");

                Debug.Log($"Quiz selected: {selectedItem}, ID: {quizId}");
            }
            else
            {
                Debug.LogError("Quiz ID not found for the selected item.");
            }
        }
        else if (selectedItem.StartsWith("Lecture"))
        {
            boardController.LoadSlidesForLecture(selectedItem);
            tabletManager.SetLectureType("Lecture");
            Debug.Log("Lecture selected: " + selectedItem);
        }
        else if (selectedItem.StartsWith("Exercise"))
        {
            boardController.LoadSlidesForLecture(selectedItem);
            tabletManager.SetLectureType("Exercise");
            iDEManager.SetLectureType("Exercise");
            Debug.Log("Exercise selected: " + selectedItem);
        }
        else if (selectedItem.StartsWith("Assignment"))
        {
            boardController.LoadSlidesForLecture(selectedItem);
            tabletManager.SetLectureType("Assignment");
            iDEManager.SetLectureType("Assignment");
            Debug.Log("Assignment selected: " + selectedItem);
        }
        else
        {
            Debug.LogWarning("Selected item does not match expected prefixes: " + selectedItem);
        }
    }
}
