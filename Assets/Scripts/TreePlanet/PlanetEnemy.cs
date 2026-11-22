
using System.Collections.Generic;
using UnityEngine;

public class PlanetEnemy : MonoBehaviour
{
    public int health = 3;

    private int maxHealth;

    public static readonly List<PlanetEnemy> All = new List<PlanetEnemy>();
    public static int Total { get; private set; }
    public static int Defeated { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        All.Clear();
        Total = 0;
        Defeated = 0;
    }

    private void Awake()
    {
        maxHealth = health;
        Total++;
    }

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }
    
    //This is where I used AI
    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            Defeated++;

            int remainingEnemies = Total - Defeated;

            string message = remainingEnemies > 0
                ? $"Enemy defeated! {remainingEnemies} left"
                : "All enemies defeated!";

            GameHUD.Say(message, GameHUD.Kind.Success);
            Destroy(gameObject);
            return;
        }

        GameHUD.Say($"Enemy hit! {health} / {maxHealth} left", GameHUD.Kind.Combat);
    }
}
