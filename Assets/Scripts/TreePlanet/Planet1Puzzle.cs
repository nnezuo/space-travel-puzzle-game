using UnityEngine;
using UnityEngine.Events;

public class Planet1Puzzle : MonoBehaviour
{
    [Header("Result")]
    public GameObject beacon;            // hidden at the start, shown when solved

    [Header("Events (for UI / game flow)")]
    public UnityEvent<int, int> onProgress;   // (collected, total)
    public UnityEvent onCompleted;

    [Header("Old on-screen text (GameHUD replaces it: leave OFF)")]
    public bool showDebugText = false;

    public int Total { get; private set; }
    public int Collected { get; private set; }
    public bool IsComplete { get; private set; }

    string message = "";

    void Start()
    {
        if (beacon != null) beacon.SetActive(false);
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
            message = "Puzzle complete! Go to the portal.";
            if (beacon != null) beacon.SetActive(true);

            GameHUD.Say("Crystal collected! " + Collected + " / " + Total, GameHUD.Kind.Success);
            GameHUD.Banner("PORTAL OPEN", "Go to the portal and press E to travel to the next planet.", 6f);
            onCompleted?.Invoke();
        }
        else
        {
            GameHUD.Say("Crystal collected! " + Collected + " / " + Total, GameHUD.Kind.Success);
        }
    }

    void OnGUI()
    {
        if (!showDebugText) return;
        GUI.Label(new Rect(20, 20, 500, 30), message);
    }
}