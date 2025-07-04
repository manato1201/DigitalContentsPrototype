using UnityEngine;
using System.Collections;

namespace Game.Warp
{
    public class WarpManager : MonoBehaviour
    {
        // ワープして一定時間後に戻す（他のネームスペースのクラスとも安全にやりとりできる設計）
        public IEnumerator WarpAndReturn(GameObject obj, Vector2 warpPos, Vector2 returnPos, float waitSeconds)
        {
            if (obj == null) yield break;
            obj.transform.position = warpPos;
            yield return new WaitForSeconds(waitSeconds);
            obj.transform.position = returnPos;
        }

        public void Warp(GameObject obj, Vector2 warpPos)
        {
            if (obj == null) return;
            obj.transform.position = warpPos;
        }
    }
}