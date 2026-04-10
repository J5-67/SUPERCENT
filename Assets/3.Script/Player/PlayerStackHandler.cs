using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Supercent.Player
{
    public class PlayerStackHandler : MonoBehaviour
    {
        [Header("Front Stack (Processed)")]
        [SerializeField] private GameObject processedItemPrefab;
        [SerializeField] private Transform frontStackPivot;
        [SerializeField] private Vector3 frontOffSet = new Vector3(0, 0, 0.8f);
        [SerializeField] private float frontSpacing = 0.5f;

        [Header("Back Stack (Raw Materials)")]
        [SerializeField] private GameObject rawItemPrefab;
        [SerializeField] private Transform backStackPivot;
        [SerializeField] private Vector3 backOffset = new Vector3(0, 0, -0.6f);
        [SerializeField] private float backSpacing = 0.35f;

        [Header("Money Stack")]
        [SerializeField] private GameObject moneyPrefab;
        [SerializeField] private Vector3 moneyOffset = new Vector3(0f, 0, -1.0f); // 리소스(-0.5)보다 더 뒤(-1.0)로 배치
        [SerializeField] private float moneySpacing = 0.1f;
        [SerializeField] private float moneyInitialHeight = 0f; // 옆에 쌓이므로 0부터 시작

        [Header("Movement Settings")]
        [SerializeField] private float followSpeed = 15f;
        [SerializeField] private float swayIntensity = 5f;
        [SerializeField] private float maxTiltAngle = 10f;
        [SerializeField] private int maxStackLimit = 20;

        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 20;

        public bool CanAdd => _backStackedItems.Count + _frontStackedItems.Count + _moneyStackedItems.Count < maxStackLimit * 2;

        private List<Transform> _frontStackedItems = new List<Transform>();
        private List<Transform> _backStackedItems = new List<Transform>();
        private List<Transform> _moneyStackedItems = new List<Transform>();
        
        private IObjectPool<GameObject> _frontPool;
        private IObjectPool<GameObject> _backPool;
        
        private Vector3 _lastParentPosition;
        private Vector3 _parentVelocity;

        private void Awake()
        {
            InitializePools();
            _lastParentPosition = transform.position;
        }

        private void InitializePools()
        {
            _frontPool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(processedItemPrefab),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                defaultCapacity: initialPoolSize
            );

            _backPool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(rawItemPrefab),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                defaultCapacity: initialPoolSize
            );
        }

        // 원자재 추가 (등 뒤)
        public void AddToStack()
        {
            if (_backStackedItems.Count >= maxStackLimit) return;
            GameObject newItem = _backPool.Get();
            newItem.transform.position = backStackPivot != null ? backStackPivot.position : transform.position;
            newItem.transform.SetParent(null);
            _backStackedItems.Add(newItem.transform);
        }

        // 돈 추가 (원자재 위 또는 뒤)
        public void AddMoneyToStack(Transform moneyTrm)
        {
            moneyTrm.SetParent(null);
            _moneyStackedItems.Add(moneyTrm);
        }

        // 가공품 추가 (전면)
        public void AddExistingToStack(Transform item)
        {
            if (_frontStackedItems.Count >= maxStackLimit) return;
            item.position = frontStackPivot != null ? frontStackPivot.position : transform.position;
            item.SetParent(null);
            _frontStackedItems.Add(item);
        }

        // 스택에서 아이템 추출 (가공기에 넣을 때 등 사용)
        public GameObject PopFromStack()
        {
            if (_backStackedItems.Count == 0) return null;
            int lastIdx = _backStackedItems.Count - 1;
            GameObject item = _backStackedItems[lastIdx].gameObject;
            _backStackedItems.RemoveAt(lastIdx);
            return item;
        }

        public GameObject PopFromFrontStack()
        {
            if (_frontStackedItems.Count == 0) return null;
            int lastIdx = _frontStackedItems.Count - 1;
            GameObject item = _frontStackedItems[lastIdx].gameObject;
            _frontStackedItems.RemoveAt(lastIdx);
            return item;
        }

        public void ReleaseToBackPool(GameObject obj) => _backPool.Release(obj);
        public void ReleaseToFrontPool(GameObject obj) => _frontPool.Release(obj);

        private void Update()
        {
            UpdateVelocity();
            UpdateStacks();
        }

        private void UpdateVelocity()
        {
            _parentVelocity = (transform.position - _lastParentPosition) / Time.deltaTime;
            _lastParentPosition = transform.position;
        }

        private void UpdateStacks()
        {
            // 전면 스택 업데이트
            UpdateSingleStack(_frontStackedItems, frontOffSet, frontSpacing, 0);
            // 후면 스택 업데이트
            UpdateSingleStack(_backStackedItems, backOffset, backSpacing, 0);
            
            // 돈 스택 업데이트 (리소스 유무에 따라 동적으로 Z축 위치 조정)
            Vector3 dynamicMoneyOffset = moneyOffset;
            if (_backStackedItems.Count > 0)
            {
                // 리소스가 있다면 리소스 위치보다 뒤로 확실히 밀어줌
                dynamicMoneyOffset.z = backOffset.z - 0.7f;
            }
            
            UpdateSingleStack(_moneyStackedItems, dynamicMoneyOffset, moneySpacing, moneyInitialHeight);
        }

        private void UpdateSingleStack(List<Transform> stack, Vector3 offset, float spacing, float initialHeight)
        {
            if (stack.Count == 0) return;

            Vector3 basePoint = transform.TransformPoint(offset);
            
            float swayAmount = Mathf.Clamp(_parentVelocity.magnitude * swayIntensity, 0, maxTiltAngle);
            Vector3 swayDir = -_parentVelocity.normalized;
            
            Vector3 cross = Vector3.Cross(Vector3.up, swayDir);
            Quaternion tiltRotation = cross.sqrMagnitude > 0.001f 
                ? Quaternion.AngleAxis(swayAmount, cross.normalized) 
                : Quaternion.identity;

            for (int i = 0; i < stack.Count; i++)
            {
                Transform current = stack[i];
                Vector3 verticalOffset = Vector3.up * (initialHeight + i * spacing);
                Vector3 targetPos = basePoint + (tiltRotation * verticalOffset);

                current.position = Vector3.Lerp(current.position, targetPos, Time.deltaTime * followSpeed);
                current.rotation = Quaternion.Slerp(current.rotation, transform.rotation, Time.deltaTime * followSpeed);
            }
        }
    }
}
