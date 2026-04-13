using UnityEngine;
using UnityEngine.Pool;

namespace Supercent.Systems
{
    /// <summary>
    /// 게임 내 모든 원자재와 가공품의 풀링을 관리하는 글로벌 매니저.
    /// 메모리 누수를 방지하고 성능을 최적화하기 위해 사용합니다.
    /// </summary>
    public class ItemPoolManager : MonoBehaviour
    {
        public static ItemPoolManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject rawMaterialPrefab;
        [SerializeField] private GameObject processedItemPrefab;

        private IObjectPool<GameObject> _rawPool;
        private IObjectPool<GameObject> _processedPool;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializePools();
        }

        private void InitializePools()
        {
            _rawPool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(rawMaterialPrefab, transform),
                actionOnGet: (obj) => {
                    obj.SetActive(true);
                    if (rawMaterialPrefab != null) obj.transform.localScale = rawMaterialPrefab.transform.localScale;
                },
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                defaultCapacity: 30,
                maxSize: 100
            );

            _processedPool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(processedItemPrefab, transform),
                actionOnGet: (obj) => {
                    obj.SetActive(true);
                    if (processedItemPrefab != null)
                    {
                        obj.transform.localScale = processedItemPrefab.transform.localScale;
                        obj.transform.rotation = processedItemPrefab.transform.rotation;
                    }
                },
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                defaultCapacity: 30,
                maxSize: 100
            );
        }

        public GameObject GetRawMaterial() => _rawPool.Get();
        public void ReleaseRawMaterial(GameObject obj) => _rawPool.Release(obj);

        public GameObject GetProcessedItem() => _processedPool.Get();
        public void ReleaseProcessedItem(GameObject obj) => _processedPool.Release(obj);
    }
}
