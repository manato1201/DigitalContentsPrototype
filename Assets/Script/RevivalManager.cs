using UnityEngine;
using System;
using System.Collections;

namespace Game.Warp
{
    public class RevivalManager : MonoBehaviour
    {
        // 復帰条件はコールバック式（安全性UP、外部から明確に受け取る）
        public IEnumerator HandleRevival(GameObject obj, Vector2 defeatPos, Vector2 revivalPos, Func<bool> revivalCondition)
        {
            if (obj == null) yield break;
            obj.transform.position = defeatPos;

            yield return new WaitUntil(revivalCondition);

            obj.transform.position = revivalPos;
        }
    }
}