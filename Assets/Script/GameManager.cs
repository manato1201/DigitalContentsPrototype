using UnityEngine;
using Game.Battle;
using Game.Movement;
using Game.Warp;

namespace Game.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private Mover playerMover;
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private WarpManager warpManager;
        [SerializeField] private RevivalManager revivalManager;

        [Header("位置設定")]
        [SerializeField] private Vector2 winWarpPos;
        [SerializeField] private Vector2 winReturnPos;
        [SerializeField] private Vector2 loseDefeatPos;
        [SerializeField] private Vector2 loseReturnPos;

        // バトルフロー例
        public void StartBattleFlow()
        {
            battleManager.StartBattle(result =>
            {
                if (result == BattleManager.BattleResult.Win)
                {
                    StartCoroutine(warpManager.WarpAndReturn(
                        playerMover.gameObject,
                        winWarpPos,
                        winReturnPos,
                        2f
                    ));
                }
                else if (result == BattleManager.BattleResult.Lose)
                {
                    StartCoroutine(revivalManager.HandleRevival(
                        playerMover.gameObject,
                        loseDefeatPos,
                        loseReturnPos,
                        () => true // 仮：すぐ復帰。ゲージMAX等に置換可
                    ));
                }
            });
        }
    }
}