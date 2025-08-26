using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class GravityController : MonoBehaviour
{
    public TextMeshProUGUI gravityText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static bool isGravityInverted = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isGravityInverted = !isGravityInverted;
            Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();

            gravityText.SetText(!isGravityInverted ? "Normal" : "Inverse");

            float gravityValue = isGravityInverted ? -1f : 1f;
            int playerRotationZ = isGravityInverted ? 180 : 0;
            int enemyRotationZ = isGravityInverted ? 180 : 0;

            playerRb.gravityScale = gravityValue;

            playerRb.velocity = Vector3.zero;
            other.transform.rotation = Quaternion.Euler(0, 0, playerRotationZ);

            foreach (GameObject obj in GameObject.FindObjectsOfType<GameObject>())
            {
                if (obj.layer == LayerMask.NameToLayer("Enemy"))
                {
                    Rigidbody2D enemyRb = obj.GetComponent<Rigidbody2D>();
                    if (enemyRb != null)
                    {
                        enemyRb.gravityScale = gravityValue;

                        if (isGravityInverted)
                        {
                            obj.transform.rotation = Quaternion.Euler(0, 180, enemyRotationZ);
                        }
                        else
                        {
                            obj.transform.rotation = Quaternion.Euler(0, 0, enemyRotationZ);
                        }


                        WalkingEnemy moves = enemyRb.GetComponent<WalkingEnemy>();
                        if (moves != null)
                        {
                            moves.walkDirection = isGravityInverted
                                ? (moves.walkDirection == WalkingEnemy.WalkDirections.Right ? WalkingEnemy.WalkDirections.Left : WalkingEnemy.WalkDirections.Right)
                                : (moves.walkDirection == WalkingEnemy.WalkDirections.Left ? WalkingEnemy.WalkDirections.Right : WalkingEnemy.WalkDirections.Left);
                        }

                    }
                }
            }
        }
    }


}
