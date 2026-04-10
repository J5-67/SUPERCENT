using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Supercent.Player
{
    public class PlayerStackHandler : MonoBehaviour
    {
        [Header("Stack Settings")]
        [SerializeField] private GameObject stackedItemPrefab;
        [SerializeField] private Transform stackPivot;
        [SerializeField] private float followSpeed = 15f;
        [SerializeField] private float itemSpacing = 0.5f;
        [SerializeField] private int maxStackCount = 20;

        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 20;

        public bool CanAdd => _stackedItems.Count < maxStackCount;

        private List<Transform> _stackedItems = new List<Transform>();
        private IObjectPool<GameObject> _stackPool;

        private void Awake()
        {
            InitializePool();
        }

        private void InitializePool()
        {
            _stackPool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(stackedItemPrefab),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                collectionCheck: false,
                defaultCapacity: initialPoolSize
            );
        }

        public void AddToStack()
        {
            GameObject newItem = _stackPool.Get();
            AddExistingToStack(newItem.transform);
        }

        public void AddExistingToStack(Transform item)
        {
            if (_stackedItems.Count >= maxStackCount) return;

            // 첫 아이템은 피벗 위치, 그 외에는 마지막 아이템 뒤에 배치
            Vector3 spawnPos = _stackedItems.Count == 0 
                ? stackPivot.position 
                : _stackedItems[_stackedItems.Count - 1].position;

            item.position = spawnPos;
            item.SetParent(null); // 부모 관계 해제
            _stackedItems.Add(item);
        }

        public GameObject PopFromStack()
        {
            if (_stackedItems.Count == 0) return null;

            int lastIndex = _stackedItems.Count - 1;
            GameObject lastItem = _stackedItems[lastIndex].gameObject;
            _stackedItems.RemoveAt(lastIndex);
            
            return lastItem;
        }

        public void ReleaseToPool(GameObject obj)
        {
            _stackPool.Release(obj);
        }

        private void FixedUpdate()
        {
            UpdateStackPosition();
        }

        private void UpdateStackPosition()
        {
            if (_stackedItems.Count == 0) return;

            for (int i = 0; i < _stackedItems.Count; i++)
            {
                Transform current = _stackedItems[i];
                
                // 플레이어(피벗) 위치를 기본으로 하되, 인덱스만큼 위로 쌓음
                Vector3 targetPos = stackPivot.position + (Vector3.up * (i * itemSpacing));
                
                // 플레이어의 회전에 맞춰 위치 오프셋을 조정 (플레이어 뒤쪽에 고정되게 하려면 추가 연산 가능)
                // 현재는 피벗 위치에서 그대로 수직으로 쌓임
                
                // 부드러운 추적 (Lerp)
                current.position = Vector3.Lerp(current.position, targetPos, Time.fixedDeltaTime * followSpeed);
                current.rotation = Quaternion.Slerp(current.rotation, stackPivot.rotation, Time.fixedDeltaTime * followSpeed);
            }
        }
    }
}
