
using System;
using UnityEngine;

public class TabletNetworkManager : MonoBehaviour
{
    [SerializeField] private GameObject[] allTablets;


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

  
}
