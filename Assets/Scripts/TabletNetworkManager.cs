using System.Collections.Generic;
using UnityEngine;

public class TabletNetworkManager : MonoBehaviour
{
    [SerializeField] private GameObject[] allTablets;

    public void HideOtherTablets(string userId, bool isInstructor)
    {
        if (allTablets == null || allTablets.Length == 0)
        {
            Debug.LogError("[TabletNetworkManager] No tablets assigned to the allTablets array.");
            return;
        }

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("[TabletNetworkManager] Provided userId is null or empty.");
            return;
        }

        Debug.Log($"[TabletNetworkManager] Managing tablet visibility for userId: {userId}");

        foreach (GameObject tablet in allTablets)
        {
            if (tablet == null)
            {
                Debug.LogWarning("[TabletNetworkManager] Found a null tablet in the allTablets array. Skipping...");
                continue;
            }

            TabletManager tabletManager = tablet.GetComponent<TabletManager>();
            if (tabletManager == null)
            {
                Debug.LogWarning($"[TabletNetworkManager] No TabletManager component found on {tablet.name}. Skipping...");
                continue;
            }

            Debug.Log($"[TabletNetworkManager] Tablet '{tablet.name}' has UserId: {tabletManager.UserId}");

            // Visibility logic
            if (isInstructor || tabletManager.UserId == userId)
            {
                tablet.SetActive(true); // Instructor sees all tablets or keep the validated user's tablet active
                Debug.Log($"[TabletNetworkManager] Tablet '{tablet.name}' remains active.");
            }
            else
            {
                tablet.SetActive(false); // Hide other tablets for students
                Debug.Log($"[TabletNetworkManager] Tablet '{tablet.name}' is hidden.");
            }
        }
    }
}
