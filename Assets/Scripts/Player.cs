using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public int health = 100;
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.5f;
    public LayerMask groundLayer;
    public Image healthImage; // Reference to the UI Image for health display

    private Rigidbody2D rb;
    private bool isGrounded;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private float moveInput;

    void Update()
    {
        moveInput = Input.GetAxis("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        SetAnimation(moveInput);

        healthImage.fillAmount = (float)health / 100f; // Update health UI
    }

    private void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void SetAnimation(float moveInput)
    {
        // Flip sprite based on direction, regardless of grounded state
        if (moveInput > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (moveInput < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        string targetState;

        if (isGrounded)
        {
            targetState = moveInput == 0 ? "Hero_Idle" : "Hero_Run";
        }
        else
        {
            targetState = rb.linearVelocity.y > 0 ? "Hero_Jump" : "Hero_Fall";
        }

        // Only Play if we're not already in that state, avoids restarting every frame
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(targetState))
        {
            animator.Play(targetState);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Damage"))
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        StartCoroutine(DamageFlash());

        if (health <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlash()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    private void Die()
    {
        // Handle player death (e.g., disable controls, play death animation)
        animator.SetTrigger("Death");
        UnityEngine.SceneManagement.SceneManager.LoadScene(0); // Reload the scene or handle game over
        // Optionally, disable player controls here
    }
}