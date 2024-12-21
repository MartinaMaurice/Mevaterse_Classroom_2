using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

public class ActivityStatsManager : MonoBehaviour
{
    private FirebaseFirestore db;

    private Dictionary<string, int> activityCounts = new Dictionary<string, int>(); // Tracks counts of activities
    private List<string> sessionFlow = new List<string>(); // Tracks the flow of activities
    private string sessionId; // Unique session ID

    private float sessionStartTime; // Start time of the session
    private float sessionEndTime;   // End time of the session

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        sessionId = System.Guid.NewGuid().ToString(); // Generate a unique session ID
        sessionStartTime = Time.realtimeSinceStartup; // Record the session start time
        Debug.Log($"Session started. Session ID: {sessionId}");
    }

    // Increment trigger counts and add to session flow
    public void IncrementActivity(string activityName)
    {
        if (!activityCounts.ContainsKey(activityName))
        {
            activityCounts[activityName] = 0;
        }

        activityCounts[activityName]++;
        sessionFlow.Add(activityName); // Add to session flow

        Debug.Log($"Activity '{activityName}' triggered. Total triggers: {activityCounts[activityName]}");
    }

    // Generate heatmap (most frequently triggered activity)
    private Dictionary<string, object> GenerateHeatmap()
    {
        string mostTriggeredActivity = "";
        int maxTriggerCount = 0;

        foreach (var activity in activityCounts)
        {
            if (activity.Value > maxTriggerCount)
            {
                maxTriggerCount = activity.Value;
                mostTriggeredActivity = activity.Key;
            }
        }

        return new Dictionary<string, object>
        {
            { "most_triggered_activity", mostTriggeredActivity },
            { "trigger_count", maxTriggerCount }
        };
    }

    // Save statistics to Firebase
    public void SaveStatistics(string userId)
    {
        sessionEndTime = Time.realtimeSinceStartup; // Record session end time
        float sessionDuration = sessionEndTime - sessionStartTime; // Calculate total session duration

        // Prepare statistics data
        Dictionary<string, object> statsData = new Dictionary<string, object>
        {
            { "session_id", sessionId },
            { "session_duration", sessionDuration },
            { "activity_counts", activityCounts },
            { "session_flow", sessionFlow },
            { "heatmap", GenerateHeatmap() },
            { "timestamp", System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
        };

        // Save to Firebase Firestore
        db.Collection("users").Document(userId).Collection("activity_stats").Document(sessionId)
            .SetAsync(statsData, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($"Statistics saved successfully. Session duration: {sessionDuration:F2} seconds.");
                }
                else
                {
                    Debug.LogError($"Error saving statistics: {task.Exception}");
                }
            });
    }
}
