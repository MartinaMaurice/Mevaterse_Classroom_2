using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class LectureSelector : MonoBehaviour
{
    public TMP_Dropdown lectureDropdown;  // TMP_Dropdown for TextMeshPro
    public BoardController boardController;  // Reference to the BoardController to load slides on the board
    public TabletManager tabletManager;  // Reference to TabletManager

    private string imagesBasePath;

    void Start()
    {
        // Set base path to "Resources/Images" where all folders are stored
        imagesBasePath = Path.Combine(Application.dataPath, "Resources", "Images");

        PopulateLectureDropdown();
    }

    void PopulateLectureDropdown()
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

        // Populate dropdown with folder names
        lectureDropdown.ClearOptions();
        lectureDropdown.AddOptions(folderNames);

        // Set the dropdown's value change event
        lectureDropdown.onValueChanged.AddListener(delegate { OnLectureSelected(lectureDropdown); });
    }

    void OnLectureSelected(TMP_Dropdown dropdown)
    {
        string selectedFolder = dropdown.options[dropdown.value].text;
        Debug.Log("Selected folder: " + selectedFolder);

        // Check if BoardController and TabletManager are assigned
        if (boardController != null)
        {
            boardController.LoadSlidesForLecture(selectedFolder);
        }
        else
        {
            Debug.LogError("BoardController is not assigned in the Inspector.");
        }

        if (tabletManager != null)
        {
            tabletManager.DisplayContentOnTablet(selectedFolder);
        }
        else
        {
            Debug.LogError("TabletManager is not assigned in the Inspector.");
        }
    }

}
