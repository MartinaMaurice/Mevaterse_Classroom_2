using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using Firebase.Firestore;
using Photon.Pun;
using System.Threading.Tasks;

public class LectureSelector : MonoBehaviourPunCallbacks
{
    public TMP_Dropdown lectureDropdown;  // TMP_Dropdown for TextMeshPro
    public BoardController boardController;  // Reference to the BoardController to load slides on the board
    public TabletManager tabletManager;  // Reference to TabletManager


    private FirebaseFirestore db;
    private string imagesBasePath;

    // Lists to store quiz display names and IDs
    private List<string> quizTitles = new List<string>();
    private List<string> quizIds = new List<string>();

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        imagesBasePath = Path.Combine(Application.dataPath, "Resources", "Images");
        if (gameObject.activeSelf)
        {
            PopulateLectureDropdown();
        }
    }

    async void PopulateLectureDropdown()
    {
        // Fetch folder names from local directory
        List<string> localFolderNames = GetLocalFolderNames();

        // Fetch quiz titles from Firestore
        await FetchQuizTitlesFromFirestore();

        // Combine both lists
        List<string> combinedOptions = new List<string>();
        combinedOptions.AddRange(localFolderNames);  // Local folders
        combinedOptions.AddRange(quizTitles);  // Firestore quizzes (only display titles)

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

    async Task FetchQuizTitlesFromFirestore()
    {
        // Fetch quizzes from Firestore
        QuerySnapshot quizSnapshot = await db.Collection("quizzes").GetSnapshotAsync();

        int quizNumber = 1;
        foreach (DocumentSnapshot document in quizSnapshot.Documents)
        {
            if (document.Exists)
            {
                // Create a user-friendly title for display in the dropdown
                string quizTitle = $"Quiz {quizNumber}";

                // Add title and ID to separate lists
                quizTitles.Add(quizTitle);
                quizIds.Add(document.Id);

                quizNumber++;
            }
        }
    }

    void OnLectureSelected(TMP_Dropdown dropdown)
    {
        string selectedItem = dropdown.options[dropdown.value].text;

        Debug.Log("Selected item: " + selectedItem);

        if (selectedItem.StartsWith("Exercise"))
        {
            // Handle grouping for exercises
            AssignPrivateChatsForExercise();
            Debug.Log("Exercise selected.");
        }
        else if (selectedItem.StartsWith("Quiz"))
        {
            int quizIndex = dropdown.value - (lectureDropdown.options.Count - quizTitles.Count);
            if (quizIndex >= 0 && quizIndex < quizIds.Count)
            {
                string quizId = quizIds[quizIndex];
                tabletManager.SetQuizId(quizId);  // Set the quiz ID in TabletManager
                tabletManager.SetLectureType("Quiz");
                Debug.Log("Quiz ID for selected item: " + quizId);
            }
            else
            {
                Debug.LogError("Quiz ID not found for selected item: " + selectedItem);
            }
        }
        else
        {
            boardController.LoadSlidesForLecture(selectedItem);
        }
    }

    void AssignPrivateChatsForExercise()
    {
        Debug.Log("AssignPrivateChatsForExercise() called.");

        var players = PhotonNetwork.PlayerList;

        if (players == null || players.Length == 0)
        {
            Debug.LogWarning("No players in the room.");
            return;
        }

        List<string> groupNames = GenerateGroupNames((players.Length + 1) / 2); // Calculate the number of groups
        int groupIndex = 0;

        for (int i = 0; i < players.Length; i += 2)
        {
            string groupName = groupNames[groupIndex];
            string player1Name = players[i].NickName;
            string player2Name = (i + 1 < players.Length) ? players[i + 1].NickName : "No Partner";

            Debug.Log($"Assigning Group {groupName}: {player1Name} and {player2Name}");

            // Assign group to player 1
            photonView.RPC("AssignToGroupChat", players[i], groupName, player2Name);

            // Assign group to player 2, if available
            if (i + 1 < players.Length)
            {
                photonView.RPC("AssignToGroupChat", players[i + 1], groupName, player1Name);
            }

            groupIndex++;
        }
    }

    List<string> GenerateGroupNames(int count)
    {
        List<string> groupNames = new List<string>();
        for (int i = 0; i < count; i++)
        {
            groupNames.Add("Group " + (char)('A' + i));
        }
        return groupNames;
    }

    [PunRPC]
    void AssignToGroupChat(string groupName, string partnerName)
    {
        Debug.Log($"RPC Received: Group {groupName}, Partner {partnerName}");

        // Display group assignment to the user
        Logger.Instance.LogInfo($"You are in {groupName} with {partnerName}");
    }
}
