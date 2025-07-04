using UnityEngine;

namespace Game.Movement
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Mover : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayer;

        private Rigidbody2D rb;
        private bool hasJumped = false;
        private Vector2? targetPos = null;

        private float targetArrivalThreshold = 0.2f;


        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        // 一度ジャンプし、その後ターゲット座標へ向かって移動
        public void JumpAndMoveTo(Vector2 position)
        {
            if (IsGrounded())
            {
                hasJumped = true;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // 縦速度リセット
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                targetPos = position;
            }
        }

        public bool IsAtTarget()
        {
            if (!targetPos.HasValue) return false;
            return Mathf.Abs(transform.position.x - targetPos.Value.x) < targetArrivalThreshold && IsGrounded();
        }

        private void FixedUpdate()
        {
            if (hasJumped && targetPos.HasValue)
            {
                // Y軸速度がマイナス＝落下中で、かつ接地していない場合のみX移動
                if (!IsGrounded())
                {
                    float direction = Mathf.Sign(targetPos.Value.x - transform.position.x);
                    rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
                }
                else if (Mathf.Abs(transform.position.x - targetPos.Value.x) < 0.2f)
                {
                    // 到達とみなしたら停止
                    rb.linearVelocity = Vector2.zero;
                    hasJumped = false;
                    targetPos = null;
                }
            }
        }

        private bool IsGrounded()
        {
            if (groundCheck == null) return false;
            return Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
        }
    }
}