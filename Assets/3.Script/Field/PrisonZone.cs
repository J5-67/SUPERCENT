using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Supercent.Core;

namespace Supercent.Field
{
    /// <summary>
    /// 구매자가 배달 완료 후 도착하는 최종 구역(감옥). 
    /// 진입 시 숫자를 카운트하고 UI를 업데이트합니다.
    /// </summary>
    public class PrisonZone : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int maxCapacity = 20;
        [SerializeField] private Text countText;
        [SerializeField] private Transform[] entryPath; // 감옥 입구까지의 길목 포인트들

        [Header("Grid Settings")]
        [SerializeField] private Transform gridPivot;
        [SerializeField] private float columnSpacing = 0.8f;
        [SerializeField] private float rowSpacing = 0.8f;
        [SerializeField] private int columns = 5;

        [Header("Random Dispersal")]
        [SerializeField] private bool useJitteredGrid = true;
        [SerializeField] private float jitterAmount = 0.3f;
        [SerializeField] private float randomRotationRange = 180f;

        [Header("Upgrade Integration")]
        [SerializeField] private GameObject prisonUpgradeZone;

        [Header("Waiting Settings")]
        [SerializeField] private Transform waitingPivot;
        [SerializeField] private float waitingSpacing = 1.0f;

        private int _currentCount = 0;
        private List<GameObject> _prisoners = new List<GameObject>();
        private List<Customer> _waitingCustomers = new List<Customer>();
        private HashSet<Customer> _processedCustomers = new HashSet<Customer>();

        private void Start()
        {
            UpdateUI();
            if (prisonUpgradeZone != null) prisonUpgradeZone.SetActive(false);
        }

        public Transform[] GetEntryPath() => entryPath;

        public void AddPrisoner()
        {
            _currentCount++;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (countText != null)
            {
                // '현재인원/최대인원' 형식으로 표시
                countText.text = $"{_currentCount}/{maxCapacity}";
            }

            // 인원이 다 찼을 때만 업그레이드 존 활성화
            if (prisonUpgradeZone != null)
            {
                bool isFull = _currentCount >= maxCapacity;
                
                // 새로 활성화되는 순간에만 카메라 포커스 연출
                if (isFull && !prisonUpgradeZone.activeSelf)
                {
                    if (TopDownCamera.Instance != null)
                    {
                        TopDownCamera.Instance.ShowTargetTemporarily(prisonUpgradeZone.transform, 2.0f);
                    }
                }
                
                prisonUpgradeZone.SetActive(isFull);
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            var customer = other.GetComponentInParent<Customer>();
            if (customer != null)
            {
                // 이미 처리 중이거나 감옥에 들어온 고객은 무시
                if (_processedCustomers.Contains(customer) || !customer.enabled) return;
                
                HandleCustomerArrival(customer);
            }
        }

        /// <summary>
        /// 감옥의 최대 수용 인원을 늘립니다. (업그레이드용)
        /// </summary>
        public void IncreaseCapacity(int amount)
        {
            maxCapacity += amount;
            UpdateUI();

            // 자리가 생겼으니 대기 중인 구매자 확인
            CheckWaitingCustomers();
        }

        private void CheckWaitingCustomers()
        {
            while (_waitingCustomers.Count > 0 && _currentCount < maxCapacity)
            {
                Customer customer = _waitingCustomers[0];
                _waitingCustomers.RemoveAt(0);
                ProcessEnteringCustomer(customer);
            }
            UpdateWaitingPositions();
        }

        private void UpdateWaitingPositions()
        {
            Transform pivot = waitingPivot != null ? waitingPivot : transform;
            for (int i = 0; i < _waitingCustomers.Count; i++)
            {
                // 입구 정면의 반대 방향(뒤쪽)으로 줄을 세움
                Vector3 targetPos = pivot.position + (-pivot.forward * i * waitingSpacing);
                _waitingCustomers[i].MoveTo(targetPos, 5f);
            }
        }

        /// prison 모델을 비활성화합니다.
        public void DeactivateVisual(GameObject target)
        {
            if (target != null) target.SetActive(false);
        }

        /// prison 모델을 활성화합니다.
        public void ActivateVisual(GameObject target)
        {
            if (target != null) target.SetActive(true);
        }

        private void HandleCustomerArrival(Customer customer)
        {
            if (_processedCustomers.Contains(customer)) return;
            _processedCustomers.Add(customer);

            if (_currentCount >= maxCapacity)
            {
                Debug.Log($"<color=orange>[PrisonZone]</color> Full! Adding to Waiting List: {customer.name}");
                
                customer.enabled = false;
                customer.UpdateUI();
                _waitingCustomers.Add(customer);
                UpdateWaitingPositions();
                return;
            }

            ProcessEnteringCustomer(customer);
        }

        private void ProcessEnteringCustomer(Customer customer)
        {
            Debug.Log($"<color=cyan>[PrisonZone]</color> Accepted: {customer.name} ({_currentCount + 1}/{maxCapacity})");
            
            int index = _currentCount; 
            AddPrisoner(); // 카운트 선점

            // 위치 및 회전 결정
            Transform basePivot = gridPivot != null ? gridPivot : transform;
            int col = index % columns;
            int row = index / columns;
            
            Vector3 targetPos = basePivot.position + (basePivot.right * col * columnSpacing) + (basePivot.forward * -row * rowSpacing);
            Quaternion targetRot = basePivot.rotation;

            if (useJitteredGrid)
            {
                float offsetRangeX = columnSpacing * jitterAmount;
                float offsetRangeZ = rowSpacing * jitterAmount;
                Vector3 jitter = (basePivot.right * Random.Range(-offsetRangeX, offsetRangeX)) + 
                                 (basePivot.forward * Random.Range(-offsetRangeZ, offsetRangeZ));
                targetPos += jitter;
                targetRot *= Quaternion.Euler(0, Random.Range(-randomRotationRange, randomRotationRange), 0);
            }

            customer.enabled = false;
            customer.UpdateUI();

            // 계층 구조 정리 및 이동
            customer.transform.SetParent(basePivot, true);
            customer.transform.localScale = Vector3.one; 
            
            customer.MoveTo(targetPos, 7f);
            customer.transform.rotation = targetRot;
            
            if (!_prisoners.Contains(customer.gameObject))
                _prisoners.Add(customer.gameObject);
        }
    }
}
