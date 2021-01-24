// Pol Lozano Llorens
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Player : MonoBehaviour
{
    [Header("Player stats")]
    [SerializeField] private float speed = 5;
    [SerializeField] private float jumpForce = 2;
    [SerializeField] private Vector2 moveDir;

    [Header("Collision Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool isGrounded = false;
    [SerializeField] private bool onLadder = false;
    [SerializeField] private Vector2 bottomOffset;
    [SerializeField] private float collisionRadius = 0.2f;

    bool enteringDoor = false;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    private Tilemap tilemap;

    private enum State
    {
        grounded,
        jumping,
        falling,
        climbing,
        wallGrab
    }

    [SerializeField] private State playerState = State.grounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        tilemap = FindObjectOfType<LevelGenerator>().Tilemap;
    }

    void Update()
    {
        moveDir = Vector2.zero;
        moveDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        Animate();
        SetRigidbodyConstraints();

        switch (playerState)
        {
            case State.grounded:
                if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Z))
                {
                    Jump();
                    playerState = State.jumping;
                }
                if (!isGrounded)
                    playerState = State.falling;
                break;
            case State.jumping:
                if (rb.velocity.y < 0)
                    playerState = State.falling;
                break;
            case State.falling:
                if (isGrounded)
                    playerState = State.grounded;
                break;
            case State.wallGrab:
                if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Z))
                {
                    Jump();
                    playerState = State.jumping;
                }
                //Drop down off ledge
                if (Input.GetAxisRaw("Vertical") == -1)
                {   
                    playerState = State.falling;
                }
                break;
            case State.climbing:
                rb.velocity = new Vector2(0, Input.GetAxisRaw("Vertical") * speed / 2);
                if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Z))
                {
                    if (moveDir.x != 0)
                    {
                        Jump();
                        playerState = State.jumping;
                    }
                }
                if (!onLadder || isGrounded)
                {
                    playerState = isGrounded ? State.grounded : State.falling;
                }
                break;
        }

    }

    private void SetRigidbodyConstraints()
    {
        switch (playerState)
        {
            case State.climbing:
                rb.gravityScale = 0;
                rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
                break;
            case State.wallGrab:
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
                break;
            default:
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.gravityScale = 4;
                break;
        }
    }

    private void FixedUpdate()
    {
        GroundCheck();
        WallCheck();
        Move(moveDir);        
    }

    private void Move(Vector2 dir)
    {
        rb.velocity = new Vector2(dir.x * speed, rb.velocity.y);
    }

    private void Animate()
    {
        anim.SetFloat("xDir", Mathf.Abs(moveDir.x));
        anim.SetFloat("yDir", moveDir.y);
        anim.SetBool("isGrounded", isGrounded);

        anim.SetBool("wallGrab", playerState == State.wallGrab);
        anim.SetBool("climbing", playerState == State.climbing);

        //Flip sprite based on direction
        if (moveDir.x != 0)
            sr.flipX = moveDir.x >= 0;
    }


    private void Jump()
    {
        //Reset rigidbody constraints
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 4;

        //Reset vertical velocity
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.velocity += Vector2.up * jumpForce;
    }

    private void GroundCheck()
    {
        //Checks for overlapping colliders under the players feet
        isGrounded = Physics2D.OverlapCircle((Vector2)transform.position + bottomOffset, collisionRadius, groundLayer);
    }

    private void WallCheck()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right, .5f * moveDir.x, groundLayer);
        if (hit.collider != null)
        {
            Vector3 hitPosition = new Vector3(hit.point.x - .01f * hit.normal.x, hit.point.y - .01f * hit.normal.y);

            Vector3Int hitPos = tilemap.WorldToCell(hitPosition);
            Vector3Int playerPos = tilemap.WorldToCell(transform.position);

            if (tilemap.GetTile(hitPos + Vector3Int.up) == null
                && tilemap.GetTile(playerPos + Vector3Int.up) == null
                && tilemap.GetTile(playerPos + Vector3Int.down) == null)
            {
                if (playerState != State.jumping && playerState != State.climbing)
                {
                    rb.MovePosition(new Vector2(hitPos.x, hitPos.y + 0.5f));
                    playerState = State.wallGrab;
                }
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        //Handle grabbing onto ladders
        if (other.CompareTag("Ladder"))
        {
            onLadder = true;

            Vector3Int hitPos = tilemap.WorldToCell(transform.position);
            var test = tilemap.GetCellCenterWorld(hitPos);

            if (Input.GetAxisRaw("Vertical") != 0 && playerState != State.climbing && onLadder)
            {
                //Attach to ladder
                rb.velocity = Vector2.zero;
                transform.position = new Vector2(test.x, rb.position.y);
                playerState = State.climbing;
            }
        }

        //Handle entering doors
        if (other.CompareTag("Door"))
        {
            if (Input.GetAxisRaw("Vertical") == 1f && !enteringDoor)
            {
                enteringDoor = true;
                anim.SetTrigger("enterDoor");
                Invoke(nameof(EnterDoor), .5f);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))       
            onLadder = false;
    }

    //For now just generate a new level 
    private void EnterDoor()
    {
        GameManager.instance.LoadLevel();
        enteringDoor = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, Vector2.right * .5f * moveDir);
        Gizmos.DrawWireSphere((Vector2)transform.position + bottomOffset, collisionRadius);
    }

}
