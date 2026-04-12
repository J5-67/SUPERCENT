using UnityEngine;
using UnityEngine.UI;

namespace Supercent.UI
{
    public class SoundToggle : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;

        private bool _isMuted = false;

        private void Start()
        {
            if (iconImage == null) iconImage = GetComponent<Image>();
            
            // 저장된 설정이 있다면 불러오는 로직 (기본은 ON)
            UpdateUI();
        }

        public void ToggleSound()
        {
            _isMuted = !_isMuted;
            
            // 오디오 리스너를 일시정지하여 전체 소리 제어
            AudioListener.pause = _isMuted;
            
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (iconImage == null) return;

            iconImage.sprite = _isMuted ? soundOffSprite : soundOnSprite;
        }
    }
}
