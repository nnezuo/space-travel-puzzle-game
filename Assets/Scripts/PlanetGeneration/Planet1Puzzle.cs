
using UnityEngine;
using UnityEngine.Events;

public class Planet1Puzzle : MonoBehaviour
{
    [Header("Events (for UI / game flow)")]
    public UnityEvent<int, int> onProgress;
    public UnityEvent onCompleted;

    [Header("Temporary on-screen text")]
    public bool showDebugText = true;

    public int Total { get; private set; }
    public int Collected { get; private set; }
    public bool IsComplete { get; private set; }

    string message = "";

    void Start()
    {
        message = "Find the crystals!";
    }

    public void RegisterCrystal()
    {
        Total++;
        onProgress?.Invoke(Collected, Total);
    }

    public void CrystalCollected()
    {
        Collected++;
        message = $"Crystals: {Collected} / {Total}";
        onProgress?.Invoke(Collected, Total);

        if (Collected >= Total && Total > 0 && !IsComplete)
        {
            IsComplete = true;

            message = "MISSION COMPLETE! GO TO THE NEXT PLANET!";

            onCompleted?.Invoke();
        }
    }

    void OnGUI()
    {
        if (!showDebugText) return;

        if (IsComplete)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 28;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.cyan;

            GUI.Label(
                new Rect(20, 20, 900, 60),
                message,
                style
            );
        }
        else
        {
            GUI.Label(
                new Rect(20, 20, 500, 30),
                message
            );
        }
    }
}
