using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Supercent.Field
{
    public class ResourceProcessor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ResourceZone inputZone;
        [SerializeField] private GameObject processedPrefab;
        [SerializeField] private Transform outputPivot;

        [Header("Settings")]
        [SerializeField] private float processingTime = 1.0f;
        [SerializeField] private float outputVerticalSpacing = 0.2f;
        [SerializeField] private float outputColumnSpacing = 0.6f;
        [SerializeField] private int outputColumns = 2;

        private List<Transform> _processedItems = new List<Transform>();
        private IObjectPool<GameObject> _outputPool;

        private void Awake()
        {
            InitializePool();
        }

        private void Start()
        {
            StartCoroutine(ProcessingRoutine());
        }

        private void InitializePool()
        {
            _outputPool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(processedPrefab, transform),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                collectionCheck: false,
                defaultCapacity: 20
            );
        }

        private IEnumerator ProcessingRoutine()
        {
            while (true)
            {
                // 입력 구역에서 원자재 하나 추출
                Transform rawMaterial = inputZone.ExtractItem();

                if (rawMaterial != null)
                {
                    // 원자재 기계 안으로 이동 연출 후 소멸 (풀링 반환 로직은 시스템에 따라 조절 가능)
                    // 현재는 간단히 비활성화 처리 (실제 환경에서는 해당 풀로 돌려보내는 기능 필요)
                    rawMaterial.gameObject.SetActive(false);

                    // 가공 대기
                    yield return new WaitForSeconds(processingTime);

                    // 결과물 생성 및 적재
                    CreateProcessedItem();
                }
                else
                {
                    // 원자재가 없으면 대기
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }



        private void CreateProcessedItem()
        {
            GameObject result = _outputPool.Get();
            int index = _processedItems.Count;
            int col = index % outputColumns;
            int row = index / outputColumns;

            Vector3 offset = new Vector3(col * outputColumnSpacing, row * outputVerticalSpacing, 0);
            result.transform.position = outputPivot.position + offset;
            result.transform.rotation = outputPivot.rotation;

            _processedItems.Add(result.transform);
        }

        // 결과물을 플레이어가 가져갈 때 호출할 메서드 (다음 단계 대비)
        public GameObject PopProcessedItem()
        {
            if (_processedItems.Count == 0) return null;

            int lastIndex = _processedItems.Count - 1;
            GameObject item = _processedItems[lastIndex].gameObject;
            _processedItems.RemoveAt(lastIndex);
            return item;
        }
    }
}
