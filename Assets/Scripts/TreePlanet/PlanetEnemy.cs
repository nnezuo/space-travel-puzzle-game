using System.Collections.Generic;
using UnityEngine;

public class PlanetEnemy : MonoBehaviour
{
    public int health = 3;

    int maxHealth;

    // Every living enemy, and how many existed / were defeated (the objective list reads these)
    public static readonly List<PlanetEnemy> All = new List<PlanetEnemy>();
    public static int Total { get; private set; }
    public static int Defeated { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { All.Clear(); Total = 0; Defeated = 0; }

    void Awake()
    {
        maxHealth = health;
        Total++;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health > 0)
        {
            GameHUD.Say("Enemy hit! " + health + " / " + maxHealth + " left", GameHUD.Kind.Combat);
            return;
        }

        Defeated++;
        int left = Total - Defeated;
        GameHUD.Say(left > 0 ? "Enemy defeated! " + left + " left" : "All enemies defeated!", GameHUD.Kind.Success);
        Destroy(gameObject);
    }
}