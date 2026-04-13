using UnityEngine;
using UnityEngine.InputSystem;

namespace Supercent.UI
{
    /// <summary>
    /// 게임 시작 시 조작이 없을 때 무한대 모양으로 움직이며 가이드를 주는 UI 스크립트
    /// </summary>
    public class UIIdleInfinityGuide : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private RectTransform targetImage;
        [SerializeField] private float speed = 2.5f;   // 움직임 속도
        [SerializeField] private float width = 150f;   // 가로 너비
        [SerializeField] private float height = 75f;    // 세로 높이
        
        private Vector2 _initialPosition;
        private bool _isStopped = false;
        private CanvasGroup _canvasGroup;

        private void Start()
        {
            if (targetImage == null) targetImage = GetComponent<RectTransform>();
            _initialPosition = targetImage.anchoredPosition;
            
            // Fade 효과를 위해 CanvasGroup이 없으면 추가
            if (!TryGetComponent(out _canvasGroup))
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (_isStopped) return;

            // 1. 플레이어 조작 감지 (New Input System 방식)
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                StopAnimation();
                return;
            }

            // 2. 무한대(8자) 모양 궤적 계산
            // x = sin(t), y = sin(2t) 조합으로 '8'자 형태를 만듬
            float t = Time.time * speed;
            float x = Mathf.Sin(t) * width;
            float y = Mathf.Sin(2 * t) * height; 

            targetImage.anchoredPosition = _initialPosition + new Vector2(x, y);
        }

        private void StopAnimation()
        {
            if (_isStopped) return;
            _isStopped = true;
            
            StartCoroutine(FadeOutAndDisable());
        }

        private System.Collections.IEnumerator FadeOutAndDisable()
        {
            float elapsed = 0f;
            float duration = 0.4f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (_canvasGroup != null)
                    _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                
                yield return null;
            }
            
            gameObject.SetActive(false);
        }
    }
}
