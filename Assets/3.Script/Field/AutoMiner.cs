using System.Collections;
using UnityEngine;

namespace Supercent.Field
{
    public class AutoMiner : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float miningInterval = 0.5f;
        [SerializeField] private float scannerRadius = 1.0f;
        [SerializeField] private LayerMask resourceLayer;

        [Header("Patrol Settings")]
        [SerializeField] private float maxDistance = 20f; // 앞으로 가는 최대 거리
        
        [Header("Resources")]
        [SerializeField] private GameObject rawMaterialPrefab;
        private ResourceZone _targetZone;

        private bool _isMining = false;
        private bool _isReturning = false;
        private float _lastMiningTime;
        private Vector3 _startPosition;
        private Vector3 _forwardDir;
        private float _currentDist = 0f;

        private void Start()
        {
            _startPosition = transform.position;
            
            // Y축 회전이 포함된 필드 방향을 정규화하여 저장
            _forwardDir = transform.forward;
            _forwardDir.y = 0;
            _forwardDir.Normalize();
        }

        public void Initialize(ResourceZone zone, GameObject rawPrefab, float dist)
        {
            _targetZone = zone;
            rawMaterialPrefab = rawPrefab;
            maxDistance = dist;
        }

        private void Update()
        {
            if (_isMining) return;

            ScanForResources();

            // 거리 업데이트
            if (_isReturning)
            {
                _currentDist -= moveSpeed * Time.deltaTime;
                if (_currentDist <= 0)
                {
                    _currentDist = 0;
                    _isReturning = false;
                }
            }
            else
            {
                _currentDist += moveSpeed * Time.deltaTime;
                if (_currentDist >= maxDistance)
                {
                    _isReturning = true;
                }
            }

            // 고정된 라인 위에서의 위치 계산 (이동 공식: 시작점 + (방향 * 거리))
            // 이 방식은 필드가 어떤 각도로 회전해 있든 오차 없이 직선 이동을 보장합니다.
            transform.position = _startPosition + (_forwardDir * _currentDist);

            // 회전 처리: 진행 방향에 따라 부드럽게 회전
            Vector3 targetLookDir = _isReturning ? -_forwardDir : _forwardDir;
            if (targetLookDir != Vector3.zero)
            {
                transform.forward = Vector3.Lerp(transform.forward, targetLookDir, Time.deltaTime * 10f);
            }
        }

        private void ScanForResources()
        {
            if (Time.time < _lastMiningTime + miningInterval) return;

            Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 0.5f, scannerRadius, resourceLayer);
            
            if (hits.Length > 0)
            {
                StartCoroutine(MiningRoutine(hits));
            }
        }

        private IEnumerator MiningRoutine(Collider[] hits)
        {
            _isMining = true;
            
            yield return new WaitForSeconds(miningInterval);

            bool minedAny = false;
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<ResourceNode>(out var node))
                {
                    node.CollectBy(null); 
                    minedAny = true;
                }
                else
                {
                    var parentNode = hit.GetComponentInParent<ResourceNode>();
                    if (parentNode != null)
                    {
                        parentNode.CollectBy(null);
                        minedAny = true;
                    }
                }
            }

            // 채굴 성공 시 결과물을 구역에 전달
            if (minedAny && _targetZone != null && rawMaterialPrefab != null)
            {
                GameObject item = Instantiate(rawMaterialPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
                _targetZone.AddItem(item);
            }

            _lastMiningTime = Time.time;
            _isMining = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + transform.forward * 0.5f, scannerRadius);
        }
    }
}
