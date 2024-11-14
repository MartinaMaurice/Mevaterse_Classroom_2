using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class AssignEventCamera : MonoBehaviour
{
    void Start()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
        {
            canvas.worldCamera = Camera.main;
        }
    }
}
