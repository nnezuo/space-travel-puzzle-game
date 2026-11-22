
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public float attackRange = 5f;
    public int damage = 1;
    public KeyCode attackKey = KeyCode.F;

    private void Update()
    {
        if (Input.GetKeyDown(attackKey))
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        PlanetEnemy target = FindNearestEnemyWithinRange();

        if (target != null)
        {
            target.TakeDamage(damage);
            GameHUD.Say("You hit the enemy!", GameHUD.Kind.Combat);
        }
        else
        {
            GameHUD.Say("No enemy close enough to attack", GameHUD.Kind.Info);
        }
    }

    private PlanetEnemy FindNearestEnemyWithinRange()
    {
        PlanetEnemy[] enemies = FindObjectsByType<PlanetEnemy>(FindObjectsSortMode.None);
        PlanetEnemy nearest = null;
        float nearestDistance = attackRange;

        foreach (PlanetEnemy enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }
}
