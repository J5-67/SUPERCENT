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
        [SerializeField] private int maxOutputCapacity = 10;
        
        [Header("UI Integration")]
        [SerializeField] private GameObject processorMaxIndicator;

        private List<Transform> _processedItems = new List<Transform>();

        public int ProcessedItemCount => _processedItems.Count;

        private void Awake()
        {
            InitializePool();
        }

        private void Start()
        {
            if (processorMaxIndicator != null) processorMaxIndicator.SetActive(false);
            StartCoroutine(ProcessingRoutine());
        }

        private void InitializePool()
        {
            // 글로벌 풀링 시스템(ItemPoolManager) 도입으로 로컬 풀 초기화 삭제
        }

        private IEnumerator ProcessingRoutine()
        {
            while (true)
            {
                // 적재 공간이 다 찼으면 대기
                if (_processedItems.Count >= maxOutputCapacity)
                {
                    if (processorMaxIndicator != null) processorMaxIndicator.SetActive(true);
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                if (processorMaxIndicator != null) processorMaxIndicator.SetActive(false);

                // 입력 구역에서 원자재 하나 추출
                Transform rawMaterial = inputZone.ExtractItem();

                if (rawMaterial != null)
                {
                    // 원자재를 글로벌 풀로 반환하여 메모리 누수 방지
                    Supercent.Systems.ItemPoolManager.Instance.ReleaseRawMaterial(rawMaterial.gameObject);

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
            GameObject result = Supercent.Systems.ItemPoolManager.Instance.GetProcessedItem();
            result.transform.SetParent(transform);
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

            // 하나라도 빠지면 MAX 표시 비활성화
            if (processorMaxIndicator != null && _processedItems.Count < maxOutputCapacity)
            {
                processorMaxIndicator.SetActive(false);
            }
            return item;
        }
    }
}
