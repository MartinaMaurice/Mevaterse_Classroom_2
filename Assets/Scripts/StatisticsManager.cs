using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

public class StatisticsManager : MonoBehaviour
{
    [SerializeField] private TabletManager[] tabletManagers;
    [SerializeField] private LeaderboardManager leaderboardManager;
    [SerializeField] private ConnectToServer connectToServer;
    [SerializeField] private TabletNetworkManager tabletNetworkManager;

    private FirebaseFirestore db;
    private Dictionary<string, ActionStatistics> sessionStats = new Dictionary<string, ActionStatistics>();
    private Dictionary<string, HeatmapStatistics> heatmapStats = new Dictionary<string, HeatmapStatistics>();
    private List<string> sessionFlow = new List<string>(); // Tracks user actions in sequence
    private List<string> errorLog = new List<string>(); // Tracks errors for overflow

    private DateTime sessionStartTime;
    private string userId;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        sessionStartTime = DateTime.Now;

        // Hook into events from managers
        foreach (var tabletManager in tabletManagers)
        {
            if (tabletManager != null)
            {
                tabletManager.OnActionTriggered += LogAction;
            }
        }

        if (leaderboardManager != null)
        {
            leaderboardManager.OnActionTriggered += LogAction;
        }

        if (connectToServer != null)
        {
            connectToServer.OnActionTriggered += LogAction;
        }

        if (tabletNetworkManager != null)
        {
            tabletNetworkManager.OnTabletActionTriggered += LogAction;
        }
    }

    public void StartSession(string userId)
    {
        this.userId = userId;
        sessionStats.Clear();
        heatmapStats.Clear();
        sessionFlow.Clear();
        errorLog.Clear();
        sessionStartTime = DateTime.Now;

        Debug.Log($"Session started for user: {userId}");
    }

    public void EndSession()
    {
        if (!string.IsNullOrEmpty(userId))
        {
            SaveSessionStatisticsToFirebase();
            SaveHeatmapStatisticsToFirebase();
            SaveSessionFlowToFirebase();
            SaveErrorLogToFirebase();
        }
        else
        {
            Debug.LogError("User ID is not set. Cannot save session statistics.");
        }
    }

    private void LogAction(string managerName, string actionName)
    {
        string timestamp = DateTime.Now.ToString("o");
        sessionFlow.Add($"{timestamp}: {actionName} by {managerName}");

        // Track session statistics
        if (!sessionStats.ContainsKey(actionName))
        {
            sessionStats[actionName] = new ActionStatistics
            {
                ManagerName = managerName,
                ActionName = actionName,
                TriggerCount = 0,
                FirstTriggered = DateTime.Now,
                LastTriggered = DateTime.Now
            };
        }

        var stats = sessionStats[actionName];
        stats.TriggerCount++;
        stats.LastTriggered = DateTime.Now;

        // Track heatmap statistics
        if (!heatmapStats.ContainsKey(actionName))
        {
            heatmapStats[actionName] = new HeatmapStatistics
            {
                Location = actionName,
                VisitCount = 0
            };
        }

        heatmapStats[actionName].VisitCount++;
    }

    public void LogError(string errorMessage)
    {
        string timestamp = DateTime.Now.ToString("o");
        errorLog.Add($"{timestamp}: {errorMessage}");
        Debug.LogError(errorMessage);
    }

    private void SaveSessionStatisticsToFirebase()
    {
        var sessionData = new Dictionary<string, object>
        {
            { "user_id", userId },
            { "session_start", sessionStartTime.ToString("o") },
            { "session_end", DateTime.Now.ToString("o") },
            { "actions", SerializeSessionStats() }
        };

        db.Collection("session_statistics").Document($"{userId}_{DateTime.Now:yyyyMMddHHmmss}").SetAsync(sessionData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"Session statistics saved for user {userId}");
            }
            else
            {
                Debug.LogError($"Failed to save session statistics: {task.Exception}");
            }
        });
    }

    private void SaveHeatmapStatisticsToFirebase()
    {
        foreach (var entry in heatmapStats)
        {
            var heatmapData = new Dictionary<string, object>
            {
                { "location", entry.Value.Location },
                { "visit_count", entry.Value.VisitCount }
            };

            db.Collection("heatmap_statistics").Document(entry.Key).SetAsync(heatmapData).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($"Heatmap data saved for location {entry.Key}");
                }
                else
                {
                    Debug.LogError($"Failed to save heatmap data: {task.Exception}");
                }
            });
        }
    }

    private void SaveSessionFlowToFirebase()
    {
        var flowData = new Dictionary<string, object>
        {
            { "user_id", userId },
            { "flow", sessionFlow }
        };

        db.Collection("session_flow").Document($"{userId}_{DateTime.Now:yyyyMMddHHmmss}").SetAsync(flowData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"Session flow saved for user {userId}");
            }
            else
            {
                Debug.LogError($"Failed to save session flow: {task.Exception}");
            }
        });
    }

    private void SaveErrorLogToFirebase()
    {
        var errorData = new Dictionary<string, object>
        {
            { "user_id", userId },
            { "errors", errorLog }
        };

        db.Collection("error_logs").Document($"{userId}_{DateTime.Now:yyyyMMddHHmmss}").SetAsync(errorData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"Error log saved for user {userId}");
            }
            else
            {
                Debug.LogError($"Failed to save error log: {task.Exception}");
            }
        });
    }

    private Dictionary<string, object> SerializeSessionStats()
    {
        var serialized = new Dictionary<string, object>();

        foreach (var entry in sessionStats)
        {
            var stats = entry.Value;
            serialized[entry.Key] = new Dictionary<string, object>
            {
                { "manager_name", stats.ManagerName },
                { "trigger_count", stats.TriggerCount },
                { "first_triggered", stats.FirstTriggered.ToString("o") },
                { "last_triggered", stats.LastTriggered.ToString("o") }
            };
        }

        return serialized;
    }

    private class ActionStatistics
    {
        public string ManagerName { get; set; }
        public string ActionName { get; set; }
        public int TriggerCount { get; set; }
        public DateTime FirstTriggered { get; set; }
        public DateTime LastTriggered { get; set; }
    }

    private class HeatmapStatistics
    {
        public string Location { get; set; }
        public int VisitCount { get; set; }
    }
}
