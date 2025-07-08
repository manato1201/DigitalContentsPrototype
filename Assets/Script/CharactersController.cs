using System;
using UnityEngine;
using UnityEngine.InputSystem;
using SpeedControl;
using Unity.VisualScripting;
using System.Collections.Generic;
using DB;
//using SpeedControl;

namespace Characters
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Player2DController : MonoBehaviour, IFilterableSpeed
    {

        [Header("移動パラメータ")]
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float moveDuration = 0.5f; // 座標到達までの時間（秒）

        private Rigidbody2D rb;
        private Speed speed = new Speed();

        public bool UseSkillControlledSpeed { get; set; } = true;

        // スクリプタブルオブジェクト
        private DB_Coordinate dbCoordinate;

        // ジャンプ後の移動
        private bool isMovingToTarget = false;
        private Vector2 targetPos;
        private float moveTimer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            speed.OnValueChange += OnSpeedChanged;
            BattleSpeedController.Instance.Subscribe(speed);

            dbCoordinate = DB_Coordinate.Entity; // Resources/DB_Coordinate.assetから取得
        }

        private void OnDestroy()
        {
            BattleSpeedController.Instance.Unsubscribe(speed);
            speed.OnValueChange -= OnSpeedChanged;
        }

        // フリック検出アクション（InputSystemのActionでSwipeやPointerDeltaなどを登録して割当）
        public void OnSwipe(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            Vector2 swipeDir = context.ReadValue<Vector2>();
            string destinationName = GetDestinationNameBySwipe(swipeDir);

            // 目的地名から座標取得
            Vector2? destination = GetCoordinateByName(destinationName);
            if (destination.HasValue)
            {
                // まずジャンプ
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                // 移動準備
                targetPos = destination.Value;
                moveTimer = 0f;
                isMovingToTarget = true;
            }
            else
            {
                Debug.LogWarning($"目的地「{destinationName}」が見つかりませんでした");
            }
        }

        private void FixedUpdate()
        {
            if (isMovingToTarget)
            {
                moveTimer += Time.fixedDeltaTime * speed.CurrentSpeed;
                float t = Mathf.Clamp01(moveTimer / moveDuration);

                // 曲線移動やイージングしたい場合はここで変換してもOK
                rb.position = Vector2.Lerp(rb.position, targetPos, t);

                // 到達判定
                if (t >= 1f || Vector2.Distance(rb.position, targetPos) < 0.05f)
                {
                    rb.position = targetPos;
                    rb.linearVelocity = Vector2.zero;
                    isMovingToTarget = false;
                }
            }
        }

        // スクリプタブルオブジェクトから座標取得
        private Vector2? GetCoordinateByName(string name)
        {
            foreach (var nc in dbCoordinate.Coordinates)
            {
                if (nc.Name == name && nc.XCoordinate.Length > 0 && nc.YCoordinate.Length > 0)
                {
                    return new Vector2(nc.XCoordinate[0], nc.YCoordinate[0]);
                }
            }
            return null;
        }

        // フリック方向→目的地名変換（必要に応じて工夫）
        private string GetDestinationNameBySwipe(Vector2 swipe)
        {
            // 例：上フリックなら "PointA"、右フリックなら "PointB"など
            if (swipe.y > Mathf.Abs(swipe.x))
                return "PointA";
            else if (swipe.y < -Mathf.Abs(swipe.x))
                return "PointC";
            else if (swipe.x > 0)
                return "PointB";
            else
                return "PointD";
        }

        private void OnSpeedChanged(float newSpeed)
        {
            // 速度変化時に即時反映したいロジックがあればここに
        }
    }
}

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy2DController : MonoBehaviour, IFilterableSpeed
{
    [SerializeField] private float movePower = 3f;
    [SerializeField] private Transform target; // 追いかけたい対象（プレイヤーなど）

    private Rigidbody2D rb;
    private Speed speed = new Speed();

    public bool UseSkillControlledSpeed { get; set; } = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        speed.OnValueChange += OnSpeedChanged;
        BattleSpeedController.Instance.Subscribe(speed);
    }

    private void OnDestroy()
    {
        BattleSpeedController.Instance.Unsubscribe(speed);
        speed.OnValueChange -= OnSpeedChanged;
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        Vector2 direction = ((Vector2)target.position - rb.position).normalized;
        rb.linearVelocity = direction * movePower * speed.CurrentSpeed;
    }

    private void OnSpeedChanged(float newSpeed)
    {
        // 速度変化時に即時反映したいロジックがあればここに
    }
}
