using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Supercent.Player;

namespace Supercent.Field
{
    /// <summary>
    /// 플레이어가 가공품을 판매대에 쌓으면 구매자가 뒤로 줄을 서서 가져가는 지능형 판매 구역 스크립트
    /// </summary>
    public class SellZone : MonoBehaviour
    {
        [Header("Customer Logic")]
        [SerializeField] private GameObject customerPrefab;
        [SerializeField] private int minDemand = 1;
        [SerializeField] private int maxDemand = 5;

        [Header("Auto Queue Settings")]
        [SerializeField] private Transform firstQueuePivot; 
        [SerializeField] private float queueSpacing = 1.8f;
        [SerializeField] private int maxQueueCount = 2;

        [Header("Counter Settings")]
        [SerializeField] private Transform counterPivot;
        [SerializeField] private float verticalSpacing = 0.2f;
        [SerializeField] private float columnSpacing = 0.6f;
        [SerializeField] private int columns = 2;
        [SerializeField] private float serveInterval = 0.5f;

        [Header("Money Integration")]
        [SerializeField] private MoneyZone moneyZone;

        [Header("Transaction Settings")]
        [SerializeField] private float depositInterval = 0.15f;
        [SerializeField] private float moveDuration = 0.3f;

        [Header("Prison Integration")]
        [SerializeField] private PrisonZone targetPrison;

        private List<Customer> _customers = new List<Customer>();
        private List<Transform> _counterItems = new List<Transform>();
        private Coroutine _depositCoroutine;
        private PlayerStackHandler _currentPlayer;
        private UnityEngine.Pool.IObjectPool<Customer> _customerPool;

        private void Awake()
        {
            InitializeCustomerPool();
        }

        private void InitializeCustomerPool()
        {
            _customerPool = new UnityEngine.Pool.ObjectPool<Customer>(
                createFunc: () => Instantiate(customerPrefab).GetComponent<Customer>(),
                actionOnGet: (c) => c.gameObject.SetActive(true),
                actionOnRelease: (c) => c.gameObject.SetActive(false),
                defaultCapacity: maxQueueCount + 2
            );
        }

        private void Start()
        {
            StartCoroutine(InitialSpawnRoutine());
            StartCoroutine(CustomerServeRoutine());
        }

        private IEnumerator InitialSpawnRoutine()
        {
            while (_customers.Count < maxQueueCount)
            {
                SpawnCustomer();
                yield return new WaitForSeconds(0.3f);
            }
        }

        private void SpawnCustomer()
        {
            if (firstQueuePivot == null) return;
            if (_customers.Count >= maxQueueCount) return;

            Vector3 spawnOffset = -firstQueuePivot.forward * (_customers.Count + 1) * queueSpacing;
            Vector3 spawnPos = firstQueuePivot.position + spawnOffset + (-firstQueuePivot.forward * 3f);
            
            Customer customer = _customerPool.Get();
            customer.transform.position = spawnPos;
            customer.transform.rotation = Quaternion.identity;
            
            customer.Initialize(Random.Range(minDemand, maxDemand + 1));
            _customers.Add(customer);
            UpdateQueuePositions();
        }

        private void UpdateQueuePositions()
        {
            for (int i = 0; i < _customers.Count; i++)
            {
                Vector3 targetPos = firstQueuePivot.position + (-firstQueuePivot.forward * i * queueSpacing);
                _customers[i].MoveTo(targetPos);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var handler = other.GetComponentInParent<PlayerStackHandler>();
            if (handler != null)
            {
                _currentPlayer = handler;
                if (_depositCoroutine == null) _depositCoroutine = StartCoroutine(DepositRoutine());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var handler = other.GetComponentInParent<PlayerStackHandler>();
            if (handler != null && _currentPlayer == handler)
            {
                _currentPlayer = null;
                if (_depositCoroutine != null)
                {
                    StopCoroutine(_depositCoroutine);
                    _depositCoroutine = null;
                }
            }
        }

        private IEnumerator DepositRoutine()
        {
            while (_currentPlayer != null)
            {
                GameObject item = _currentPlayer.PopFromFrontStack();
                if (item != null)
                {
                    DepositItem(item);
                    yield return new WaitForSeconds(depositInterval);
                }
                else
                {
                    yield return new WaitForSeconds(0.2f);
                }
            }
        }

        public void DepositItem(GameObject item)
        {
            if (item == null) return;

            int index = _counterItems.Count;
            int col = index % columns;
            int row = index / columns;

            Vector3 offset = new Vector3(col * columnSpacing, row * verticalSpacing, 0);
            Vector3 targetPos = counterPivot.position + offset;

            Transform itemTrm = item.transform;
            _counterItems.Add(itemTrm);
            
            StartCoroutine(MoveToCounter(itemTrm, targetPos));
        }

        private IEnumerator CustomerServeRoutine()
        {
            while (true)
            {
                if (_customers.Count > 0)
                {
                    Customer target = _customers[0];
                    
                    if (target.IsSatisfied)
                    {
                        HandleSatisfiedCustomer(target);
                        yield return new WaitForSeconds(0.8f);
                        continue;
                    }

                    if (_counterItems.Count > 0)
                    {
                        int lastIdx = _counterItems.Count - 1;
                        Transform item = _counterItems[lastIdx];
                        _counterItems.RemoveAt(lastIdx);

                        target.ReceiveItem();
                        StartCoroutine(MoveToCustomer(item, target.transform));

                        if (target.IsSatisfied)
                        {
                            yield return new WaitForSeconds(0.6f);
                            HandleSatisfiedCustomer(target);
                        }
                    }
                }
                yield return new WaitForSeconds(serveInterval);
            }
        }

        private void HandleSatisfiedCustomer(Customer customer)
        {
            if (!_customers.Contains(customer)) return;
            
            _customers.Remove(customer);
            
            // 앞으로 퇴장 (감옥이 설정되어 있다면 감옥 경로를 따라감)
            if (targetPrison != null)
            {
                customer.FollowPath(targetPrison.GetEntryPath(), 5f, () => _customerPool.Release(customer));
            }
            else
            {
                // 감옥이 없을 경우 방황하다가 풀로 반환
                customer.MoveTo(customer.transform.position + firstQueuePivot.forward * 10f, 6f);
                StartCoroutine(ReleaseCustomerWithDelay(customer, 3f));
            }

            // 돈 생성
            if (moneyZone != null)
            {
                moneyZone.SpawnMoney();
            }

            UpdateQueuePositions();
            Invoke(nameof(SpawnCustomer), 1.5f);
        }

        private IEnumerator ReleaseCustomerWithDelay(Customer customer, float delay)
        {
            yield return new WaitForSeconds(delay);
            _customerPool.Release(customer);
        }

        private IEnumerator MoveToCounter(Transform item, Vector3 targetPos)
        {
            Vector3 startPos = item.position;
            float elapsed = 0f;
            item.SetParent(transform);

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / moveDuration;
                item.position = Vector3.Lerp(startPos, targetPos, t) + (Vector3.up * Mathf.Sin(t * Mathf.PI) * 1f);
                yield return null;
            }
            item.position = targetPos;
        }

        private IEnumerator MoveToCustomer(Transform item, Transform target)
        {
            Vector3 startPos = item.position;
            float elapsed = 0f;
            item.SetParent(null);

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / moveDuration;
                item.position = Vector3.Lerp(startPos, target.position + Vector3.up * 1f, t);
                item.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                yield return null;
            }
            
            Supercent.Systems.ItemPoolManager.Instance.ReleaseProcessedItem(item.gameObject);
        }
    }
}
