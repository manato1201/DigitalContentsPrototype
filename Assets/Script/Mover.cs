using UnityEngine;

namespace Game.Movement
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Mover : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void MoveTo(Vector2 targetPosition)
        {
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }

        public void Stop()
        {
            rb.linearVelocity = Vector2.zero;
        }

        public void WarpTo(Vector2 targetPosition)
        {
            rb.linearVelocity = Vector2.zero;
            transform.position = targetPosition;
        }
    }
}