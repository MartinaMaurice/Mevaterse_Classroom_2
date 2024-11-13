using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using Firebase.Firestore;
using Firebase.Extensions;

public class IDEManager : MonoBehaviour
{
    //bgrb
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private TextMeshProUGUI outputText;
    [SerializeField] private Button runButton;
    [SerializeField] private Button submitButton;
    [SerializeField] private GameObject selectionPanel; // Panel with IDE and Quiz buttons
    private FirebaseFirestore db;
    public TabletManager tabletManager;

    private string serverUrl = "http://192.168.100.137:5000/run";
    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        runButton.onClick.AddListener(OnRunCode);
        submitButton.onClick.AddListener(OnSubmitCode);
        selectionPanel.SetActive(false);
        Debug.LogError("IDEManager Start");
    }

    // Method to send code to server for execution
    private void OnRunCode()
    {
        string userCode = codeInputField.text;
        Debug.LogError("Code being sent to server");
        StartCoroutine(SendCodeToServer(userCode));
    }

    // Coroutine to send code to the server and get output
    private IEnumerator SendCodeToServer(string code)
    {
        Dictionary<string, string> formData = new Dictionary<string, string>
    {
        { "code", code }
    };

        UnityWebRequest request = UnityWebRequest.Post(serverUrl, formData);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            outputText.text = request.downloadHandler.text; // Directly display the plain text
            Debug.Log("Code executed successfully, output: " + request.downloadHandler.text);
        }
        else
        {
            outputText.text = "Error:\n" + request.error;
            Debug.LogError("Error in code execution: " + request.error);
        }
    }
    private void OnSubmitCode()
    {
        Debug.Log("Attempting to submit code to Firestore.");

        if (tabletManager == null || string.IsNullOrEmpty(tabletManager.UserId))
        {
            Debug.LogError("User ID is not set. Cannot save code.");
            return;
        }

        string userCode = codeInputField.text;
        string codeOutput = outputText.text;
        string userId = tabletManager.UserId;

        Debug.Log("Code and output ready for submission. User ID: " + userId);

        // You could use a unique identifier or incrementing ID for each exercise, e.g., Exercise1, Exercise2, etc.
        string exerciseNumber = "Exercise1"; // Update this as needed for dynamic naming or pass it as a parameter

        Dictionary<string, object> codeData = new Dictionary<string, object>
    {
        { "code", userCode },
        { "output", codeOutput }
    };

        // Set the code data to the specified exercise number under the "exercises" collection
        db.Collection("users").Document(userId).Collection("exercises").Document(exerciseNumber)
            .SetAsync(codeData, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Code successfully saved for user.");
                }
                else
                {
                    Debug.LogError("Error saving code to Firestore: " + task.Exception);
                }
            });
    }
}