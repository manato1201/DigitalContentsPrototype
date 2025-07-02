using UnityEngine;
using System;

namespace Game.Battle
{
    public class BattleManager : MonoBehaviour
    {
        public enum BattleResult { Win, Lose, Draw }

        // バトルの判定ロジック（外部からのみ呼ばせる）
        public void StartBattle(Action<BattleResult> onResult)
        {
            int player = UnityEngine.Random.Range(0, 3);
            int enemy = UnityEngine.Random.Range(0, 3);

            BattleResult result = (BattleResult)((player - enemy + 3) % 3);
            onResult?.Invoke(result);
        }
    }
}