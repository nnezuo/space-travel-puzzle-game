using UnityEngine;

public class Crystal : MonoBehaviour
{
    public float spinSpeed = 60f;
    public float bobHeight = 0.3f;
    public float bobSpeed = 2f;

    Planet1Puzzle puzzle;
    public PlanetEnemy guardingEnemy;
    Vector3 startLocalPos;
    bool collected;

    void Start()
    {
        startLocalPos = transform.localPosition;
        puzzle = FindAnyObjectByType<Planet1Puzzle>();
        if (puzzle != null) puzzle.RegisterCrystal();
    }

    void Update()
    {
        // Spin around the crystal's own up axis and float up and down
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
        transform.localPosition = startLocalPos +
            Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    void OnTriggerEnter(Collider other) => TryCollect(other, true);
    void OnTriggerStay(Collider other) => TryCollect(other, false);

    void TryCollect(Collider other, bool showMessage)
    {
        if (collected) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;

        if (guardingEnemy != null)
        {
            if (showMessage)
                GameHUD.Say("A guardian protects this crystal. Defeat it first!", GameHUD.Kind.Warning);
            return;
        }

        collected = true;
        if (puzzle != null) puzzle.CrystalCollected();
        Destroy(gameObject);
    }
}