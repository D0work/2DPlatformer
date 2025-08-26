using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A class which controlls player aiming and shooting
/// </summary>
public class ShootingController : MonoBehaviour
{
    [Header("GameObject/Component References")]
    [Tooltip("The projectile to be fired.")]
    public GameObject projectilePrefab = null;
    [Tooltip("The transform in the heirarchy which holds projectiles if any")]
    public Transform projectileHolder = null;

    [Header("Input Settings, Actions, & Controls")]
    [Tooltip("Whether this shooting controller is controled by the player")]
    public bool isPlayerControlled = false;
    public InputAction fireAction;

    [Header("Firing Settings")]
    [Tooltip("The minimum time between projectiles being fired.")]
    public float fireRate = 0.05f;

    [Tooltip("The maximum diference between the direction the" +
        " shooting controller is facing and the direction projectiles are launched.")]
    public float projectileSpread = 1.0f;

    // The last time this component was fired
    private float lastFired = Mathf.NegativeInfinity;

    [Header("Effects")]
    [Tooltip("The effect to create when this fires")]
    public GameObject fireEffect;

    [Tooltip("Target")]
    public Transform targetTransform;
    [Tooltip("Map Boundary")]
    public BoxCollider2D boundaryBox;

    [Tooltip("Weapon")]
    public GameObject weapon = null;

    private bool isLeft = false;

    private Animator animator = null;
    private float animatorSpeed = 1.0f;

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is enabled
    /// </summary>
    void OnEnable()
    {
        fireAction.Enable();
    }

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is disabled
    /// </summary>
    void OnDisable()
    {
        fireAction.Disable();
    }

    /// <summary>
    /// Description:
    /// Standard unity function that runs every frame
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void Update()
    {
        ProcessInput();
    }

    /// <summary>
    /// Description:
    /// Standard unity function that runs when the script starts
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void Start()
    {
        if (fireAction.bindings.Count == 0 && isPlayerControlled)
        {
            Debug.LogWarning("The Fire Input Action does not have a binding set but is set to be player controlled! Make sure that it has a binding or the shooting controller will not shoot!");
        }
        this.animator = GetComponent<Animator>();
        this.animatorSpeed = animator.speed;
    }

    public void setLeft(bool l)
    {
        this.isLeft = l;
    }

    public bool getLeft()
    {
        return this.isLeft;
    }

    public void SlowDownAnimator()
    {
        this.animator.speed /= 4f; 
    }

    public void ResetAnimator()
    {
        animator.speed = animatorSpeed;
    }


    public void SpeedUpAnimator()
    {
        this.animator.speed *= 2f;
    }

    /// <summary>
    /// Description:
    /// Reads input from the input manager
    /// Inputs:
    /// None
    /// Returns:
    /// void (no return)
    /// </summary>
    public void ProcessInput()
    {
        if (isPlayerControlled)
        {
            if (fireAction.bindings.Count == 0)
            {
                Debug.LogError("The Fire Input Action does not have a binding set! It must have a binding set in order to fire!");
            }
            if (fireAction.ReadValue<float>() >= 1)
            {
                Fire();
            }
        }   
    }

    /// <summary>
    /// Description:
    /// Fires a projectile if possible
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    public void Fire()
    {
        // If the cooldown is over fire a projectile
        if ((Time.timeSinceLevelLoad - lastFired) > fireRate)
        {
            // Launches a projectile
            SpawnProjectile();

            if (fireEffect != null)
            {
                Instantiate(fireEffect, transform.position, transform.rotation, null);
            }

            // Restart the cooldown
            lastFired = Time.timeSinceLevelLoad;
        }
    }

    /// <summary>
    /// Description:
    /// Spawns a projectile and sets it up
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    public void SpawnProjectile()
    {
        // Check that the prefab is valid
        if (projectilePrefab != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, transform.position, transform.rotation, null);
            Vector3 fireDirection = new Vector3();
            if (!weapon)
            {
                if (targetTransform != null && Random.value > 0.5f)
                {
                    Vector3 targetPosition = GetTargetPosition();
                    fireDirection = (targetPosition - transform.position).normalized;

                    float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg - 90f;
                    angle += Random.Range(-projectileSpread, projectileSpread);
                    projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);

                    Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.velocity = fireDirection * fireRate;
                    }
                }
                else
                {
                    Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    mouseWorldPosition.z = 0f;
                    fireDirection = (mouseWorldPosition - transform.position).normalized;

                    // Account for spread
                    float baseAngle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg - 90f;
                    Vector3 rotationEulerAngles = projectile.transform.rotation.eulerAngles;
                    rotationEulerAngles.z += baseAngle + Random.Range(-projectileSpread, projectileSpread);
                    projectile.transform.rotation = Quaternion.Euler(rotationEulerAngles);


                    // speed
                    Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.velocity = projectile.transform.right * fireRate;
                    }
                }


                // Keep the heirarchy organized
                if (projectileHolder == null && GameObject.Find("ProjectileHolder") != null)
                {
                    projectileHolder = GameObject.Find("ProjectileHolder").transform;
                }
                if (projectileHolder != null)
                {
                    projectile.transform.SetParent(projectileHolder);
                }
            }
            else
            {
                if (isLeft)
                {
                    fireDirection = -transform.right;
                }
                else
                {
                    fireDirection = transform.right;
                }

                float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg - 90f;
                angle += Random.Range(-projectileSpread, projectileSpread);
                projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);

                Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = fireDirection * fireRate;
                }
            }
        }
    }

    private Vector3 GetTargetPosition()
    {
        if (targetTransform != null /*&& Random.value > 0.5f*/) 
        {
            return targetTransform.position;
        }
        else
        {
            Bounds bounds = boundaryBox.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomY = Random.Range(bounds.min.y, bounds.max.y);
            return new Vector3(randomX, randomY, transform.position.z);
        }
    }
}
