using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Supercent.Field
{
    public class Customer : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private int requiredAmount = 4;
        [SerializeField] private int currentAmount = 0;
        
        [Header("UI References")]
        [SerializeField] private GameObject uiRoot;
        [SerializeField] private Text countText;
        [SerializeField] private Slider progressSlider;
        
        public bool IsSatisfied => currentAmount >= requiredAmount;
        public int RemainingAmount => Mathf.Max(0, requiredAmount - currentAmount);

        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void Initialize(int demand)
        {
            // 요구 수량이 0이면 최소 1로 설정하여 무한대기 방지
            requiredAmount = Mathf.Max(1, demand);
            currentAmount = 0;
            
            if (progressSlider != null)
            {
                progressSlider.minValue = 0;
                progressSlider.maxValue = requiredAmount;
                progressSlider.value = 0;
            }

            // 초기 수치를 즉시 반영
            UpdateUI();
        }

        public void ReceiveItem()
        {
            currentAmount++;
            UpdateUI();
            
            StopCoroutine(nameof(BopEffect));
            StartCoroutine(nameof(BopEffect));
        }

        public void UpdateUI()
        {
            if (countText != null)
            {
                countText.text = RemainingAmount.ToString();
            }

            if (progressSlider != null)
            {
                progressSlider.value = currentAmount;
            }
            
            if (uiRoot != null)
            {
                // 만족 시 UI 부모를 꺼서 숫자와 게이지를 숨김
                uiRoot.SetActive(!IsSatisfied);
            }
        }

        private IEnumerator BopEffect()
        {
            float elapsed = 0f;
            float duration = 0.2f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
                transform.localScale = _baseScale * scaleMultiplier;
                yield return null;
            }
            transform.localScale = _baseScale;
        }

        private Coroutine _moveCoroutine;

        public void MoveTo(Vector3 targetPos, float speed = 5f)
        {
            SafeStopMove();
            _moveCoroutine = StartCoroutine(MoveRoutine(targetPos, speed));
        }

        public void FollowPath(Transform[] waypoints, float speed = 5f)
        {
            SafeStopMove();
            _moveCoroutine = StartCoroutine(FollowPathRoutine(waypoints, speed));
        }

        private void SafeStopMove()
        {
            // 모든 코루틴을 중지하여 yield StartCoroutine으로 대기 중인 하위 이동까지 모두 제거
            StopAllCoroutines();
            _moveCoroutine = null;
            
            // 물리적인 속도가 남아있을 경우 초기화 (Kinematic이 아닐 때만)
            if (TryGetComponent<Rigidbody>(out var rb) && !rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private IEnumerator FollowPathRoutine(Transform[] waypoints, float speed)
        {
            if (waypoints == null) yield break;

            foreach (var wp in waypoints)
            {
                if (wp == null) continue;
                yield return StartCoroutine(MoveRoutine(wp.position, speed));
            }
        }

        private IEnumerator MoveRoutine(Vector3 targetPos, float speed)
        {
            while (Vector3.SqrMagnitude(transform.position - targetPos) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                
                Vector3 dir = (targetPos - transform.position).normalized;
                if (dir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
                }
                
                yield return null;
            }
            transform.position = targetPos;
        }
    }
}
