using UnityEngine;

namespace Game.Inputs
{
    public class PlayerInputManager : MonoBehaviour
    {
        [SerializeField] private KeyCode keyOne = KeyCode.D;
        [SerializeField] private KeyCode keyTwo = KeyCode.K;

        public delegate void KeyPressedDelegate(KeyCode key);
        public event KeyPressedDelegate OnKeyPressed;

        private KeyCode lastKeyPressed = KeyCode.None;

        void Update()
        {
            if (Input.GetKeyDown(keyOne) && lastKeyPressed != keyOne)
            {
                OnKeyPressed?.Invoke(keyOne);
                lastKeyPressed = keyOne;
            }
            else if (Input.GetKeyDown(keyTwo) && lastKeyPressed != keyTwo)
            {
                OnKeyPressed?.Invoke(keyTwo);
                lastKeyPressed = keyTwo;
            }
        }

        public void ResetKeyHistory()
        {
            lastKeyPressed = KeyCode.None;
        }
    }
}