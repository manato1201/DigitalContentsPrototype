using UnityEngine;

namespace Game.Core
{
    public class CameraSwitcher : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera defeatCamera;

        public void SwitchToMain()
        {
            if (mainCamera != null) mainCamera.enabled = true;
            if (defeatCamera != null) defeatCamera.enabled = false;
        }

        public void SwitchToDefeat()
        {
            if (mainCamera != null) mainCamera.enabled = false;
            if (defeatCamera != null) defeatCamera.enabled = true;
        }
    }
}