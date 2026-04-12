using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Supercent.Field
{
    /// <summary>
    /// 가공기에서 판매대로 가공품을 자동으로 운반하는 AI 판매원
    /// </summary>
    public class AutoSeller : MonoBehaviour
    {
        private enum SellerState
        {
            Idle,
            MoveToSource,
            Collect,
            MoveToDestination,
            Deliver
        }

        [Header("References")]
        [SerializeField] private CollectorZone sourceCollectorZone;
        [SerializeField] private SellZone targetSellZone;
        [SerializeField] private Transform stackPivot;

        [Header("Settings")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private int maxStackCount = 3;
        [SerializeField] private float collectIntervalSeconds = 0.2f;
        [SerializeField] private float deliverIntervalSeconds = 0.2f;
        [SerializeField] private float stopDistance = 1.8f; // 기계 안으로 들어가지 않도록 기본값 상향
        [SerializeField] private float stackVerticalSpacing = 0.4f;
        [SerializeField] private float rotationSpeed = 12f;

        private List<Transform> _carriedItems = new List<Transform>();
        private SellerState _currentState = SellerState.MoveToSource;
        private float _stopDistanceSqr;

        private void Awake()
        {
            _stopDistanceSqr = stopDistance * stopDistance;
        }

        private void Update()
        {
            HandleMovement();
            UpdateStackPositions();
        }

        private void HandleMovement()
        {
            if (_currentState == SellerState.Idle || _currentState == SellerState.Collect || _currentState == SellerState.Deliver)
                return;

            Vector3 targetPosition = GetTargetPosition();
            Vector3 diff = targetPosition - transform.position;
            diff.y = 0;

            if (diff.sqrMagnitude <= _stopDistanceSqr)
            {
                TransitionToTaskState();
                return;
            }

            MoveTowards(diff.normalized);
        }

        private Vector3 GetTargetPosition()
        {
            if (_currentState == SellerState.MoveToSource)
                return sourceCollectorZone.transform.position;
            
            if (_currentState == SellerState.MoveToDestination)
                return targetSellZone.transform.position;

            return transform.position;
        }

        private void MoveTowards(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return;

            // 이동
            transform.position += direction * (moveSpeed * Time.deltaTime);
            
            // 회전: 이동 방향으로 즉시/부드럽게 회전하여 문워크 방지
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        private void TransitionToTaskState()
        {
            if (_currentState == SellerState.MoveToSource)
            {
                _currentState = SellerState.Collect;
                StartCoroutine(CollectRoutine());
                return;
            }

            if (_currentState == SellerState.MoveToDestination)
            {
                _currentState = SellerState.Deliver;
                StartCoroutine(DeliverRoutine());
            }
        }

        private IEnumerator CollectRoutine()
        {
            while (_carriedItems.Count < maxStackCount)
            {
                if (sourceCollectorZone == null || sourceCollectorZone.TargetProcessor == null) break;

                GameObject item = sourceCollectorZone.TargetProcessor.PopProcessedItem();
                if (item == null)
                {
                    // 가져갈 아이템이 없는데 이미 들고 있는 게 있다면 배달하러 이동
                    if (_carriedItems.Count > 0) break;
                    
                    // 아예 없다면 잠시 대기
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                Transform itemTrm = item.transform;
                itemTrm.SetParent(null);
                _carriedItems.Add(itemTrm);
                
                yield return new WaitForSeconds(collectIntervalSeconds);
            }

            _currentState = SellerState.MoveToDestination;
        }

        private IEnumerator DeliverRoutine()
        {
            while (_carriedItems.Count > 0)
            {
                int lastIndex = _carriedItems.Count - 1;
                Transform item = _carriedItems[lastIndex];
                _carriedItems.RemoveAt(lastIndex);

                targetSellZone.DepositItem(item.gameObject);
                
                yield return new WaitForSeconds(deliverIntervalSeconds);
            }

            _currentState = SellerState.MoveToSource;
        }

        private void UpdateStackPositions()
        {
            if (_carriedItems.Count == 0) return;

            // stackPivot이 할당되지 않았을 경우 자신의 transform을 기준으로 사용
            Transform basePivot = stackPivot != null ? stackPivot : transform;

            for (int i = 0; i < _carriedItems.Count; i++)
            {
                Vector3 targetLocalPos = Vector3.up * (i * stackVerticalSpacing);
                Vector3 targetWorldPos = basePivot.TransformPoint(targetLocalPos);
                
                _carriedItems[i].position = Vector3.Lerp(_carriedItems[i].position, targetWorldPos, Time.deltaTime * 15f);
                _carriedItems[i].rotation = Quaternion.Slerp(_carriedItems[i].rotation, transform.rotation, Time.deltaTime * 15f);
            }
        }

        public void Initialize(CollectorZone collectorZone, SellZone sellZone)
        {
            sourceCollectorZone = collectorZone;
            targetSellZone = sellZone;
            _currentState = SellerState.MoveToSource;
        }
    }
}
