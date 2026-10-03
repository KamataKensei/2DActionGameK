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

    float minInputToMove = 0.2f;
    Rigidbody2D rigidBody2D;
    SpriteRenderer spriteRenderer;
    InputAction moveAction;
    InputAction jumpAction;
    float moveInputX;
    int facing;
    bool isGrounded;
    int currentHp;

    void Awake()
    {
        rigidBody2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        Vector2 move = moveAction.ReadValue<Vector2>();
        moveInputX = move.x;

        if(Mathf.Abs(moveInputX) < minInputToMove)
        {
            moveInputX = 0f;
        }

        UpdateFacing();
        UpdateGrounded();
        TryJump();
    }

    private void FixedUpdate()
    {
        Vector2 velocity = rigidBody2D.linearVelocity;
        velocity.x = moveInputX * moveSpeed;
        rigidBody2D.linearVelocity = velocity;

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
    }
}
