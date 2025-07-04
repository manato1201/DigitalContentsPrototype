using UnityEngine;
using System;

namespace Game.Battle
{
    public class EnemyDetector : MonoBehaviour
    {
        [SerializeField] private float checkRadius = 0.5f;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private Transform checkPoint; // プレイヤーの前方など

        // EnemyLayerに外部からアクセスできるプロパティ
        public LayerMask EnemyLayer => enemyLayer;

        // checkpoint位置で敵判定
        public bool IsEnemyAtCheckpoint()
        {
            return Physics2D.OverlapCircle(checkPoint.position, checkRadius, enemyLayer) != null;
        }

        // 任意の座標で敵判定（MainFlowから呼ぶ用）
        public bool IsEnemyAtPosition(Vector2 pos)
        {
            return Physics2D.OverlapCircle(pos, checkRadius, enemyLayer) != null;
        }
    }
}