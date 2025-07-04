using UnityEngine;
using System;

namespace Game.Battle
{
    public class BattleManager : MonoBehaviour
    {
        public enum BattleResult { Win, Lose, Draw }

        // プレイヤーの選択を引数で受け取る
        public void StartBattle(int playerChoice, Action<BattleResult> onResult)
        {
            int enemyChoice = UnityEngine.Random.Range(0, 3);
            BattleResult result = (BattleResult)((playerChoice - enemyChoice + 3) % 3);
            onResult?.Invoke(result);
        }
    }
}