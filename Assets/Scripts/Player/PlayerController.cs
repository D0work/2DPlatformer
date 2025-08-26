using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Class which handles player movement
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Game Object and Component References")]
    [Tooltip("The Ground Check component used to check whether this player is grounded currently.")]
    public GroundCheck groundCheck = null;
    [Tooltip("The sprite renderer that represents the player.")]
    public SpriteRenderer spriteRenderer = null;
    [Tooltip("The health component attached to the player.")]
    public Health playerHealth;

    // The rigidbody used to move the player (necessary for this component, so not made public)
    private Rigidbody2D playerRigidbody = null;

    #region Getters (primarily from other components)
    #region Directional facing
    /// <summary>
    /// Enum to help determine which direction the player is facing.
    /// </summary>
    public enum PlayerDirection
    {
        Right,
        Left
    }

    // Which way the player is facing right now
    public PlayerDirection facing
    {
        get
        {
            if (moveAction.ReadValue<Vector2>().x > 0)
            {
                return (playerRigidbody.gravityScale > 0 ? PlayerDirection.Right : PlayerDirection.Left);
            }
            else if (moveAction.ReadValue<Vector2>().x < 0)
            {
                return (playerRigidbody.gravityScale > 0 ? PlayerDirection.Left : PlayerDirection.Right);
            }
            else
            {
                if (spriteRenderer != null && spriteRenderer.flipX == true) {
                    return PlayerDirection.Left;
                }
                return PlayerDirection.Right;
            }
        }
    }
    #endregion

    // Whether this player is grounded false if no ground check component assigned
    public bool grounded
    {
        get
        {
            if (groundCheck != null)
            {
                return groundCheck.CheckGrounded();
            }
            else
            {
                return false;
            }
        }
    }

    // Whether this player is dashing
    private bool dashing = false;
    // Whether this player is atacking
    private bool atacking = false;
    // Whether this player is shoting 
    private bool shoting = false;

    #endregion

    [Header("Movement Settings")]
    [Tooltip("The speed at which to move the player horizontally")]
    public float movementSpeed = 4.0f;

    [Header("Jump Settings")]
    [Tooltip("The force with which the player jumps.")]
    public float jumpPower = 10.0f;
    [Tooltip("The number of jumps that the player is alowed to make.")]
    public int allowedJumps = 1;
    [Tooltip("The duration that the player spends in the \"jump\" state")]
    public float jumpDuration = 0.1f;
    [Tooltip("The effect to spawn when the player jumps")]
    public GameObject jumpEffect = null;
    [Tooltip("Layers to pass through when moving upwards")]
    public List<string> passThroughLayers = new List<string>();

    [Header("Input Actions & Controls")]
    [Tooltip("The input action(s) that map to player movement")]
    public InputAction moveAction;
    [Tooltip("The input action(s) that map to jumping")]
    public InputAction jumpAction;

    [Tooltip("The gameobjet for atack type actions")]
    public GameObject weapon = null;

    // The number of times this player has jumped since being grounded
    private int timesJumped = 0;
    // Whether the player is in the middle of a jump right now
    private bool jumping = false;

    [Header("Extra Moves")]
    [Tooltip("Additionnal inputs different from basics inputs")]
    public bool extraMoves = false;
    public InputAction dashAction;
    [Tooltip("The speed in the \"dash\" state")]
    public float dashBoost = 1f;
    [Tooltip("The duration that the player spends in the \"dash\" state")]
    public float dashDuration = 0.25f;
    [Tooltip("The effect to spawn when the player dash")]
    public GameObject dashEffect = null;

    [Tooltip("The input action(s) that map to player shot")]
    public InputAction shotAction;
    [Tooltip("The duration that the player spends in the \"shot\" state")]
    public float shotDuration = 0.25f;

    [Tooltip("The input action(s) that map to player atack")]
    public InputAction atackAction;
    [Tooltip("The duration that the player spends in the \"atack\" state")]
    public float atackDuration = 0.25f;

    [Tooltip("Script to Handling shooting")]
    [SerializeField] private ShootingController playerShootScript = null;
    [Tooltip("Script to Handling physical atacks")]
    [SerializeField] private PhysicalDamage playerAtackScript = null;

    #region Player State Variables
    /// <summary>
    /// Enum used for categorizing the player's state
    /// </summary>
    public enum PlayerState
    {
        Idle,
        Walk,
        Jump,
        Fall,
        Dead,
        Dash,
        Shot,
        Atack
    }

    // The player's current state (walking, idle, jumping, or falling)
    public PlayerState state = PlayerState.Idle;
    #endregion

    #region Functions
    #region GameObject Functions

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is enabled
    /// </summary>
    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        if (extraMoves)
        {
            dashAction.Enable();
            shotAction.Enable();
            atackAction.Enable();
        }
    }

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is disabled
    /// </summary>
    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        if (extraMoves)
        {
            dashAction.Disable();
            shotAction.Disable();
            atackAction.Disable();
        }
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called once before the first update
    /// Input: 
    /// none
    /// Return: 
    /// void (no return)
    /// </summary>
    private void Start()
    {
        playerShootScript = this.GetComponentInChildren<ShootingController>();
        playerAtackScript = this.GetComponentInChildren<PhysicalDamage>();
        Debug.Log("playerShotScript is " + (playerShootScript ? "set" : "no set") + " from " + playerShootScript.gameObject.name);
        Debug.Log("playerAtackScript is " + (playerAtackScript ? "set" : "no set") + " from " + playerAtackScript.gameObject.name);
        SetupRigidbody();
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called once every frame after update
    /// Every frame, process input, move the player, determine which way they should face, and choose which state they are in
    /// Input: 
    /// none
    /// Return: 
    /// void (no return)
    /// </summary>
    private void LateUpdate()
    {
        ProcessInput();
        HandleSpriteDirection();
        DetermineState();
    }
    #endregion

    #region Input Handling and Movement Functions
    /// <summary>
    /// Description:
    /// Processes input
    /// Input: none
    /// Return: void (no return)
    /// </summary>
    private void ProcessInput()
    {
        HandleMovementInput();
        HandleJumpInput();
        HandleDashInput();
        HandleActackInput();
        HandleShotInput();
    }

    /// <summary>
    /// Description:
    /// Handles movement input
    /// Input: none
    /// Return: void (no return)
    /// </summary>
    private void HandleMovementInput()
    {
        Vector2 movementForce = Vector2.zero;
        if (Mathf.Abs(moveAction.ReadValue<Vector2>().x) > 0 && state != PlayerState.Dead)
        {
            movementForce = transform.right * movementSpeed * moveAction.ReadValue<Vector2>().x;
        }
        if (playerRigidbody.gravityScale < 0)
        {
            movementForce *= -1; 
        }
        MovePlayer(movementForce);
    }

    /// <summary>
    /// Description:
    /// Moves the player with a specified force
    /// Input: 
    /// Vector2 movementForce
    /// Return: 
    /// void (no return)
    /// </summary>
    /// <param name="movementForce">The force with which to move the player</param>
    private void MovePlayer(Vector2 movementForce)
    {
        float dashMultiplier = dashing ? dashBoost : 1f;

        if (grounded && !jumping)
        {
            float horizontalVelocity = movementForce.x * dashMultiplier;
            float verticalVelocity = 0;
            playerRigidbody.velocity = new Vector2(horizontalVelocity, verticalVelocity);
        }
        else
        {
            float horizontalVelocity = movementForce.x * dashMultiplier;
            float verticalVelocity = playerRigidbody.velocity.y;
            playerRigidbody.velocity = new Vector2(horizontalVelocity, verticalVelocity);
        }
        if (playerRigidbody.velocity.y > 0)
        {
            foreach (string layerName in passThroughLayers)
            {
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer(layerName), true);
            } 
        }
        else
        {
            foreach (string layerName in passThroughLayers)
            {
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer(layerName), false);
            }
        }
    }

    /// <summary>
    /// Description:
    /// Handles jump input
    /// Input:
    /// none
    /// Return:
    /// void (no return)
    /// </summary>
    private void HandleJumpInput()
    {
        if (jumpAction.triggered)
        {
            StartCoroutine("Jump", 1.0f);
        }
    }

    /// <summary>
    /// Description:
    /// Coroutine which causes the player to jump.
    /// Input: 
    /// none
    /// Return: 
    /// void (no return)
    /// </summary>
    /// <returns>IEnumerator: makes coroutine possible</returns>
    private IEnumerator Jump(float powerMultiplier = 1.0f)
    {
        if (timesJumped < allowedJumps && state != PlayerState.Dead)
        {
            jumping = true;
            float time = 0;
            SpawnJumpEffect();
            playerRigidbody.velocity = new Vector2(playerRigidbody.velocity.x, 0);
            playerRigidbody.AddForce(transform.up * jumpPower * powerMultiplier, ForceMode2D.Impulse);
            timesJumped++;
            while (time < jumpDuration)
            {
                yield return null;
                time += Time.deltaTime;
            }
            jumping = false;
        }
    }

    /// <summary>
    /// Description:
    /// Spawns the effect that occurs when the player jumps
    /// Input: 
    /// none
    /// Return: 
    /// void (no return)
    /// </summary>
    private void SpawnJumpEffect()
    {
        if (jumpEffect != null)
        {
            Instantiate(jumpEffect, transform.position, transform.rotation, null);
        }
    }

    /// <summary>
    /// Description:
    /// Bounces the player upwards, refunding jumps.
    /// Input: 
    /// none
    /// Return: 
    /// void (no return)
    /// </summary>
    public void Bounce()
    {
        timesJumped = 0;
        if (jumpAction.ReadValue<float>() >= 1)
        {
            StartCoroutine("Jump", 1.5f);
        }
        else
        {
            StartCoroutine("Jump", 1.0f);
        }
    }

    private void HandleDashInput()
    {
        if (dashAction.triggered)
        {
            if (!grounded)
            {
                StartCoroutine("Dash", dashBoost);
            }
        }
    }

    private IEnumerator Dash(float powerMultiplier = 1.0f)
    {
        dashing = true;
        float time = 0f;

        SpawnDashEffect(); 
        Vector2 dashDirection = transform.right.normalized; 
        playerRigidbody.velocity = Vector2.zero;
        Vector2 Boost = dashDirection * dashBoost * powerMultiplier;
        playerRigidbody.AddForce(Boost, ForceMode2D.Impulse);
        Debug.Log("dashDirection: " + dashDirection.ToString() + " |Boost = " + Boost.ToString());
        while (time < dashDuration)
        {
            time += Time.deltaTime;
            yield return null;
        }

        dashing = false;
    }

    private void SpawnDashEffect()
    {
        if (dashEffect != null)
        {
            Instantiate(dashEffect, transform.position, transform.rotation, null);
        }
    }

    private void HandleActackInput()
    {
        if (atackAction.triggered)
        {
            StartCoroutine("Atack");
        }
    }

    private IEnumerator Atack()
    {
        atacking = true;
        float time = 0;
        while (time < atackDuration)
        {
            yield return null;
            time += Time.deltaTime;
        }
        atacking = false;
    }

    private void HandleShotInput()
    {
        if (shotAction.triggered)
        {
            StartCoroutine("Shot");
        }
    }

    private IEnumerator Shot()
    {
        shoting = true;
        float time = 0;
        while (time < shotDuration)
        {
            yield return null;
            time += Time.deltaTime;
        }
        shoting = false;
    }

    public void inverseFlipX()
    {
        spriteRenderer.flipX = !spriteRenderer.flipX;
    }


    /// <summary>
    /// Description:
    /// Determines which way the player should be facing, then makes them face in that direction
    /// Input: 
    /// none
    /// Return: 
    /// void (no return)
    /// </summary>
    private void HandleSpriteDirection()
    {
        if (spriteRenderer != null)
        {
            Debug.Log("Facing : " + facing.ToString());
            if (facing == PlayerDirection.Left)
            {
                spriteRenderer.flipX = true;
                if (playerShootScript) playerShootScript.setLeft(true);
                if (playerAtackScript) playerAtackScript.setLeft(true);
            }
            else
            {
                spriteRenderer.flipX = false;
                if (playerShootScript) playerShootScript.setLeft(false);
                if (playerAtackScript) playerAtackScript.setLeft(false);
            }
        }
    }
    #endregion

    #region State Functions
    /// <summary>
    /// Description:
    /// Gets and returns the player's current state
    /// Input: 
    /// none
    /// Return: 
    /// PlayerState
    /// </summary>
    /// <returns>PlayerState: The player's current state (idle, walking, jumping, falling</returns>
    private PlayerState GetState()
    {
        return state;
    }

    /// <summary>
    /// Description:
    /// Sets the player's current state
    /// Input: 
    /// none
    /// Return: 
    /// void (no return)
    /// </summary>
    /// <param name="newState">The PlayerState to set the current state to</param>
    private void SetState(PlayerState newState)
    {
        state = newState;
    }

    /// <summary>
    /// Description:
    /// Determines which state is appropriate for the player currently
    /// Input: 
    /// none
    /// Return: 
    /// void (no return)
    /// </summary>
    private void DetermineState()
    {
        if (playerHealth.currentHealth <= 0)
        {
            SetState(PlayerState.Dead);
        }
        else if (extraMoves && dashing)
        {
            SetState(PlayerState.Dash);
            Debug.Log("Player dashAction");
        }
        else if (extraMoves && shoting)
        {
            SetState(PlayerState.Shot);
            Debug.Log("Player shotAction");
        }
        else if (extraMoves && atacking)
        {
            SetState(PlayerState.Atack);
            Debug.Log("Player atackAction");
        }
        else if (grounded)
        {
            if (Mathf.Abs(playerRigidbody.velocity.x) > 0.1f)
            {
                SetState(PlayerState.Walk);

            }
            else
            {
                SetState(PlayerState.Idle);
            }

            if (!jumping)
            {
                timesJumped = 0;
            }
        }
        else
        {
            if (jumping)
            {
                SetState(PlayerState.Jump);
            }
            else
            {
                SetState(PlayerState.Fall);
            }
        }
        Debug.Log("Player State : " + this.gameObject.name + " :"+ GetState().ToString() + " - jumpTriggered:" + jumping.ToString() + " - dashTriggered:" + dashing.ToString() + " - atackTriggered:" + atacking.ToString() + " |  shotTriggered:" + shoting.ToString());
    }



    #endregion

    /// <summary>
    /// Description:
    /// Sets up the player's rigidbody
    /// Input: 
    /// none
    /// Return: 
    /// void (no return)
    /// </summary>
    private void SetupRigidbody()
    {
        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody2D>();
        }
    }
    #endregion
}
