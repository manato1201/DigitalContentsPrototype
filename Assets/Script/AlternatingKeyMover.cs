using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 【エラー修正・バトル機能統合版】
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AlternatingKeyMover : MonoBehaviour
{


    // ★ 敗北・復活の状態を追加
    private enum GameState { Normal, Moving, Battle, Result, PlayerLosing_Knockback, PlayerLosing_Revival, PlayerLosing_ReturnMove, Finished }
    private enum BattleResult { Win, Lose, Draw }

    [Header("カメラ設定")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera defeatCamera;

    [Header("敗北・復活設定")]
    [SerializeField] private Vector2 defeatPosition; // 吹き飛ばされる先の座標
    [SerializeField] private Vector2 revivalReturnPosition; // 復活時に移動する中間座標
    [SerializeField] private Vector2 finalRevivalPosition; // 最終的に瞬間移動する復帰座標
    [SerializeField] private float revivalGaugeDecreaseAccelerator = 0.2f; // 復活中のゲージ減少加速量


    // --- Inspectorで設定する項目 ---
    [Header("キー設定")]
    [SerializeField] private KeyCode keyOne = KeyCode.D;
    [SerializeField] private KeyCode keyTwo = KeyCode.K;

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

    [Header("ステージ設定")]
    [SerializeField] private List<Vector2> targetPositions = new List<Vector2>();

    [Header("UI設定")]
    [SerializeField] private Slider gaugeSlider;
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private TextMeshProUGUI battleResultText;

    [Header("バトル設定")]
    [SerializeField] private Transform enemyCheckPoint;
    [SerializeField] private float enemyCheckRadius = 0.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Vector2 knockbackForce = new Vector2(5f, 5f);
    [SerializeField] private float knockbackRotation = 720f;


    // --- 内部変数 ---
    private Rigidbody2D rb;
    private GameState currentGameState = GameState.Normal;
    private GameObject currentEnemy;
    private Vector3 initialScale;
    private float _currentGauge = 0f;
    private float _currentGaugeIncrease;
    private float _currentGaugeDecreaseRate; // 動的に変更するための変数
    private KeyCode _lastKeyPressed;
    private int _currentTargetIndex = 0;
    private const float k_GaugeThreshold = 0.05f;

    // --- Unityライフサイクルメソッド ---

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialScale = transform.localScale;
        if (battlePanel != null) battlePanel.SetActive(false);
    }

    void Start()
    {
        _currentGaugeIncrease = initialGaugeIncrease;
        _currentGaugeDecreaseRate = gaugeDecreaseRate;
        UpdateGaugeUI();
        SwitchCamera(false); // 最初にメインカメラを有効化
    }

    void Update()
    {
        // ゲームの状態に応じて入力を受け付ける
        switch (currentGameState)
        {
            case GameState.Normal:
                HandleInputAndGauge(false); // 通常のゲージ処理
                break;
            case GameState.PlayerLosing_Revival:
                HandleInputAndGauge(true); // 復活中のゲージ処理
                break;
        }
        CheckGameClearCondition();
    }

    void FixedUpdate()
    {
        // 物理的な移動処理を状態に応じて実行
        switch (currentGameState)
        {
            // ★新しいケースを追加
            case GameState.PlayerLosing_Knockback:
                HandleKnockbackMove();
                break;

            case GameState.PlayerLosing_ReturnMove:
                HandleReturnMove();
                break;
        }
    }

    // --- メインフロー ---

    private void HandleInputAndGauge(bool isRevival)
    {
        HandleInput();
        DecreaseGaugeOverTime(isRevival);

        bool isGaugeFull = _currentGauge + k_GaugeThreshold >= maxGauge;
        if (isGaugeFull)
        {
            if (isRevival)
            {
                // 復活ゲージが満タンになった場合
                StartCoroutine(ReturnSequence());
            }
            else
            {
                // 通常ゲージが満タンになった場合
                CheckForEnemy();
            }
        }
    }

    private void DecreaseGaugeOverTime(bool isRevival)
    {
        if (_currentGauge > 0)
        {
            // 復活中はゲージ減少量が徐々に増えていく
            if (isRevival)
            {
                _currentGaugeDecreaseRate += revivalGaugeDecreaseAccelerator * Time.deltaTime;
            }
            _currentGauge -= _currentGaugeDecreaseRate * Time.deltaTime;
            _currentGauge = Mathf.Clamp(_currentGauge, 0f, maxGauge);
            UpdateGaugeUI();
        }
    }

    private void CheckForEnemy()
    {
        if (_currentTargetIndex >= targetPositions.Count)
        {
            Debug.Log("全ステージクリア！");
            currentGameState = GameState.Finished;
            return;
        }

        if (enemyCheckPoint == null)
        {
            Debug.LogWarning("【要確認】Inspectorで 'Enemy Check Point' が設定されていません。");
            StartCoroutine(NormalMoveSequence());
            return;
        }

        // --- ★★★ ここからデバッグコード ★★★ ---
        Debug.Log($"索敵を実行中... 中心点: {enemyCheckPoint.position}, 半径: {enemyCheckRadius}");

        Collider2D enemyCollider = Physics2D.OverlapCircle(enemyCheckPoint.position, enemyCheckRadius, enemyLayer);

        if (enemyCollider != null)
        {
            // 何か検出した場合、その情報をログに出す
            Debug.Log($"<color=cyan>索敵範囲内にコライダーを検出！</color> " +
                      $"名前: '{enemyCollider.gameObject.name}', " +
                      $"タグ: '{enemyCollider.tag}', " +
                      $"レイヤー: '{LayerMask.LayerToName(enemyCollider.gameObject.layer)}'");

            // タグが一致するかチェック
            if (enemyCollider.CompareTag("Player"))
            {
                Debug.Log("<color=green>タグが 'Player' と一致しました。バトルを開始します。</color>");
                currentEnemy = enemyCollider.gameObject;
                StartBattle();
            }
            else
            {
                // タグが一致しなかった場合
                Debug.LogWarning($"<color=yellow>コライダーを検出しましたが、タグが 'Player' ではありませんでした。通常移動を開始します。</color>");
                StartCoroutine(NormalMoveSequence());
            }
        }
        else
        {
            // 何も検出しなかった場合
            Debug.LogWarning("<color=red>索敵範囲内に 'Enemy' レイヤーのコライダーが見つかりませんでした。通常移動を開始します。</color>");
            StartCoroutine(NormalMoveSequence());
        }
    }

    // --- バトル関連のメソッド ---

    private void StartBattle()
    {
        currentGameState = GameState.Battle;
        // ★修正：isKinematic -> bodyType
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        if (battleResultText != null) battleResultText.text = "Make Your Choice";
        if (battlePanel != null) battlePanel.SetActive(true);
    }

    public void OnPlayerChoice(int playerChoice)
    {
        if (currentGameState != GameState.Battle) return;

        int enemyChoice = Random.Range(0, 3);
        // ★修正：(BattleResult)にキャストしてenumに変換
        BattleResult result = (BattleResult)((playerChoice - enemyChoice + 3) % 3);

        StartCoroutine(HandleBattleResult(result));
    }

    private IEnumerator HandleBattleResult(BattleResult result)
    {
        currentGameState = GameState.Result;
        if (battlePanel != null) battlePanel.SetActive(false);
        // ★修正：isKinematic -> bodyType
        rb.bodyType = RigidbodyType2D.Dynamic;

        switch (result)
        {
            case BattleResult.Win:
                if (battleResultText != null) battleResultText.text = "あなたの勝ち！";
                Debug.Log("勝利");
                KnockbackTarget(currentEnemy);
                yield return new WaitForSeconds(2f);
                GoToNextStage();
                break;
            case BattleResult.Lose:
                if (battleResultText != null) battleResultText.text = "あなたの負け...";
                Debug.Log("敗北");
                // ★ 敗北シーケンスを開始
                StartCoroutine(PlayerLosingSequence());
               // KnockbackTarget(this.gameObject);
                //yield return new WaitForSeconds(2f);
                //ResetForRetry();
                break;
            case BattleResult.Draw:
                if (battleResultText != null) battleResultText.text = "あいこ！もう一度！";
                Debug.Log("あいこ");
                yield return new WaitForSeconds(1.5f);
                StartBattle();
                break;
        }
    }

    private void KnockbackTarget(GameObject target)
    {
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb == null) return;

        // ★修正：isKinematic -> bodyType
        targetRb.bodyType = RigidbodyType2D.Dynamic;
        Vector2 knockbackDir = (target.transform.position - this.transform.position).normalized;
        if (knockbackDir == Vector2.zero) knockbackDir = Vector2.up;

        // ★修正：velocity -> linearVelocity
        targetRb.linearVelocity = Vector2.zero;
        targetRb.AddForce(new Vector2(knockbackDir.x * knockbackForce.x, knockbackForce.y), ForceMode2D.Impulse);
        targetRb.AddTorque(knockbackRotation * (Random.value > 0.5f ? 1f : -1f));
    }

    private void ResetForRetry()
    {
        Debug.Log("ゲームの進行状況を初期化します。");

        // ★追加：ステージ進行度を最初に戻す
        _currentTargetIndex = 0;

        // ★追加：ゲージ関連を完全に初期値に戻す
        _currentGauge = 0f;
        _currentGaugeIncrease = initialGaugeIncrease;
        _currentGaugeDecreaseRate = gaugeDecreaseRate; // 加速した分をリセット
        UpdateGaugeUI();

        // 入力履歴とゲーム状態をリセット
        _lastKeyPressed = KeyCode.None;
        currentGameState = GameState.Normal;
    }

    // --- 通常移動のメソッド ---

    private IEnumerator NormalMoveSequence()
    {
        currentGameState = GameState.Moving;

        // ★修正：velocity -> linearVelocity
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
        yield return new WaitForSeconds(0.1f);

        while (currentGameState == GameState.Moving)
        {
            float targetX = targetPositions[_currentTargetIndex].x;
            if (Mathf.Abs(transform.position.x - targetX) < 0.2f)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                break;
            }
            float direction = Mathf.Sign(targetX - transform.position.x);
            // ★修正：velocity -> linearVelocity
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
            yield return new WaitForFixedUpdate();
        }

        while (currentGameState == GameState.Moving)
        {
            if (IsGrounded())
            {
                GoToNextStage();
                break;
            }
            yield return new WaitForFixedUpdate();
        }
    }





    // --- 敗北・復活シーケンス ---

    private IEnumerator PlayerLosingSequence()
    {
        // 状態を設定し、初期アクション（向き反転、回転開始）を行う
        currentGameState = GameState.PlayerLosing_Knockback;
        transform.localScale = new Vector3(-initialScale.x, initialScale.y, initialScale.z);

        // 回転だけさせる（移動はFixedUpdateに任せる）
        if (rb != null)
        {
            rb.angularVelocity = knockbackRotation;
        }

        yield return null; // コルーチンはすぐに次のフレームで終了
    }


    private void HandleKnockbackMove()
    {
        // 目的地に十分に近づいたか判定
        if (Vector2.Distance(transform.position, defeatPosition) < 0.1f)
        {
            // --- 到着後の処理 ---
            rb.bodyType = RigidbodyType2D.Kinematic; // 物理的な影響を完全に止める
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            transform.position = defeatPosition; // 座標を正確に合わせる
            transform.rotation = Quaternion.identity;

            SwitchCamera(true); // ★カメラを切り替える

            // 復活フェーズへ移行
            currentGameState = GameState.PlayerLosing_Revival;
            _currentGauge = 0f;
            _currentGaugeDecreaseRate = gaugeDecreaseRate;
            UpdateGaugeUI();
        }
        else
        {
            // --- 目的地に向かって移動し続ける処理 ---
            // 物理演算と協調しながら目的地へ移動させる
            Vector2 direction = (defeatPosition - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
    }
    private IEnumerator ReturnSequence()
    {
        currentGameState = GameState.PlayerLosing_ReturnMove;

        // 復帰ポイント到着を待つ
        while (Vector2.Distance(transform.position, revivalReturnPosition) > 0.5f)
        {
            // この間の移動はFixedUpdateのHandleReturnMoveで行われる
            yield return null;
        }

        // 到着後の処理
        SwitchCamera(false); // メインカメラに戻す
        transform.position = finalRevivalPosition; // 最終復帰ポイントへ瞬間移動
        transform.localScale = initialScale; // 向きを元に戻す

        ResetForRetry(); // ゲージなどをリセットして通常状態へ
    }

    private void HandleReturnMove()
    {
        // 復帰ポイントへ向かって移動
        Vector2 direction = (revivalReturnPosition - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    // --- 補助メソッド ---

    private void SwitchCamera(bool toDefeatCam)
    {
        if (mainCamera != null) mainCamera.enabled = !toDefeatCam;
        if (defeatCamera != null) defeatCamera.enabled = toDefeatCam;
    }
   

    private void GoToNextStage()
    {
        // まずプレイヤーの動きを完全に止める
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f; // 回転も止める

        // ★★★ ここからが変更点 ★★★

        // 現在クリアしたステージが、リストの最後のステージかどうかを判定
        // (リストのインデックスは0から始まるため、最後のインデックスは「要素数 - 1」)
        if (_currentTargetIndex >= targetPositions.Count - 1)
        {
            // --- 最後のステージだった場合の処理 ---
            Debug.Log("<color=magenta><b>最終座標に到達！ 全stageクリア！</b></color>");
            currentGameState = GameState.Finished; // ゲームの状態を「完了」にする

            // ゲージUIを空にしておく
            _currentGauge = 0f;
            UpdateGaugeUI();
        }
        else
        {
            // --- まだ次のステージがある場合の処理（これまで通り） ---
            Debug.Log($"ステージ {_currentTargetIndex + 1} クリア！ 次のステージへ。");
            _currentTargetIndex++; // 次の目標へインデックスを進める
            _currentGaugeIncrease *= gaugeIncreaseModifier;
            _currentGauge = 0f;
            _lastKeyPressed = KeyCode.None;
            currentGameState = GameState.Normal; // 再び入力待ち状態に戻す
            UpdateGaugeUI();
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
    }

    private void UpdateGaugeUI()
    {
        if (gaugeSlider != null) { gaugeSlider.value = _currentGauge / maxGauge; }
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
        UpdateGaugeUI();
    }
    private void DecreaseGaugeOverTime()
    {
        if (_currentGauge > 0)
        {
            _currentGauge -= gaugeDecreaseRate * Time.deltaTime;
            _currentGauge = Mathf.Clamp(_currentGauge, 0f, maxGauge);
            UpdateGaugeUI();
        }
    }


    private void CheckGameClearCondition()
    {
        // ゲームがまだ終わっておらず、現在の目標が最後のステージの場合のみチェック
        if (currentGameState != GameState.Finished && _currentTargetIndex == targetPositions.Count - 1)
        {
            // 最後の目標座標を取得
            Vector2 finalTargetPosition = targetPositions[_currentTargetIndex];

            // 最後の目標座標との距離が非常に近く、かつ接地しているか判定
            if (Vector2.Distance(transform.position, finalTargetPosition) < 0.2f && IsGrounded())
            {
                // クリア条件を満たした場合
                Debug.Log("<color=magenta><b>最終座標に到達し、着地を確認！ 全stageクリア！</b></color>");
                currentGameState = GameState.Finished; // ゲームの状態を「完了」にする

                // プレイヤーの動きを完全に止める
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;

                // UIも最終状態にする
                _currentGauge = 0f;
                UpdateGaugeUI();
            }
        }
    }
}