using UnityEngine;

namespace Supercent.Field
{
    public class AutoMinerManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ResourceField targetField;
        [SerializeField] private ResourceZone inputZone; // 광물이 전달될 구역
        [SerializeField] private GameObject minerPrefab;
        [SerializeField] private GameObject rawMaterialPrefab; // 소환할 원자재 프리팹

        [Header("Settings")]
        [SerializeField] private int minerCount = 3;

        private bool _isUnlocked = false;

        private void Start()
        {
            if (targetField == null) targetField = FindFirstObjectByType<ResourceField>();
            if (inputZone == null) inputZone = FindFirstObjectByType<ResourceZone>();
        }

        public void UnlockAutoMiners()
        {
            if (_isUnlocked || targetField == null) return;
            _isUnlocked = true;

            SpawnMinersAtField();
        }

        private void SpawnMinersAtField()
        {
            float spacing = targetField.Spacing;
            int cols = targetField.Cols;
            int rows = targetField.Rows;

            float totalWidth = (cols - 1) * spacing;
            float totalDepth = (rows - 1) * spacing;
            
            float maxX = totalWidth * 0.5f;
            float startZ = -totalDepth * 0.5f - 2f;

            // 프리팹 자체의 Y 높이값 유지
            float prefabY = minerPrefab.transform.position.y;

            for (int i = 0; i < minerCount; i++)
            {
                float spawnX = maxX - (i * spacing);
                Vector3 localPos = new Vector3(spawnX, prefabY, startZ);
                Vector3 spawnPos = targetField.transform.TransformPoint(localPos);
                
                GameObject minerObj = Instantiate(minerPrefab, spawnPos, targetField.transform.rotation);
                
                if (minerObj.TryGetComponent<AutoMiner>(out var miner))
                {
                    // 스폰 위치가 필드 뒤쪽(-2m)이므로, 필드 전체(totalDepth)를 캐려면 2m 이상의 거리가 필요함
                    miner.Initialize(inputZone, rawMaterialPrefab, totalDepth + 2.5f);
                }
            }
        }
    }
}
