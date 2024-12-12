
using System;
using UnityEngine;

public class TabletNetworkManager : MonoBehaviour
{
    [SerializeField] private GameObject[] allTablets;

    public event Action<string, string> OnTabletActionTriggered;

    public void HideOtherTablets(string userId)
    {
        foreach (GameObject tablet in allTablets)
        {
            TabletManager tabletManager = tablet.GetComponent<TabletManager>();
            if (tabletManager != null && tabletManager.UserId != userId)
            {
                tablet.SetActive(false);
            }
        }
    }

    private void TriggerAction(string actionName, string details)
    {
        OnTabletActionTriggered?.Invoke(this.GetType().Name, actionName);
        Debug.Log($"Action Triggered: {actionName}, Details: {details}");
    }
}
