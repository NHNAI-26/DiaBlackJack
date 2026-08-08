using Border.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Border.UI
{
    /// <summary>Plays the shared UI press sound for successful button clicks.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UISelectableSoundHook : MonoBehaviour
    {
        private const string ButtonPressSfxId = "buttonPress";

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button ??= GetComponent<Button>();
            _button?.onClick.AddListener(PlayClickSound);
        }

        private void OnDisable()
        {
            _button?.onClick.RemoveListener(PlayClickSound);
        }

        private void PlayClickSound()
        {
            if (_button == null || !_button.IsInteractable())
            {
                return;
            }

            SoundManager.Current?.PlaySfx(ButtonPressSfxId);
        }
    }
}
