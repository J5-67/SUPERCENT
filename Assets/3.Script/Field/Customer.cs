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

        public void MoveTo(Vector3 targetPos, float speed = 5f)
        {
            StopCoroutine(nameof(MoveRoutine));
            StartCoroutine(MoveRoutine(targetPos, speed));
        }

        private IEnumerator MoveRoutine(Vector3 targetPos, float speed)
        {
            while (Vector3.SqrMagnitude(transform.position - targetPos) > 0.01f)
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
