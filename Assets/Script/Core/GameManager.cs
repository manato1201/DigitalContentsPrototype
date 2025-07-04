using UnityEngine;
using System.Collections;
using Game.Movement;
using Game.Battle;
using Game.Warp;
using Game.UI;
using Game.Inputs;
using Game.Core; // CameraSwitcher
using System.Collections.Generic;

namespace Game.Core
{
    public class GameManager : MonoBehaviour
    {
        // 参照系
        [Header("Managers & Components")]
        [SerializeField] private Mover playerMover;
        [SerializeField] private CameraSwitcher cameraSwitcher;
        [SerializeField] private PlayerInputManager inputManager;
        [SerializeField] private GaugeManager gaugeManager;
        [SerializeField] private EnemyDetector enemyDetector;
        [SerializeField] private BattleUI battleUI;
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private WarpManager warpManager;
        [SerializeField] private RevivalManager revivalManager;

        [Header("座標設定")]
        [SerializeField] private Vector2 winWarpPos;
        [SerializeField] private Vector2 winReturnPos;
        [SerializeField] private Vector2 loseDefeatPos;
        [SerializeField] private Vector2 loseReturnPos;

        [Header("移動目標リスト")]
        [SerializeField] private List<Vector2> moveTargetPositions = new List<Vector2>();

        private enum GameState
        {
            Idle,
            JumpMoving,
            GaugeCharging,
            SearchingEnemy,
            Battle,
            Result,
            WinWarp,
            LoseWarp,
            Revival,
        }
        private GameState state = GameState.Idle;

        private void Start()
        {
            cameraSwitcher.SwitchToMain();
            StartCoroutine(MainFlow());
        }

        private IEnumerator MainFlow()
        {
            int currentTargetIndex = 0;

            while (currentTargetIndex < moveTargetPositions.Count)
            {
                // 1. ゲージ入力待ち
                gaugeManager.ResetGauge();
                inputManager.ResetKeyHistory();
                inputManager.OnKeyPressed += OnGaugeKeyInput;
                yield return new WaitUntil(() => gaugeManager.IsFull());
                inputManager.OnKeyPressed -= OnGaugeKeyInput;

                // 2. 敵がいるか判定
                Vector2 targetPos = moveTargetPositions[currentTargetIndex];

                // --- CheckForEnemy参考：プレイヤーを一時的にターゲット座標へ（敵の有無確認用）
                GameObject foundEnemy = CheckForEnemyAtPosition(targetPos);
                if (foundEnemy == null)
                {
                    // 敵がいなければ移動して次へ
                    playerMover.JumpAndMoveTo(targetPos);
                    yield return new WaitUntil(() => playerMover.IsAtTarget());
                    currentTargetIndex++;
                }
                else
                {
                    // 敵がいた場合：バトル
                    bool winOrLose = false; // 勝敗がつくまでループ
                    while (!winOrLose)
                    {
                        BattleManager.BattleResult result = BattleManager.BattleResult.Draw;
                        yield return StartCoroutine(BattlePhase(r => result = r));

                        if (result == BattleManager.BattleResult.Draw)
                        {
                            // あいこなら再バトル
                            Debug.Log("あいこでもう一度バトル！");
                        }
                        else
                        {
                            // 勝敗が決まったら移動
                            yield return StartCoroutine(HandleResult(result));
                            playerMover.JumpAndMoveTo(targetPos);
                            yield return new WaitUntil(() => playerMover.IsAtTarget());
                            currentTargetIndex++;
                            winOrLose = true;
                        }
                    }
                }
            }

            Debug.Log("<color=lime><b>クリア！</b></color>");
        }

        private GameObject CheckForEnemyAtPosition(Vector2 position)
        {
            float checkRadius = 0.5f; // 調整可
            LayerMask enemyLayer = enemyDetector.EnemyLayer; // EnemyDetector側にプロパティ追加推奨
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(position, checkRadius, enemyLayer);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Enemy"))
                {
                    return hitCollider.gameObject;
                }
            }
            return null;
        }

        // BattlePhaseの引数をAction<BattleResult>でコールバック
        private IEnumerator BattlePhase(System.Action<BattleManager.BattleResult> onResult)
        {
            bool choiceMade = false;
            int playerChoice = -1;
            battleUI.Show(idx =>
            {
                playerChoice = idx;
                choiceMade = true;
            });
            yield return new WaitUntil(() => choiceMade);

            bool resultDone = false;
            BattleManager.BattleResult result = BattleManager.BattleResult.Draw;
            battleManager.StartBattle(playerChoice, r =>
            {
                result = r;
                resultDone = true;
            });
            yield return new WaitUntil(() => resultDone);

            onResult(result);
        }


        
        

        // ゲージ増加用（交互押し）
        private void OnGaugeKeyInput(KeyCode key)
        {
            gaugeManager.IncreaseGauge();
        }

        // 勝敗による処理（コルーチン化）
        private IEnumerator HandleResult(BattleManager.BattleResult result)
        {
            switch (result)
            {
                case BattleManager.BattleResult.Win:
                    Debug.Log("勝利");
                    state = GameState.WinWarp;
                    // 勝利時：カメラそのまま、ワープして戻す
                    yield return StartCoroutine(warpManager.WarpAndReturn(
                        playerMover.gameObject, winWarpPos, winReturnPos, 4f
                    ));
                    break;

                case BattleManager.BattleResult.Lose:
                    Debug.Log("敗北");
                    state = GameState.LoseWarp;
                    // 敗北時：カメラ切替、復帰まで復活演出
                    cameraSwitcher.SwitchToDefeat();
                    yield return StartCoroutine(revivalManager.HandleRevival(
                        playerMover.gameObject, loseDefeatPos, loseReturnPos,
                        () => gaugeManager.IsFull() // 例: ゲージ満タンで復帰
                    ));
                    cameraSwitcher.SwitchToMain();
                    break;

                case BattleManager.BattleResult.Draw:
                    Debug.Log("あいこ");
                    // あいこの場合はもう一度バトル（流れに戻る）
                    break;
            }
        }
    }
}