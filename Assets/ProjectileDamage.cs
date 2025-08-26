using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class handles behavior for shoot damage
/// </summary>
public class ProjectileDamage : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The health component associated with this head")]
    public Health associatedHealth;
    [Tooltip("The amount of damage to deal when jumped on")]
    public int damage = 1;

    private void Start()
    {
        associatedHealth = this.GetComponentInParent<Health>();
    }

    /// <summary>
    /// Description:
    /// Standard Unity function that is called when a collider enters a trigger
    /// Input:
    /// Collision2D collision
    /// Return:
    /// void (no return)
    /// </summary>
    /// <param name="collision">The collision information of what has collided with the attached collider</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Projectile")
        {
            associatedHealth.TakeDamage(damage);
        }
        Destroy(gameObject);
    }

}
