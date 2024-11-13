using System.Diagnostics;
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
    [SerializeField] private GameObject IDEPanel; // Panel with IDE and Quiz buttons

    private FirebaseFirestore db;
    public TabletManager tabletManager;

    private string serverUrl = "http://localhost:5000/run";  // Use "localhost" explicitly
    private Process serverProcess;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        runButton.onClick.AddListener(OnRunCode);
        submitButton.onClick.AddListener(OnSubmitCode);
        selectionPanel.SetActive(false);
        UnityEngine.Debug.Log("IDEManager Start");

        StartServer();
    }

    // Method to send code to server for execution
    private void OnRunCode()
    {
        string userCode = codeInputField.text;
        UnityEngine.Debug.Log("Code being sent to server");
        StartCoroutine(SendCodeToServer(userCode));
    }

    private void StartServer()
    {

        string pythonPath = @"C:/Users/Martina/AppData/Local/Programs/Python/Python313/python.exe";

        string serverScriptPath = @"c:/Users/Martina/OneDrive/Documents/GitHub/Mevaterse_Classroom_2/Assets/server.py";

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = serverScriptPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true  // Run without showing a command prompt window
        };

        try
        {
            serverProcess = new Process { StartInfo = startInfo };
            serverProcess.OutputDataReceived += (sender, args) => UnityEngine.Debug.Log("Server Output: " + args.Data);
            serverProcess.ErrorDataReceived += (sender, args) => UnityEngine.Debug.LogError("Server Error: " + args.Data);
            serverProcess.Start();
            serverProcess.BeginOutputReadLine();
            serverProcess.BeginErrorReadLine();
            UnityEngine.Debug.Log("Server started successfully.");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Failed to start server: " + e.Message);
        }
    }
    private void OnApplicationQuit()
    {
        // Ensure the server process is terminated when Unity quits
        if (serverProcess != null && !serverProcess.HasExited)
        {
            serverProcess.Kill();
            UnityEngine.Debug.Log("Server process terminated.");
        }
    }
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
            UnityEngine.Debug.Log("Code executed successfully, output: " + request.downloadHandler.text);
        }
        else
        {
            outputText.text = "Error:\n" + request.error;
            UnityEngine.Debug.LogError("Error in code execution: " + request.error);
        }
    }
    private void OnSubmitCode()
    {
        UnityEngine.Debug.Log("Attempting to submit code to Firestore.");

        if (tabletManager == null || string.IsNullOrEmpty(tabletManager.UserId))
        {
            UnityEngine.Debug.LogError("User ID is not set. Cannot save code.");
            return;
        }

        string userCode = codeInputField.text;
        string codeOutput = outputText.text;
        string userId = tabletManager.UserId;

        UnityEngine.Debug.Log("Code and output ready for submission. User ID: " + userId);

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
                    UnityEngine.Debug.Log("Code successfully saved for user.");
                }
                else
                {
                    UnityEngine.Debug.LogError("Error saving code to Firestore: " + task.Exception);
                }
            });

        IDEPanel.SetActive(false);
        selectionPanel.SetActive(true);
    }
}