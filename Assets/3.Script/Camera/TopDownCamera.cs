using UnityEngine;

namespace Supercent.Core
{
    public class TopDownCamera : MonoBehaviour
    {
        public static TopDownCamera Instance { get; private set; }

        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 10, -10);
        [SerializeField] private float smoothSpeed = 5f;

        [Header("Effect Defaults")]
        [SerializeField] private float defaultZoomMultiplier = 1.5f;
        [SerializeField] private float defaultEffectDuration = 2.0f;

        private Vector3 _originalOffset;
        private Vector3 _currentOffset;
        private Transform _originalTarget;
        private Coroutine _focusCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            _originalTarget = target;
            _originalOffset = offset;
            _currentOffset = offset;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            FollowTarget();
        }

        private void FollowTarget()
        {
            Vector3 desiredPosition = target.position + _currentOffset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.unscaledDeltaTime);
            transform.position = smoothedPosition;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            _originalTarget = newTarget;
        }

        public void ShowTargetTemporarily(Transform tempTarget, float duration, System.Action onComplete = null)
        {
            if (_focusCoroutine != null) StopCoroutine(_focusCoroutine);
            // 줌 배율을 1.0으로 설정하여 확대/축소 없이 바로 비춰주도록 수정
            _focusCoroutine = StartCoroutine(UpgradeFocusRoutine(tempTarget, 1.0f, duration, onComplete));
        }

        public void ShowUpgradeEffectSimple(Transform upgradeTarget, System.Action onComplete = null)
        {
            ShowUpgradeEffect(upgradeTarget, defaultZoomMultiplier, defaultEffectDuration, onComplete);
        }

        public void ShowUpgradeEffect(Transform upgradeTarget, float zoomMultiplier, float duration, System.Action onComplete = null)
        {
            if (_focusCoroutine != null) StopCoroutine(_focusCoroutine);
            _focusCoroutine = StartCoroutine(UpgradeFocusRoutine(upgradeTarget, zoomMultiplier, duration, onComplete));
        }

        private System.Collections.IEnumerator UpgradeFocusRoutine(Transform tempTarget, float zoomMultiplier, float duration, System.Action onComplete)
        {
            target = tempTarget;
            Vector3 targetOffset = _originalOffset * zoomMultiplier;

            // 줌 아웃 (unscaledDeltaTime 사용으로 일시정지 중에도 작동 가능)
            float elapsed = 0f;
            float transition = 0.5f;
            while (elapsed < transition)
            {
                elapsed += Time.unscaledDeltaTime;
                _currentOffset = Vector3.Lerp(_originalOffset, targetOffset, elapsed / transition);
                yield return null;
            }
            _currentOffset = targetOffset;

            yield return new WaitForSecondsRealtime(duration);

            // 줌 인 및 복귀
            elapsed = 0f;
            while (elapsed < transition)
            {
                elapsed += Time.unscaledDeltaTime;
                _currentOffset = Vector3.Lerp(targetOffset, _originalOffset, elapsed / transition);
                yield return null;
            }
            _currentOffset = _originalOffset;
            target = _originalTarget;
            _focusCoroutine = null;

            // 카메라가 완전히 안착하고 유저가 상황을 인지할 수 있도록 충분한 대기 시간 추가
            yield return new WaitForSecondsRealtime(0.5f);

            // 모든 연출 종료 후 콜백 실행
            onComplete?.Invoke();
        }
    }
}
