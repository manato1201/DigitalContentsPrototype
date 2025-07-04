using UnityEngine;
using UnityEngine.UI;
using System;

namespace Game.Battle
{
    public class BattleUI : MonoBehaviour
    {
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private GameObject panel;

        // ボタン選択時にコールバック
        public void Show(Action<int> onChoice)
        {
            panel.SetActive(true);
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int idx = i; // クロージャ注意
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() =>
                {
                    panel.SetActive(false);
                    onChoice?.Invoke(idx);
                });
            }
        }
    }
}