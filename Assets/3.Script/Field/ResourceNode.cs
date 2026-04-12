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


        public void CollectBy(Player.PlayerStackHandler handler)
        {
            if (_isCollected) return; // handler가 없더라도 채굴은 가능하게 함
            StartCoroutine(CollectRoutine(handler));
        }

        private IEnumerator CollectRoutine(Player.PlayerStackHandler handler)
        {
            _isCollected = true;

            // 0.5초 동안 수집되는 연출 (스케일 축소)
            float elapsed = 0f;
            float duration = 0.5f;

            if (handler != null && handler.TryGetComponent<Player.PlayerToolManager>(out var toolManager) && toolManager.HasDrill)
            {
                duration = 0.15f; // 드릴이 있으면 수집 속도 대폭 증가
            }

            Vector3 initialScale = transform.localScale;
            Vector3 localPos = transform.localPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, elapsed / duration);
                yield return null;
            }

            // 플레이어 스택이 여유 있을 때만 추가
            if (handler != null)
            {
                if (handler.CanAdd)
                {
                    handler.AddToStack();
                }
                else
                {
                    // 스택이 가득 찼다면 MAX 표시 호출
                    handler.ShowMaxIndicator();
                }
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
