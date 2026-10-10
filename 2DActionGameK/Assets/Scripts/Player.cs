using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("移動スピード")]
    [SerializeField] float moveSpeed = 5f;
    [Header("ジャンプ -- 高さ")]
    [SerializeField] float jumpHeight = 3f;
    [Header("ジャンプ -- 上昇時間（頂点まで何秒か。小さいほど速く上がる）")]
    [SerializeField] float riseTime = 0.4f;
    [Header("ジャンプ -- 下降時間（頂点から同じ高さに戻るまで何秒か。小さいほど速く落ちる）")]
    [SerializeField] float fallTime = 0.3f;
    [Header("ジャンプ -- 落ちる速さの上限（高い所から落ちたとき用）")]
    [SerializeField] float maxFallSpeed = 20f;
    [Header("接地判定 -- 足元の位置")]
    [SerializeField] Transform groundCheck;
    [Header("接地判定 -- 足元の円の半径")]
    [SerializeField] float groundRadius = 0.12f;
    [Header("接地判定 -- 地面とみなすレイヤー")]
    [SerializeField] LayerMask groundLayers;
    [Header("HP -- 最大値")]
    [SerializeField] int maxHp = 3;
    [Header("HP -- ライフ表示")]
    [SerializeField] Life life;
    [Header("被弾 -- 飛ばされる速度（X+ = 向いている方向、X- = 真後ろ、Y+ = 上）")]
    [SerializeField] Vector2 knockbackVelocity = new Vector2(-4f, 6f);
    [Header("被弾 -- 操作できない時間（秒）")]
    [SerializeField] float hitStunTime = 0.4f;
    [Header("サウンド -- 移動開始 SE")]
    [SerializeField] AudioClip moveStartClip;
    [Header("サウンド -- ジャンプ SE")]
    [SerializeField] AudioClip jumpClip;
    [Header("サウンド -- 着地 SE")]
    [SerializeField] AudioClip landClip;
    [Header("サウンド -- ダメージ SE")]
    [SerializeField] AudioClip damageClip;
    [Header("サウンド -- 死亡 SE")]
    [SerializeField] AudioClip deathClip;

    float minInputToMove = 0.2f;
    Rigidbody2D rigidBody2D;
    SpriteRenderer spriteRenderer;
    InputAction moveAction;
    InputAction jumpAction;
    float moveInputX;
    int facing;
    bool isGrounded;
    int currentHp;
    float hitStunTimer;
    bool isDead;
    bool wasGrounded;
    AudioSource audioSource;
    bool wasMoving;
    Animator animator;
    string currentAnim = "Idle";

    void Awake()
    {
        rigidBody2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        moveAction = InputSystem.actions.FindAction("Player/Move");
        jumpAction = InputSystem.actions.FindAction("Player/Jump");
        currentHp = maxHp;
    }

    void Start()
    {
        life.Setup(maxHp);
        life.SetLife(currentHp);
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        if (hitStunTimer > 0f)
        {
            hitStunTimer -= Time.deltaTime;
        }

        if (isDead)
        {
            moveInputX = 0f;
            return;
        }

        Vector2 move = moveAction.ReadValue<Vector2>();
        moveInputX = move.x;

        if (Mathf.Abs(moveInputX) < minInputToMove || hitStunTimer > 0f)
        {
            moveInputX = 0f;
        }

        UpdateGrounded();

        HandleMoveStartSound();
        HandleLandSound();

        if (hitStunTimer > 0f)
        {
            return;
        }

        UpdateFacing();
        TryJump();
    }

    private void FixedUpdate()
    {
        if (hitStunTimer <= 0f && !isDead)
        {
            Vector2 velocity = rigidBody2D.linearVelocity;
            velocity.x = moveInputX * moveSpeed;
            rigidBody2D.linearVelocity = velocity;
        }

        ApplyJumpGravity();
    }

    void UpdateFacing()
    {
        if (moveInputX > 0f)
        {
            facing = 1;
        }
        else if (moveInputX < 0f)
        {
            facing = -1;
        }

        spriteRenderer.flipX = facing < 0;
    }

    void UpdateGrounded()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayers) != null;
    }

    void TryJump()
    {
        if (!jumpAction.WasPressedThisFrame())
        {
            return;
        }

        if (!isGrounded)
        {
            return;
        }

        Vector2 velocity = rigidBody2D.linearVelocity;
        velocity.y = 2f * jumpHeight / riseTime;
        rigidBody2D.linearVelocity = velocity;
        PlayOneShot(jumpClip);
    }

    void LateUpdate()
    {
        UpdateAnimation();
        HideAfterDeath();
    }


    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }

    void ApplyJumpGravity()
    {
        float riseGravity = 2f * jumpHeight / (riseTime * riseTime);
        float fallGravity = 2f * jumpHeight / (fallTime * fallTime);
        float gravity = rigidBody2D.linearVelocity.y > 0f ? riseGravity : fallGravity;

        rigidBody2D.gravityScale = gravity / Mathf.Abs(Physics2D.gravity.y);
        Vector2 velocity = rigidBody2D.linearVelocity;

        if (velocity.y < -maxFallSpeed)
        {
            velocity.y = -maxFallSpeed;
            rigidBody2D.linearVelocity = velocity;
        }
    }

    public void TakeDamage(int amount)
    {
        currentHp = Mathf.Max(currentHp - amount, 0);
        life.SetLife(currentHp);
        ApplyKnockback();

        if (currentHp <= 0)
        {
            Die();
        }
        else
        {
            PlayOneShot(damageClip);
        }
    }

    void ApplyKnockback()
    {
        rigidBody2D.linearVelocity = new Vector2(knockbackVelocity.x * facing, knockbackVelocity.y);
        hitStunTimer = hitStunTime;
    }

    public void Die()
    {
        isDead = true;
        Vector2 velocity = rigidBody2D.linearVelocity;
        velocity.x = 0f;
        rigidBody2D.linearVelocity = velocity;
        PlayOneShot(deathClip);
    }

    void PlayOneShot(AudioClip clip)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }
        
        audioSource.PlayOneShot(clip);
    }

    void HandleMoveStartSound()
    {
        bool isMoving = isGrounded && Mathf.Abs(moveInputX) > minInputToMove;
        if (isMoving && !wasMoving)
        {
            PlayOneShot(moveStartClip);
        }

        wasMoving = isMoving;
    }

    void HandleLandSound()
    {
        if (!wasGrounded && isGrounded)
        {
            PlayOneShot(landClip);
        }
    }

    void UpdateAnimation()
    {
        string nextAnim;
        if (isDead)
        {
            nextAnim = "Death";
        }
        else if (hitStunTimer > 0f)
        {
            nextAnim = "Damage";
        }
        else if (!isGrounded)
        {
            nextAnim = "Jump";
        }
        else if (moveInputX != 0f)
        {
            nextAnim = "Move";
        }
        else
        {
            nextAnim = "Idle";
        }
        
        if (nextAnim == currentAnim)
        {
            return;
        }
        
        animator.SetTrigger(nextAnim);
        currentAnim = nextAnim;
    }

    void HideAfterDeath()
    {
        if (!isDead || !spriteRenderer.enabled)
        {
            return;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        if (state.IsName("Death") && state.normalizedTime >= 1f)
        {
            spriteRenderer.enabled = false;
        }
    }
}
