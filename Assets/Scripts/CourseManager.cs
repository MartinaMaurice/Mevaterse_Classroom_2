using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Course
{
    public string CourseName;
    public string CourseID;

    public Course(string courseName, string courseId)
    {
        CourseName = courseName;
        CourseID = courseId;
    }
}

[System.Serializable]
public class CourseListWrapper
{
    public List<Course> Courses;
}

public class CourseManager : MonoBehaviour
{
    public static CourseManager Instance { get; private set; }

    private const string CoursesKey = "SavedCourses";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);  // Keep the manager alive between scenes
    }

    // Load all saved courses from PlayerPrefs
    public List<Course> LoadAllCourses()
    {
        string json = PlayerPrefs.GetString(CoursesKey, "");
        if (string.IsNullOrEmpty(json)) return new List<Course>();

        CourseListWrapper wrapper = JsonUtility.FromJson<CourseListWrapper>(json);
        return wrapper?.Courses ?? new List<Course>();
    }

    // Save courses to PlayerPrefs
    public void SaveCourses(List<Course> courses)
    {
        CourseListWrapper wrapper = new CourseListWrapper { Courses = courses };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(CoursesKey, json);
        PlayerPrefs.Save();
        Debug.Log($"Courses saved: {json}");
    }

    // Add a new course and save it
    public void AddCourse(Course newCourse)
    {
        List<Course> courses = LoadAllCourses();
        courses.Add(newCourse);
        SaveCourses(courses);
    }

    // Check if a course with the given ID exists
    public bool CourseExists(string courseId)
    {
        List<Course> courses = LoadAllCourses();
        return courses.Exists(course => course.CourseID == courseId);
    }
}
