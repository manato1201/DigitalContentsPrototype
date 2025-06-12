using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // public変数（パスカルケース）
    public float JumpForce = 5.0f;
    public float MoveDistance = 2.0f;

    // 定数（k_）
    private const float k_JumpCooldown = 0.2f;

    // private変数（キャメルケース）
    private Rigidbody2D playerRb;
    private bool isMovingLeft = false; // boolは動詞
    private float lastJumpTime = 0.0f;
    private bool m_IsReady = true; // プライベートメンバー

    void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // d/kキー入力を監視
        if (Input.GetKeyDown(KeyCode.D) && !isMovingLeft && m_IsReady)
        {
            isMovingLeft = true;
            Jump(Vector2.left);
        }
        else if (Input.GetKeyDown(KeyCode.K) && isMovingLeft && m_IsReady)
        {
            isMovingLeft = false;
            Jump(Vector2.right);
        }
    }

    private void Jump(Vector2 direction)
    {
        m_IsReady = false;
        playerRb.linearVelocity = Vector2.zero; // 動きをリセット
        // MoveDistanceで指定した方向にジャンプ
        playerRb.AddForce(new Vector2(direction.x * MoveDistance, 1f) * JumpForce, ForceMode2D.Impulse);
        Invoke(nameof(ResetJump), k_JumpCooldown);
    }

    private void ResetJump()
    {
        m_IsReady = true;
    }
}