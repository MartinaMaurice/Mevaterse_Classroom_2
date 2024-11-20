using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;
using System.Collections.Generic;

public class TextChat : MonoBehaviourPunCallbacks
{
    public TMP_InputField inputField;
    public bool isSelected = false;
    private GameObject commandInfo;
    private Dictionary<string, string> playerGroups = new Dictionary<string, string>();

    private void Start()
    {
        commandInfo = GameObject.Find("CommandInfo");
        Debug.Log("TextChat initialized.");
    }

    public void LateUpdate()
    {
        if (Input.GetKeyUp(KeyCode.Return))
        {
            if (!isSelected)
            {
                isSelected = true;
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
                inputField.caretPosition = inputField.text.Length;
                commandInfo?.SetActive(false);
            }
            else if (isSelected && !string.IsNullOrEmpty(inputField.text))
            {
                string groupName = GetPlayerGroup(PhotonNetwork.NickName);
                photonView.RPC("SendMessageRpc", RpcTarget.AllBuffered, PhotonNetwork.NickName, inputField.text, groupName);
                inputField.text = "";
                isSelected = false;
                EventSystem.current.SetSelectedGameObject(null);
                commandInfo?.SetActive(true);
            }
        }
        else if (Input.GetKeyUp(KeyCode.Escape) && isSelected)
        {
            isSelected = false;
            EventSystem.current.SetSelectedGameObject(null);
            commandInfo?.SetActive(true);
        }
    }

    [PunRPC]
    public void SendMessageRpc(string sender, string msg, string groupName)
    {
        string currentPlayerGroup = GetPlayerGroup(PhotonNetwork.NickName);

        if (currentPlayerGroup == groupName)
        {
            string message = $"<color=\"yellow\">{sender}</color>: {msg}";
            Debug.Log($"Message received in group {groupName}: {message}");
        }
    }

    public string GetPlayerGroup(string playerName)
    {
        if (playerGroups.ContainsKey(playerName))
        {
            return playerGroups[playerName];
        }
        return "Unknown";
    }

    public void AssignToGroup(string playerName, string groupName)
    {
        playerGroups[playerName] = groupName;
        Debug.Log($"Player {playerName} assigned to group {groupName}.");
    }
}
