using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LectureSelector : MonoBehaviour
{
    public TMP_Dropdown lectureDropdown;  // Change to TMP_Dropdown for TextMeshPro
    public BoardController boardController;  // Reference to the BoardController to load slides on the board

    private string imagesBasePath;

    void Start()
    {
        imagesBasePath = Path.Combine(Application.dataPath, "Resources", "Images");

        PopulateLectureDropdown();
    }

    void PopulateLectureDropdown()
    {
        List<string> lectureFolders = new List<string>();

        // Get all lecture folders in the Images directory
        if (Directory.Exists(imagesBasePath))
        {
            foreach (string dir in Directory.GetDirectories(imagesBasePath))
            {
                string dirName = Path.GetFileName(dir);
                lectureFolders.Add(dirName);
            }
        }
        else
        {
            Debug.LogError("Images base path not found: " + imagesBasePath);
        }

        // Populate dropdown with lecture folder names
        lectureDropdown.ClearOptions();
        lectureDropdown.AddOptions(lectureFolders);

        // Set the dropdown's value change event
        lectureDropdown.onValueChanged.AddListener(delegate { OnLectureSelected(lectureDropdown); });
    }

    void OnLectureSelected(TMP_Dropdown dropdown)
    {
        string selectedLecture = dropdown.options[dropdown.value].text;
        Debug.Log("Selected lecture folder: " + selectedLecture);

        // Load slides for the selected lecture in BoardController
        boardController.LoadSlidesForLecture(selectedLecture);
    }
}
