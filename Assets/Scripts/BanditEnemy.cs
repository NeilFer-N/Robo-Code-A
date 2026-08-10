using UnityEngine;

public class BanditEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public Transform pointA;
    public Transform pointB;

    [Header("Detection")]
    public float chaseRange = 5f;   // how close player must be to start chasing
    public float attackRange = 0.5f;  // how close player must be to attack
    public float maxAttackHeightDiff = 1f; // how much vertical difference is still "reachable" to attack
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;

    [Header("Combat")]
    public int health = 30;
    public float attackCooldown = 1.5f;

    [Header("Patrol")]
    public float waitTimeAtPoint = 1.5f;

    private Transform player;
    private Player playerComponent;
    private Transform currentTarget;
    private Rigidbody2D rb;
    private Animator animator;
    private bool isGrounded;
    private bool isAttacking;
    private bool isDead;
    private bool isWaitingAtPoint;
    private float lastAttackTime;
    private float waitTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentTarget = pointB; // start heading toward point B

        if (pointA == null || pointB == null)
        {
            Debug.LogWarning($"{name}: Point A or Point B is not assigned in the Inspector — patrol will not work.");
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerComponent = playerObj.GetComponent<Player>();
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        isGrounded = groundCheck != null &&
            Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("Grounded", isGrounded);

        float distanceToPlayer = player != null ? Mathf.Abs(transform.position.x - player.position.x) : Mathf.Infinity;

        Debug.Log($"{name}: player={(player != null ? player.name : "NULL")}, distToPlayer={distanceToPlayer:F2}, isAttacking={isAttacking}, isWaiting={isWaitingAtPoint}, currentTarget={(currentTarget != null ? currentTarget.name : "NULL")}, velocity={rb.linearVelocity}");

        if (isAttacking)
        {
            // Stand still while mid-attack
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetInteger("AnimState", 1); // Combat Idle
            return;
        }

        if (distanceToPlayer <= attackRange && GetHeightDiffToPlayer() <= maxAttackHeightDiff)
        {
            // Close enough to attack
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            FaceTarget(player.position);
            animator.SetInteger("AnimState", 1); // Combat Idle

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }
        else if (distanceToPlayer <= chaseRange)
        {
            // Chase the player
            MoveToward(player.position);
        }
        else
        {
            // Patrol between point A and point B
            Patrol();
        }
    }

    private void Patrol()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning($"{name}: Point A or Point B is not assigned in the Inspector.");
            return;
        }

        if (isWaitingAtPoint)
        {
            // Stand still and idle while waiting
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetInteger("AnimState", 0); // Idle

            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0f)
            {
                isWaitingAtPoint = false;
                currentTarget = currentTarget == pointA ? pointB : pointA;
            }
            return;
        }

        MoveToward(currentTarget.position);

        float dist = Mathf.Abs(transform.position.x - currentTarget.position.x);

        if (dist < 0.3f)
        {
            isWaitingAtPoint = true;
            waitTimer = waitTimeAtPoint;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private float GetHeightDiffToPlayer()
    {
        // transform.position pivots differ between the enemy and player prefabs
        // (the enemy's origin is at its feet, the player's is higher up near its
        // center). Comparing raw transform.position.y biases the check so attacks
        // only land when the enemy happens to be above the player. Comparing
        // groundCheck (feet) positions instead makes the check symmetric.
        float enemyFootY = groundCheck != null ? groundCheck.position.y : transform.position.y;
        float playerFootY = (playerComponent != null && playerComponent.groundCheck != null)
            ? playerComponent.groundCheck.position.y
            : player.position.y;

        return Mathf.Abs(enemyFootY - playerFootY);
    }

    private void MoveToward(Vector2 targetPos)
    {
        Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);

        FaceTarget(targetPos);
        animator.SetInteger("AnimState", 2); // Run
    }

    private void FaceTarget(Vector2 targetPos)
    {
        if (targetPos.x > transform.position.x)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else if (targetPos.x < transform.position.x)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
    }

    private void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        animator.Play("Attack", 0, 0f); // force play from the start, bypassing any early auto-exit transition

        // Damage is now dealt via an Animation Event on LightBandit_Attack.anim
        // calling DealDamage() at the hit frame — no Invoke needed for that anymore.

        // How long the whole attack should lock movement for.
        // Adjust to match your Attack.anim length.
        Invoke(nameof(EndAttack), 0.6f);
    }

    public void DealDamage()
    {
        Debug.Log($"{name}: DealDamage() called. player={(player != null ? player.name : "NULL")}");

        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        Debug.Log($"{name}: distanceToPlayer={distanceToPlayer:F2}, attackRange+0.5={attackRange + 0.5f:F2}");

        if (distanceToPlayer <= attackRange + 0.5f)
        {
            Player playerScript = player.GetComponent<Player>();
            Debug.Log($"{name}: playerScript found={playerScript != null}");
            if (playerScript != null)
            {
                playerScript.TakeDamage(10);
                Debug.Log($"{name}: dealt damage via TakeDamage()");
            }
        }
        else
        {
            Debug.Log($"{name}: player out of range, no damage dealt");
        }
    }

    private void EndAttack()
    {
        isAttacking = false;
        animator.SetTrigger("Recover");
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        health -= amount;
        animator.SetTrigger("Hurt");

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger("Death");
        // Optionally disable collider/script after a delay, or destroy the object
        Destroy(gameObject, 2f);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position + Vector3.up * 0.75f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, chaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}