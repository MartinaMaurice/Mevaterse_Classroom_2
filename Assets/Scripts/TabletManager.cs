using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.IO;

public class TabletManager : MonoBehaviourPunCallbacks
{
    public TMP_InputField writeSpace;
    public GameObject displayPanel; // Panel to display lecture content on the tablet
    private GameObject commandInfo;
    private TextChat textChat;

    private bool isBeingEdited;

    private void Start()
    {
        writeSpace.readOnly = true;
        isBeingEdited = false;
        commandInfo = GameObject.Find("CommandInfo");

        writeSpace.text = $"{PhotonNetwork.NickName}'s notes, {DateTime.UtcNow.Date:MM/dd/yyyy}";
        writeSpace.caretPosition = writeSpace.text.Length;

        textChat = GameObject.Find("TextChat").GetComponent<TextChat>();
    }

    void LateUpdate()
    {
        if (Input.GetKeyUp(KeyCode.Space) && !isBeingEdited && !textChat.isSelected)
        {
            writeSpace.readOnly = false;
            EventSystem.current.SetSelectedGameObject(writeSpace.gameObject);
            Cursor.lockState = CursorLockMode.None;
            writeSpace.caretPosition = writeSpace.text.Length;
            commandInfo.SetActive(false);
            isBeingEdited = true;
        }
        else if (Input.GetKeyUp(KeyCode.Escape) && isBeingEdited)
        {
            writeSpace.readOnly = true;
            EventSystem.current.SetSelectedGameObject(null);
            Cursor.lockState = CursorLockMode.Locked;
            LogManager.Instance.SaveNotes(writeSpace.text);
            commandInfo.SetActive(true);
            isBeingEdited = false;
        }
    }

    public void DisplayContentOnTablet(string folderName)
    {
        if (displayPanel == null)
        {
            Debug.LogError("DisplayPanel is not assigned in the Inspector.");
            return;
        }

        // Clear previous content on the displayPanel (if any)
        foreach (Transform child in displayPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Load images from the selected folder
        string folderPath = Path.Combine(Application.dataPath, "Resources", "Images", folderName);
        if (Directory.Exists(folderPath))
        {
            foreach (string imagePath in Directory.GetFiles(folderPath, "*.png"))
            {
                Texture2D texture = LoadImage(imagePath);
                if (texture != null)
                {
                    GameObject imageObject = new GameObject("SlideImage");
                    imageObject.transform.SetParent(displayPanel.transform);
                    var imageComponent = imageObject.AddComponent<UnityEngine.UI.RawImage>();
                    imageComponent.texture = texture;
                }
            }
        }
        else
        {
            Debug.LogError("Folder not found: " + folderPath);
        }
    }

    private Texture2D LoadImage(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(fileData))
        {
            return texture;
        }
        return null;
    }
}
