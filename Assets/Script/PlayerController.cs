using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
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

    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 2f;

    [Header("ステージ設定")]
    [SerializeField] private List<Vector2> targetPositions = new List<Vector2>();

    private float _currentGauge = 0f;
    private float _currentGaugeIncrease;
    private KeyCode _lastKeyPressed;
    private MoveState _currentMoveState = MoveState.None;
    private int _currentTargetIndex = 0;
    private Vector2 _startPosition;
    private Vector2 _peakPosition;

    public float CurrentGauge => _currentGauge;
    public float MaxGauge => maxGauge;
    public int CurrentStage => _currentTargetIndex + 1;

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
        else
        {
            HandleMovement();
        }
    }

    private void HandleMovement()
    {
        switch (_currentMoveState)
        {
            case MoveState.Jumping: HandleJumping(); break;
            case MoveState.MovingHorizontally: HandleHorizontalMove(); break;
            case MoveState.Landing: HandleLanding(); break;
        }
    }

    private void HandleInputAndGauge()
    {
        HandleInput();
        DecreaseGaugeOverTime();

        // ★★★ ここが最も重要なチェックポイントです ★★★
        // 浮動小数点数の誤差を考慮し、ほぼ等しい場合もチェックに含めます
        bool isGaugeFull = _currentGauge >= maxGauge || Mathf.Approximately(_currentGauge, maxGauge);

        if (isGaugeFull)
        {
            // 【デバッグログ 1】
            // このログが表示されるかどうかが最重要です
            Debug.Log($"<color=green><b>【診断】isGaugeFullがtrueになりました！ (ゲージ: {_currentGauge})</b></color>");

            if (_currentTargetIndex < targetPositions.Count)
            {
                Debug.Log("<color=green><b>【診断】条件クリア！ 移動シーケンスを開始します。</b></color>");
                StartMoveSequence();
            }
            else
            {
                Debug.LogWarning("【診断】移動シーケンスを開始できません。Target Positionsリストが空か、全てクリア済みです。");
                if (_currentMoveState != MoveState.Finished)
                {
                    _currentMoveState = MoveState.Finished;
                }
            }
        }
    }

    private void IncreaseGauge()
    {
        _currentGauge += _currentGaugeIncrease;
        _currentGauge = Mathf.Clamp(_currentGauge, 0f, maxGauge);
        // このログで100と表示されていることを確認
        Debug.Log($"ゲージ: {_currentGauge.ToString("F1")} / {maxGauge}");
    }

    // --- 以下、変更のないメソッド群 ---

    private void HandleInput()
    {
        if (Input.GetKeyDown(keyOne) && _lastKeyPressed != keyOne) { IncreaseGauge(); _lastKeyPressed = keyOne; }
        else if (Input.GetKeyDown(keyTwo) && _lastKeyPressed != keyTwo) { IncreaseGauge(); _lastKeyPressed = keyTwo; }
    }
    private void DecreaseGaugeOverTime()
    {
        if (_currentGauge > 0) { _currentGauge -= gaugeDecreaseRate * Time.deltaTime; _currentGauge = Mathf.Clamp(_currentGauge, 0f, maxGauge); }
    }
    private void StartMoveSequence()
    {
        _currentGauge = maxGauge; _startPosition = transform.position;
        _peakPosition = new Vector2(_startPosition.x, _startPosition.y + jumpHeight);
        _currentMoveState = MoveState.Jumping;
    }
    private void HandleJumping()
    {
        Vector2 target = new Vector2(transform.position.x, _peakPosition.y); MoveTowards2D(target);
        if (Mathf.Abs(transform.position.y - _peakPosition.y) < 0.01f) { _currentMoveState = MoveState.MovingHorizontally; }
    }
    private void HandleHorizontalMove()
    {
        Vector2 currentTarget = targetPositions[_currentTargetIndex]; Vector2 target = new Vector2(currentTarget.x, transform.position.y);
        MoveTowards2D(target); if (Mathf.Abs(transform.position.x - target.x) < 0.01f) { _currentMoveState = MoveState.Landing; }
    }
    private void HandleLanding()
    {
        Vector2 target = targetPositions[_currentTargetIndex]; MoveTowards2D(target);
        if (Vector2.Distance(transform.position, target) < 0.01f) { transform.position = new Vector3(target.x, target.y, transform.position.z); GoToNextStage(); }
    }
    private void GoToNextStage()
    {
        _currentTargetIndex++; _currentGaugeIncrease *= gaugeIncreaseModifier; _currentGauge = 0f;
        _lastKeyPressed = KeyCode.None; _currentMoveState = MoveState.None;
    }
    private void MoveTowards2D(Vector2 target)
    {
        float z = transform.position.z; Vector2 newPos = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        transform.position = new Vector3(newPos.x, newPos.y, z);
    }
}