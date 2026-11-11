
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public float attackRange = 5f;
    public int damage = 1;
    public KeyCode attackKey = KeyCode.F;

    void Update()
    {
        if (Input.GetKeyDown(attackKey))
        {
            Attack();
        }
    }

    void Attack()
    {
        PlanetEnemy[] enemies =
            FindObjectsByType<PlanetEnemy>(FindObjectsSortMode.None);

        PlanetEnemy closestEnemy = null;
        float closestDistance = attackRange;

        foreach (PlanetEnemy enemy in enemies)
        {
            float distance = Vector3.Distance(
                transform.position,
                enemy.transform.position
            );

            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy != null)
        {
            closestEnemy.TakeDamage(damage);
            Debug.Log("You attacked the enemy!");
        }
        else
        {
            Debug.Log("No enemy close enough to attack.");
        }
    }
}
