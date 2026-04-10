using System.Collections;
using UnityEngine;

namespace Supercent.Field
{
    public class ResourceNode : MonoBehaviour
    {
        private bool _isCollected = false;
        private ResourceField _field;

        public void Initialize(ResourceField field)
        {
            _field = field;
            _isCollected = false;
            transform.localScale = Vector3.one;
        }

        private void OnTriggerEnter(Collider foreign)
        {
            if (_isCollected) return;

            // 1. 컴포넌트 직접 시도
            if (foreign.TryGetComponent<Player.PlayerStackHandler>(out var handler))
            {
                StartCoroutine(CollectRoutine(handler));
                return;
            }

            // 2. 태그로 확인 시 부모에서 다시 시도
            if (foreign.CompareTag("Player"))
            {
                handler = foreign.GetComponentInParent<Player.PlayerStackHandler>();
                if (handler != null)
                {
                    StartCoroutine(CollectRoutine(handler));
                }
            }
        }

        private IEnumerator CollectRoutine(Player.PlayerStackHandler handler)
        {
            _isCollected = true;

            // 0.5초 동안 수집되는 연출 (스케일 축소)
            float elapsed = 0f;
            float duration = 0.5f;
            Vector3 initialScale = transform.localScale;
            Vector3 localPos = transform.localPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, elapsed / duration);
                yield return null;
            }

            // 플레이어 스택이 여유 있을 때만 추가
            if (handler != null && handler.CanAdd)
            {
                handler.AddToStack();
            }

            // 필드 매니저를 통해 풀로 반환하며 위치 값 전달 (재생성용)
            if (_field != null)
            {
                _field.ReleaseNode(this.gameObject, localPos);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
