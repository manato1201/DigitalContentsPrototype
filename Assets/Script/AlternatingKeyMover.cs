using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // ★★★ この行を必ず追加してください！ ★★★

/// <summary>
/// 【Rigidbody2D対応・最新API版】物理演算ベースで移動するクラス
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AlternatingKeyMover : MonoBehaviour
{
    private enum MoveState { None, Jumping, MovingHorizontally, Landing, Finished }

    [Header("キー設定")]
    [SerializeField] private KeyCode keyOne = KeyCode.A;
    [SerializeField] private KeyCode keyTwo = KeyCode.D;

    [Header("ゲージ設定")]
    [SerializeField] private float maxGauge = 100f;
    [SerializeField] private float initialGaugeIncrease = 20f;
    [SerializeField] private float gaugeIncreaseModifier = 0.9f;
    [SerializeField] private float gaugeDecreaseRate = 5f;

    [Header("物理挙動設定")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("接地判定設定")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    [Header("ステージ設定")] // ★ この行が欠けていた可能性があります
    [SerializeField] private List<Vector2> targetPositions = new List<Vector2>();

    [Header("UI設定")] // ★追加
    [SerializeField] private Slider gaugeSlider; // ★追加：UIのスライダーを格納する変数


    // --- 内部変数 ---
    private Rigidbody2D rb;
    private float _currentGauge = 0f;
    private float _currentGaugeIncrease;
    private KeyCode _lastKeyPressed;
    private MoveState _currentMoveState = MoveState.None;
    private int _currentTargetIndex = 0;

    private const float k_GaugeThreshold = 0.05f;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        _currentGaugeIncrease = initialGaugeIncrease;
    }

    void Update()
    {
        if (_currentMoveState == MoveState.None)
        {
            HandleInputAndGauge();
        }
    }

    void FixedUpdate()
    {
        if (_currentMoveState != MoveState.None && _currentMoveState != MoveState.Finished)
        {
            HandleMovement();
        }
    }

    // ★★★ ゲージUIを更新するための専用メソッドを追加 ★★★
    private void UpdateGaugeUI()
    {
        // gaugeSliderが設定されている場合のみ処理を行う
        if (gaugeSlider != null)
        {
            // Sliderの値を、現在のゲージの割合（0.0～1.0）に設定する
            gaugeSlider.value = _currentGauge / maxGauge;
        }
    }

    private void HandleInputAndGauge()
    {
        HandleInput();
        DecreaseGaugeOverTime();

        bool isGaugeFull = _currentGauge + k_GaugeThreshold >= maxGauge;
        if (isGaugeFull)
        {
            if (_currentTargetIndex < targetPositions.Count)
            {
                StartMoveSequence();
            }
            else if (_currentMoveState != MoveState.Finished)
            {
                _currentMoveState = MoveState.Finished;
                Debug.Log("全ステージクリア！");
            }
        }
    }

    private void StartMoveSequence()
    {
        _currentMoveState = MoveState.Jumping;

        // ★ API変更に対応: velocity -> linearVelocity
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
    }

    private void HandleMovement()
    {
        switch (_currentMoveState)
        {
            case MoveState.Jumping:
                _currentMoveState = MoveState.MovingHorizontally;
                break;

            case MoveState.MovingHorizontally:
                float targetX = targetPositions[_currentTargetIndex].x;
                float direction = Mathf.Sign(targetX - transform.position.x);

                // ★ API変更に対応: velocity -> linearVelocity
                rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

                if (Mathf.Abs(transform.position.x - targetX) < 0.1f)
                {
                    // ★ API変更に対応: velocity -> linearVelocity
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    _currentMoveState = MoveState.Landing;
                }
                break;

            case MoveState.Landing:
                if (IsGrounded())
                {
                    GoToNextStage();
                }
                break;
        }
    }

    private void GoToNextStage()
    {
        // ★ API変更に対応: velocity -> linearVelocity
        rb.linearVelocity = Vector2.zero;
        _currentTargetIndex++;
        _currentGaugeIncrease *= gaugeIncreaseModifier;
        _currentGauge = 0f;
        _lastKeyPressed = KeyCode.None;
        _currentMoveState = MoveState.None;
        UpdateGaugeUI(); // ★追加
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(keyOne) && _lastKeyPressed != keyOne) { IncreaseGauge(); _lastKeyPressed = keyOne; }
        else if (Input.GetKeyDown(keyTwo) && _lastKeyPressed != keyTwo) { IncreaseGauge(); _lastKeyPressed = keyTwo; }
    }
    private void IncreaseGauge()
    {
        _currentGauge += _currentGaugeIncrease;
        _currentGauge = Mathf.Clamp(_currentGauge, 0f, maxGauge);
        UpdateGaugeUI(); // ★追加
    }
    private void DecreaseGaugeOverTime()
    {
        if (_currentGauge > 0) { _currentGauge -= gaugeDecreaseRate * Time.deltaTime; _currentGauge = Mathf.Clamp(_currentGauge, 0f, maxGauge); }
        UpdateGaugeUI(); // ★追加
    }
}