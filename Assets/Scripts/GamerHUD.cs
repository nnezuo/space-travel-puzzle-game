using System.Collections.Generic;
using UnityEngine;

// All the on-screen UI in one place: mission intro, objective list, message toasts,
// gravity keys, attack key, prompts and banners. Put it on an empty object in the scene.
public class GameHUD : MonoBehaviour
{
    public enum Kind { Info, Combat, Success, Warning }

    // ------------------------------------------------------------------
    // Static API: other scripts call these (no references needed)
    // ------------------------------------------------------------------
    static GameHUD instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => instance = null;

    // A message that slides in at the top of the screen and fades out
    public static void Say(string text, Kind kind = Kind.Info)
    {
        if (instance != null) instance.AddToast(text, kind);
        else Debug.Log(text);   // no HUD in the scene: fall back to the Console
    }

    // A hint in the middle of the screen. Call it every frame while it applies.
    public static void Prompt(string text)
    {
        if (instance == null) return;
        instance.promptText = text;
        instance.promptUntil = Time.time + 0.15f;
    }

    // A big message in the middle of the screen for a few seconds
    public static void Banner(string title, string subtitle, float seconds = 5f)
    {
        if (instance == null) return;
        instance.bannerTitle = title;
        instance.bannerSub = subtitle;
        instance.bannerStart = Time.time;
        instance.bannerEnd = Time.time + seconds;
    }

    // "Alpha1" -> "1", "F" -> "F"
    public static string KeyLabel(KeyCode k)
    {
        if (k >= KeyCode.Alpha0 && k <= KeyCode.Alpha9) return ((int)k - (int)KeyCode.Alpha0).ToString();
        if (k >= KeyCode.Keypad0 && k <= KeyCode.Keypad9) return ((int)k - (int)KeyCode.Keypad0).ToString();
        return k.ToString().ToUpper();
    }

    // ------------------------------------------------------------------
    // Settings
    // ------------------------------------------------------------------
    [Header("References (found automatically if empty)")]
    public PlayerController player;
    public GravityMode gravityMode;
    public PlayerAttack attack;
    public Planet1Puzzle puzzle;

    [Header("Intro message")]
    public bool showIntro = true;
    public float introSeconds = 12f;
    public KeyCode skipIntroKey = KeyCode.Return;
    public string introTitle = "YOUR MISSION";
    public string[] introLines =
    {
        "Find every enemy and defeat them.",
        "Touch the crystals they are guarding.",
        "Then the portal opens: travel to the next planet."
    };
    [TextArea] public string introTip = "Tip: float up with the Reversed gravity key to spot enemies and crystals from above.";

    [Header("Look")]
    public Color panelColor = new Color(0.05f, 0.07f, 0.14f, 0.80f);
    public Color accentColor = new Color(0.35f, 0.80f, 1f);
    [Range(0.6f, 1.6f)] public float uiScale = 1f;
    public float toastSeconds = 3.5f;

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------
    struct Toast { public string text; public Kind kind; public float time; }
    readonly List<Toast> toasts = new List<Toast>();

    string promptText; float promptUntil;
    string bannerTitle, bannerSub; float bannerStart, bannerEnd;
    float introStart, introEnd;
    float attackFlash = -10f;
    float nextFind;
    float uiAlpha = 1f;               // multiplies every color while drawing (for fades)

    GUIStyle labelStyle;
    readonly Dictionary<int, GUIStyle> boxStyles = new Dictionary<int, GUIStyle>();

    void Awake()
    {
        instance = this;
        introStart = Time.time;
        introEnd = Time.time + introSeconds;
        FindRefs();
    }

    void OnDestroy() { if (instance == this) instance = null; }

    void FindRefs()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (gravityMode == null) gravityMode = FindAnyObjectByType<GravityMode>();
        if (attack == null) attack = FindAnyObjectByType<PlayerAttack>();
        if (puzzle == null) puzzle = FindAnyObjectByType<Planet1Puzzle>();
    }

    void Update()
    {
        if (Time.time >= nextFind) { nextFind = Time.time + 1f; FindRefs(); }

        // Skip the intro early (it fades out in 0.6 s)
        if (showIntro && Input.GetKeyDown(skipIntroKey) && introEnd > Time.time + 0.6f)
            introEnd = Time.time + 0.6f;

        if (attack != null && Input.GetKeyDown(attack.attackKey)) attackFlash = Time.time;

        toasts.RemoveAll(t => Time.time - t.time > toastSeconds);
    }

    void AddToast(string text, Kind kind)
    {
        // Same message again within 1.5 s: just refresh it (avoids spam)
        for (int i = 0; i < toasts.Count; i++)
        {
            if (toasts[i].text == text && Time.time - toasts[i].time < 1.5f)
            {
                toasts[i] = new Toast { text = text, kind = kind, time = Time.time };
                return;
            }
        }
        toasts.Insert(0, new Toast { text = text, kind = kind, time = Time.time });
        if (toasts.Count > 5) toasts.RemoveAt(toasts.Count - 1);
    }

    // ------------------------------------------------------------------
    // Drawing
    // ------------------------------------------------------------------
    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        EnsureStyles();

        float u = Screen.height / 1000f * uiScale;   // one "unit" scales with the screen

        DrawObjective(u);
        DrawToasts(u);
        DrawBottom(u);
        DrawBanner(u);
        DrawIntro(u);

        uiAlpha = 1f;
        GUI.color = Color.white;
    }

    // ---------- Objective list (top-left) ----------
    void DrawObjective(float u)
    {
        if (!OnPuzzlePlanet()) return;

        int enemiesTotal = PlanetEnemy.Total, enemiesDone = PlanetEnemy.Defeated;
        bool enemiesOk = enemiesTotal > 0 && enemiesDone >= enemiesTotal;
        bool crystalsOk = puzzle.IsComplete;

        float x = 22 * u, y = 22 * u, w = 380 * u, rowH = 36 * u;
        float h = 50 * u + 3 * rowH + 12 * u;

        Box(new Rect(x, y, w, h), panelColor);
        Text(new Rect(x + 18 * u, y + 10 * u, w, 30 * u), "OBJECTIVE", 17 * u, accentColor, TextAnchor.MiddleLeft, true);

        float ry = y + 46 * u;
        DrawRow(x, ry, w, rowH, u, "Defeat the enemies",
                enemiesTotal > 0 ? enemiesDone + " / " + enemiesTotal : "", enemiesOk, true);
        DrawRow(x, ry + rowH, w, rowH, u, "Touch the crystals",
                puzzle.Total > 0 ? puzzle.Collected + " / " + puzzle.Total : "", crystalsOk, true);
        DrawRow(x, ry + rowH * 2, w, rowH, u, "Take the portal", "", false, crystalsOk);
    }

    void DrawRow(float x, float y, float w, float h, float u, string label, string count, bool done, bool enabled)
    {
        float s = 22 * u;
        Rect box = new Rect(x + 18 * u, y + (h - s) * 0.5f, s, s);
        Box(box, new Color(1f, 1f, 1f, enabled ? 0.30f : 0.12f));
        if (done) Box(new Rect(box.x + 4 * u, box.y + 4 * u, s - 8 * u, s - 8 * u), new Color(0.40f, 0.90f, 0.50f));

        Color c = done ? new Color(0.55f, 0.90f, 0.62f) : (enabled ? Color.white : new Color(1f, 1f, 1f, 0.40f));
        Text(new Rect(x + 52 * u, y, w - 60 * u, h), label, 21 * u, c, TextAnchor.MiddleLeft);
        if (count != "") Text(new Rect(x, y, w - 18 * u, h), count, 21 * u, c, TextAnchor.MiddleRight, true);
    }

    // ---------- Message toasts (top-center) ----------
    void DrawToasts(float u)
    {
        float y = 24 * u;
        int shown = Mathf.Min(toasts.Count, 4);

        for (int i = 0; i < shown; i++)
        {
            Toast t = toasts[i];
            float age = Time.time - t.time;
            float fadeIn = Mathf.Clamp01(age / 0.18f);
            float fadeOut = Mathf.Clamp01((toastSeconds - age) / 0.5f);
            uiAlpha = Mathf.Min(fadeIn, fadeOut);

            Vector2 size = Measure(t.text, 22 * u, false);
            float w = size.x + 74 * u, h = 46 * u;
            float x = (Screen.width - w) * 0.5f;
            float slide = (1f - fadeIn) * -16f * u;

            Box(new Rect(x, y + slide, w, h), panelColor);
            float d = 14 * u;
            Box(new Rect(x + 18 * u, y + slide + (h - d) * 0.5f, d, d), KindColor(t.kind), Mathf.FloorToInt(d * 0.5f));
            Text(new Rect(x + 44 * u, y + slide, w - 50 * u, h), t.text, 22 * u, Color.white, TextAnchor.MiddleLeft);

            y += h + 8 * u;
        }
        uiAlpha = 1f;
    }

    Color KindColor(Kind k)
    {
        switch (k)
        {
            case Kind.Combat: return new Color(1.00f, 0.62f, 0.25f);
            case Kind.Success: return new Color(0.40f, 0.90f, 0.50f);
            case Kind.Warning: return new Color(1.00f, 0.42f, 0.42f);
            default: return accentColor;
        }
    }

    // ---------- Gravity keys, attack key, prompt and altitude bar (bottom) ----------
    void DrawBottom(float u)
    {
        bool showGravity = gravityMode != null && gravityMode.Available;
        bool showAttack = attack != null && PlanetEnemy.All.Count > 0;

        float cellW = 96 * u, capS = 58 * u, gap = 26 * u, pad = 18 * u;
        int cells = (showGravity ? 3 : 0) + (showAttack ? 1 : 0);
        float panelTop = Screen.height - 20 * u;

        if (cells > 0)
        {
            float w = pad * 2 + cells * cellW + (showGravity && showAttack ? gap : 0);
            float h = 112 * u;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - h - 22 * u;
            panelTop = y;
            Box(new Rect(x, y, w, h), panelColor);

            float cx = x + pad;
            if (showGravity)
            {
                string[] names = { "Low", "Normal", "Reversed" };
                KeyCode[] keys = { gravityMode.lowKey, gravityMode.normalKey, gravityMode.reversedKey };
                GravitySource.Rule[] rules = { GravitySource.Rule.Low, GravitySource.Rule.Normal, GravitySource.Rule.Reversed };
                for (int i = 0; i < 3; i++)
                {
                    Cell(cx, y + 12 * u, cellW, capS, KeyLabel(keys[i]), names[i], gravityMode.Mode == rules[i], u);
                    cx += cellW;
                }
                if (showAttack) cx += gap;
            }
            if (showAttack)
            {
                bool inRange = EnemyInRange();
                bool lit = Time.time - attackFlash < 0.25f || inRange;
                Cell(cx, y + 12 * u, cellW, capS, KeyLabel(attack.attackKey), inRange ? "Attack!" : "Attack", lit, u);
            }

            // Altitude bar while floating: shows how close the player is to the limit
            if (showGravity && gravityMode.Mode == GravitySource.Rule.Reversed)
            {
                float bw = 280 * u, bh = 12 * u;
                Rect br = new Rect((Screen.width - bw) * 0.5f, y - 30 * u, bw, bh);
                Text(new Rect(br.x, br.y - 20 * u, bw, 18 * u), "ALTITUDE LIMIT", 13 * u,
                     new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter, true);
                Box(br, new Color(1f, 1f, 1f, 0.18f));
                float f = gravityMode.HeightFraction;
                if (f > 0.05f)
                    Box(new Rect(br.x, br.y, br.width * f, br.height),
                        Color.Lerp(accentColor, new Color(1f, 0.35f, 0.3f), f * f));
            }
        }

        // Prompt (e.g. "Press [E] to travel")
        if (Time.time < promptUntil && !string.IsNullOrEmpty(promptText))
        {
            Vector2 s = Measure(promptText, 24 * u, true);
            float w = s.x + 60 * u, h = 52 * u;
            Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.68f, w, h);
            Box(r, panelColor);
            Text(r, promptText, 24 * u, Color.white, TextAnchor.MiddleCenter, true);
        }
    }

    void Cell(float x, float y, float cellW, float capS, string key, string label, bool active, float u)
    {
        KeyCap(new Rect(x + (cellW - capS) * 0.5f, y, capS, capS), key, active, u);
        Text(new Rect(x, y + capS + 4 * u, cellW, 22 * u), label, 17 * u,
             active ? Color.white : new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter, active);
    }

    void KeyCap(Rect r, string key, bool active, float u)
    {
        Box(r, active ? accentColor : new Color(1f, 1f, 1f, 0.18f));
        Rect inner = new Rect(r.x + 3 * u, r.y + 3 * u, r.width - 6 * u, r.height - 6 * u);
        Box(inner, active ? new Color(0.92f, 0.98f, 1f) : new Color(0.10f, 0.12f, 0.20f, 0.95f));
        Text(r, key, r.height * 0.5f, active ? new Color(0.05f, 0.10f, 0.20f) : Color.white,
             TextAnchor.MiddleCenter, true);
    }

    // ---------- Big banner (center) ----------
    void DrawBanner(float u)
    {
        if (Time.time >= bannerEnd) return;
        uiAlpha = Mathf.Clamp01((Time.time - bannerStart) / 0.4f) * Mathf.Clamp01((bannerEnd - Time.time) / 0.6f);

        float w = 760 * u, h = 150 * u;
        Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.22f, w, h);
        Box(r, panelColor);
        Text(new Rect(r.x, r.y + 14 * u, w, 70 * u), bannerTitle, 50 * u, accentColor, TextAnchor.MiddleCenter, true);
        Text(new Rect(r.x + 20 * u, r.y + 84 * u, w - 40 * u, 50 * u), bannerSub, 24 * u, Color.white, TextAnchor.MiddleCenter);
        uiAlpha = 1f;
    }

    // ---------- Mission intro (big card at the start) ----------
    void DrawIntro(float u)
    {
        if (!showIntro || Time.time >= introEnd) return;

        float a = Mathf.Clamp01((Time.time - introStart) / 0.5f) * Mathf.Clamp01((introEnd - Time.time) / 0.6f);
        uiAlpha = a;

        // Dark veil over the whole screen
        Box(new Rect(0, 0, Screen.width, Screen.height), new Color(0.02f, 0.03f, 0.08f, 0.55f), 0);

        int n = introLines != null ? introLines.Length : 0;
        float w = 900 * u, pad = 40 * u;
        float h = pad + 76 * u + n * 46 * u + 20 * u + 112 * u + 76 * u + 50 * u + pad * 0.5f;
        Rect card = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f + (1f - a) * 24 * u, w, h);
        Box(card, panelColor, 18);

        float y = card.y + pad;
        Text(new Rect(card.x, y, w, 70 * u), introTitle, 56 * u, accentColor, TextAnchor.MiddleCenter, true);
        y += 76 * u;

        for (int i = 0; i < n; i++)
        {
            float d = 14 * u;
            Box(new Rect(card.x + pad + 6 * u, y + (46 * u - d) * 0.5f, d, d), accentColor, Mathf.FloorToInt(d * 0.5f));
            Text(new Rect(card.x + pad + 36 * u, y, w - pad * 2 - 36 * u, 46 * u), introLines[i], 28 * u, Color.white, TextAnchor.MiddleLeft);
            y += 46 * u;
        }
        y += 20 * u;

        // The controls, as key buttons
        KeyCode atk = attack != null ? attack.attackKey : KeyCode.F;
        KeyCode k1 = gravityMode != null ? gravityMode.lowKey : KeyCode.Alpha1;
        KeyCode k2 = gravityMode != null ? gravityMode.normalKey : KeyCode.Alpha2;
        KeyCode k3 = gravityMode != null ? gravityMode.reversedKey : KeyCode.Alpha3;

        float cell = 118 * u, cap = 56 * u;
        float cx = card.x + (w - cell * 5) * 0.5f;
        Cell(cx, y, cell, cap, KeyLabel(atk), "Attack", true, u);
        Cell(cx + cell, y, cell, cap, KeyLabel(k1), "Low", false, u);
        Cell(cx + cell * 2, y, cell, cap, KeyLabel(k2), "Normal", false, u);
        Cell(cx + cell * 3, y, cell, cap, KeyLabel(k3), "Reversed", false, u);
        Cell(cx + cell * 4, y, cell, cap, "E", "Portal", false, u);
        y += 112 * u;

        Text(new Rect(card.x + pad, y, w - pad * 2, 70 * u), introTip, 22 * u,
             new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleCenter, false, true);
        y += 76 * u;

        float blink = 0.6f + 0.4f * Mathf.Sin(Time.time * 3f);
        Text(new Rect(card.x, y, w, 40 * u), "Press " + KeyLabel(skipIntroKey) + " to start", 22 * u,
             new Color(accentColor.r, accentColor.g, accentColor.b, blink), TextAnchor.MiddleCenter, true);

        uiAlpha = 1f;
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    bool OnPuzzlePlanet()
    {
        if (puzzle == null || player == null || player.CurrentSource == null) return false;
        return player.CurrentSource.gameObject == puzzle.gameObject;   // GravitySource and puzzle are on the planet
    }

    bool EnemyInRange()
    {
        if (attack == null || player == null) return false;
        float r2 = attack.attackRange * attack.attackRange;
        foreach (PlanetEnemy e in PlanetEnemy.All)
            if (e != null && (e.transform.position - player.transform.position).sqrMagnitude <= r2) return true;
        return false;
    }

    void EnsureStyles()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.clipping = TextClipping.Overflow;
        }
    }

    // Rounded rectangle. radius < 0 = automatic
    void Box(Rect r, Color c, int radius = -1)
    {
        if (radius < 0) radius = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(r.width, r.height) * 0.25f), 2, 14);
        GUI.color = new Color(c.r, c.g, c.b, c.a * uiAlpha);
        BoxStyle(radius).Draw(r, GUIContent.none, false, false, false, false);
        GUI.color = Color.white;
    }

    GUIStyle BoxStyle(int radius)
    {
        if (!boxStyles.TryGetValue(radius, out GUIStyle st) || st.normal.background == null)
        {
            st = new GUIStyle();
            st.normal.background = MakeRounded(radius);
            st.border = new RectOffset(radius, radius, radius, radius);
            boxStyles[radius] = st;
        }
        return st;
    }

    // A white rounded square, small enough to be stretched (9-slice) to any size
    static Texture2D MakeRounded(int radius)
    {
        int size = radius * 2 + 2;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float cx = Mathf.Clamp(px, radius, size - radius);
                float cy = Mathf.Clamp(py, radius, size - radius);
                float d = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d + 0.5f)));
            }
        tex.Apply();
        return tex;
    }

    // Text with a soft shadow, so it stays readable on any background
    void Text(Rect r, string s, float size, Color c, TextAnchor anchor = TextAnchor.MiddleLeft,
              bool bold = false, bool wrap = false)
    {
        labelStyle.fontSize = Mathf.RoundToInt(size);
        labelStyle.alignment = anchor;
        labelStyle.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        labelStyle.wordWrap = wrap;
        GUI.color = Color.white;

        float o = Mathf.Max(1f, size * 0.06f);
        labelStyle.normal.textColor = new Color(0f, 0f, 0f, 0.65f * c.a * uiAlpha);
        GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), s, labelStyle);

        labelStyle.normal.textColor = new Color(c.r, c.g, c.b, c.a * uiAlpha);
        GUI.Label(r, s, labelStyle);
    }

    Vector2 Measure(string s, float size, bool bold)
    {
        labelStyle.fontSize = Mathf.RoundToInt(size);
        labelStyle.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        labelStyle.wordWrap = false;
        return labelStyle.CalcSize(new GUIContent(s));
    }
}