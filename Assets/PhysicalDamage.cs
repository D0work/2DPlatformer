using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalDamage : MonoBehaviour
{
    [Header("Player Settings")]
    [Tooltip("Player")]
    private Health player = null;
    public BoxCollider2D hitboxCollider = null;

    [Header("Damage Settings")]
    [Tooltip("How much damage to deal")]
    public int damageAmount = 1;
    [Tooltip("Whether or not to destroy the attached game object after dealing damage")]
    public bool destroyAfterDamage = true;

    private GameObject currentTarget = null;
    private bool isLeft = false;

    void Start()
    {
        player = GetComponentInParent<Health>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other && other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            currentTarget = other.gameObject;
            DealDamage(currentTarget);
        }
    }

    public void setLeft(bool l)
    {
        this.isLeft = l;
    }

    public bool getLeft()
    {
        return this.isLeft;
    }

    private void DealDamage(GameObject collisionGameObject)
    {
        Health collidedHealth = collisionGameObject.GetComponent<Health>();
        if (collidedHealth != null)
        {
            if (collidedHealth.teamId != player.teamId)
            {
                collidedHealth.TakeDamage(damageAmount);
                if (destroyAfterDamage)
                {
                    Destroy(this.gameObject);
                }
            }
        }
    }

    public void ActivateHitbox()
    {
        hitboxCollider.enabled = true;
        Collider2D[] enemiesInHitbox = Physics2D.OverlapBoxAll(hitboxCollider.bounds.center, hitboxCollider.bounds.size, 0f);
        if (enemiesInHitbox != null && enemiesInHitbox.Length > 0)
        {
            foreach (Collider2D col in enemiesInHitbox)
            {
                if (col.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                {
                    DealDamage(col.gameObject);
                }
            }
        }
    }

    public void DeactivateHitbox()
    {
        hitboxCollider.enabled = false;
    }

}
