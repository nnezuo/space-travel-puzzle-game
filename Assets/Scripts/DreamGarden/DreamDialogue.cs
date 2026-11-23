using System;
using System.Collections.Generic;
using UnityEngine;

// one reusable modal for conversations, inscriptions and typed riddles
public sealed class DreamDialogue : MonoBehaviour
{
    public bool IsOpen { get; private set; }
    private string speaker, answer = "", feedback = "";
    private string[] pages;
    private int pageIndex;
    private Func<string, bool> validateAnswer;
    private Action completionCallback;
    private readonly List<Behaviour> pausedBehaviours = new List<Behaviour>();
    private Rigidbody body;
    private bool wasKinematic, cursorVisible;
    private CursorLockMode cursorLock;
    private GUIStyle heading, prose, input, button;
    private bool focusAnswer;
    private Vector2 scroll;

    public void Open(Transform player, string name, string[] lines,
        Func<string, bool> checkAnswer = null, Action onCompleted = null)
    {
        if (IsOpen || player == null || lines == null || lines.Length == 0) return;
        speaker = name;
        pages = lines;
        pageIndex = 0;
        answer = "";
        feedback = "";
        validateAnswer = checkAnswer;
        completionCallback = onCompleted;
        scroll = Vector2.zero;
        // Capture cursor and enabled controls once; Close restores exactly this state.
        cursorLock = Cursor.lockState;
        cursorVisible = Cursor.visible;
        PausePlayer(player);
        IsOpen = true;
        focusAnswer = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void PausePlayer(Transform player)
    {
        Pause(player.GetComponent<PlayerController>());
        Pause(player.GetComponent<PlayerAttack>());
        foreach (PlanetCamera camera in FindObjectsByType<PlanetCamera>())
        {
            if (camera.target != player) continue;
            Pause(camera);
            Pause(camera.GetComponent<DreamCameraOcclusion>());
        }
        body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            wasKinematic = body.isKinematic;
            if (!wasKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
        }
    }

    private void Pause(Behaviour behaviour)
    {
        // Only re-enable controls this dialogue disabled, not controls already off.
        if (behaviour == null || !behaviour.enabled) return;
        pausedBehaviours.Add(behaviour);
        behaviour.enabled = false;
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        foreach (Behaviour behaviour in pausedBehaviours)
            if (behaviour != null) behaviour.enabled = true;
        pausedBehaviours.Clear();
        if (body != null) body.isKinematic = wasKinematic;
        Cursor.lockState = cursorLock;
        Cursor.visible = cursorVisible;
        validateAnswer = null;
        completionCallback = null;
    }

    private void OnDisable() => Close();
    private void Update()
    {
        if (IsOpen && (Input.GetKeyDown(KeyCode.Escape) || DreamSceneFlow.Busy)) Close();
    }

    private void Advance()
    {
        if (pageIndex < pages.Length - 1)
        {
            pageIndex++;
            scroll = Vector2.zero;
            focusAnswer = true;
            return;
        }
        if (validateAnswer == null)
        {
            Close();
            return;
        }
        if (!validateAnswer(answer))
        {
            feedback = "The water remains still. Listen to what is left when every voice has gone.";
            focusAnswer = true;
            return;
        }
        CompleteRiddle();
    }

    private void CompleteRiddle()
    {
        // Restore controls before notifying the puzzle; Close clears the stored callback.
        Action callback = completionCallback;
        Close();
        callback?.Invoke();
    }

    private void OnGUI()
    {
        if (!IsOpen) return;
        if (heading == null) InitializeStyles();
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        int oldDepth = GUI.depth;
        GUI.depth = -100;
        float scale = Mathf.Min(Screen.width / 1000f, Screen.height / 700f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1000 * scale) / 2,
            (Screen.height - 700 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
        DrawDialogueContents();
        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
        GUI.depth = oldDepth;
    }

    private void InitializeStyles()
    {
        heading = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
        heading.normal.textColor = new Color(1f, .84f, .59f);
        prose = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true };
        prose.normal.textColor = new Color(.92f, .9f, .96f);
        input = new GUIStyle(GUI.skin.textField) { fontSize = 21, padding = new RectOffset(12, 12, 9, 9) };
        button = new GUIStyle(GUI.skin.button) { fontSize = 18 };
    }

    private void DrawDialogueContents()
    {
        DrawPanel(new Rect(0, 0, 1000, 700), new Color(.015f, .012f, .04f, .62f));
        DrawPanel(new Rect(90, 226, 820, 432), new Color(.065f, .06f, .11f, .98f));
        DrawPanel(new Rect(90, 226, 820, 3), new Color(.72f, .55f, .32f));
        GUI.Label(new Rect(120, 248, 650, 42), speaker, heading);
        GUI.Label(new Rect(120, 294, 650, 26), "THE FORGOTTEN DREAM GARDEN", GUI.skin.label);
        scroll = GUI.BeginScrollView(new Rect(120, 332, 760, 142), scroll,
            new Rect(0, 0, 735, Mathf.Max(135, prose.CalcHeight(new GUIContent(pages[pageIndex]), 725))));
        GUI.Label(new Rect(0, 0, 725, Mathf.Max(135, prose.CalcHeight(new GUIContent(pages[pageIndex]), 725))), pages[pageIndex], prose);
        GUI.EndScrollView();
        bool riddle = pageIndex == pages.Length - 1 && validateAnswer != null;
        if (riddle)
        {
            GUI.SetNextControlName("dream answer");
            answer = GUI.TextField(new Rect(120, 483, 520, 46), answer, 64, input);
            if (focusAnswer && Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl("dream answer");
                focusAnswer = false;
            }
            GUI.Label(new Rect(120, 536, 750, 52), feedback, prose);
        }
        if (GUI.Button(new Rect(650, 592, 230, 44), riddle ? "Restore memory" : pageIndex < pages.Length - 1 ? "Continue" : "Close", button)) Advance();
        if (GUI.Button(new Rect(120, 592, 180, 44), "Leave [Esc]", button)) Close();
        if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
        {
            Advance();
            Event.current.Use();
        }
    }

    private static void DrawPanel(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
