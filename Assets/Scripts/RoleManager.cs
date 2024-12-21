using System.Collections.Generic;
using UnityEngine;

public class RoleManager : MonoBehaviour
{
    private static RoleManager _instance;
    private Dictionary<string, string> userRoles = new Dictionary<string, string>(); // Stores roles by userId

    public static RoleManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject roleManager = new GameObject("RoleManager");
                _instance = roleManager.AddComponent<RoleManager>();
                DontDestroyOnLoad(roleManager);
            }
            return _instance;
        }
    }

    public void SetRole(string userId, string role)
    {
        if (userRoles.ContainsKey(userId))
        {
            userRoles[userId] = role;
        }
        else
        {
            userRoles.Add(userId, role);
        }

        Debug.Log($"RoleManager: Set role for UserID {userId} as {role}");
    }

    public string GetRole(string userId)
    {
        if (userRoles.TryGetValue(userId, out string role))
        {
            return role;
        }

        Debug.LogWarning($"RoleManager: Role not found for UserID {userId}. Defaulting to 'Student'.");
        return "Student"; // Default role
    }
}
