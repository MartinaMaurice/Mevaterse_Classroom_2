using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;
using System.Collections.Generic;

public class TextChat : MonoBehaviourPunCallbacks
{
    public TMP_InputField inputField;  // The chat input field
    public bool isSelected = false;   // Tracks if the input field is active
    private GameObject commandInfo;   // Command info object (can be toggled on/off)
    private Dictionary<string, string> playerGroups = new Dictionary<string, string>(); // Map of player to group

    private void Start()
    {
        commandInfo = GameObject.Find("CommandInfo");
        Debug.Log("TextChat initialized.");
    }

    public void LateUpdate()
    {
        // Handle Enter key to toggle chat input
        if (Input.GetKeyUp(KeyCode.Return))
        {
            if (!isSelected)
            {
                isSelected = true;
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
                inputField.caretPosition = inputField.text.Length;
                commandInfo?.SetActive(false); // Hide command info
                Debug.Log("Chat input field selected.");
            }
            else if (isSelected && !string.IsNullOrEmpty(inputField.text))
            {
                string groupName = GetPlayerGroup(PhotonNetwork.NickName); // Get the player's group
                photonView.RPC("SendMessageRpc", RpcTarget.AllBuffered, PhotonNetwork.NickName, inputField.text, groupName);
                Debug.Log($"Message sent: {inputField.text}");
                inputField.text = ""; // Clear the input field
                isSelected = false;
                EventSystem.current.SetSelectedGameObject(null); // Deselect input field
                commandInfo?.SetActive(true); // Show command info
            }
        }
        else if (Input.GetKeyUp(KeyCode.Escape) && isSelected)
        {
            isSelected = false;
            EventSystem.current.SetSelectedGameObject(null);
            commandInfo?.SetActive(true);
            Debug.Log("Chat input field deselected.");
        }
    }

    [PunRPC]
    public void SendMessageRpc(string sender, string msg, string groupName)
    {
        string currentPlayerGroup = GetPlayerGroup(PhotonNetwork.NickName); // Get this player's group

        // Display messages only for the group the player belongs to
        if (currentPlayerGroup == groupName)
        {
            string message = $"<color=\"yellow\">{sender}</color>: {msg}";
            Logger.Instance.LogInfo(message);
            LogManager.Instance.LogInfo($"{sender} wrote in group {groupName}: \"{msg}\"");
            Debug.Log($"Message received in group {groupName}: {message}");
        }
    }

    public string GetPlayerGroup(string playerName)
    {
        if (playerGroups.ContainsKey(playerName))
        {
            return playerGroups[playerName];
        }
        Debug.LogWarning($"Player {playerName} does not have an assigned group.");
        return "Unknown";
    }

    public void AssignToGroup(string playerName, string groupName)
    {
        if (!playerGroups.ContainsKey(playerName))
        {
            playerGroups[playerName] = groupName;
            Debug.Log($"Player {playerName} assigned to group {groupName}.");
        }
    }
}
