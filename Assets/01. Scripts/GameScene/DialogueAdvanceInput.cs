using UnityEngine.InputSystem;

namespace DiaBlackJack.GameScene
{
    internal static class DialogueAdvanceInput
    {
        public static bool WasPressedThisFrame()
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            return IsRequested(
                mouse != null && mouse.leftButton.wasPressedThisFrame,
                keyboard != null && keyboard.spaceKey.wasPressedThisFrame,
                keyboard != null && keyboard.enterKey.wasPressedThisFrame,
                keyboard != null && keyboard.numpadEnterKey.wasPressedThisFrame);
        }

        internal static bool IsRequested(
            bool leftMousePressed,
            bool spacePressed,
            bool enterPressed,
            bool numpadEnterPressed)
        {
            return leftMousePressed ||
                spacePressed ||
                enterPressed ||
                numpadEnterPressed;
        }
    }
}
