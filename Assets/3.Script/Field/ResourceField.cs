using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace Supercent.Field
{
    public class ResourceField : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int rows = 16;
        [SerializeField] private int cols = 8;
        [SerializeField] private float spacing = 1.5f;
        [SerializeField] private float respawnTime = 3.0f;

        [Header("Pool Settings")]
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private int defaultCapacity = 128;
        [SerializeField] private int maxSize = 200;

        private IObjectPool<GameObject> _pool;

        private void Awake()
        {
            InitializePool();
        }

        private void Start()
        {
            GenerateField();
        }

        private void InitializePool()
        {
            _pool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(nodePrefab, transform),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        [ContextMenu("Generate Field")]
        public void GenerateField()
        {
            if (nodePrefab == null)
            {
                Debug.LogWarning("ResourceField: Node Prefab이 할당되지 않았습니다.");
                return;
            }

            Vector3 startPos = transform.position;
            
            // 중앙 정렬을 위해 오프셋 계산
            float xOffset = (cols - 1) * spacing * 0.5f;
            float zOffset = (rows - 1) * spacing * 0.5f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    GameObject node = _pool.Get();
                    node.transform.localPosition = new Vector3(
                        (c * spacing) - xOffset,
                        0,
                        (r * spacing) - zOffset
                    );

                    // 초기화 로직 추가
                    if (node.TryGetComponent<ResourceNode>(out var resourceNode))
                    {
                        resourceNode.Initialize(this);
                    }
                }
            }
        }

        public void ReleaseNode(GameObject node, Vector3 position)
        {
            _pool.Release(node);
            StartCoroutine(RespawnRoutine(position));
        }

        private IEnumerator RespawnRoutine(Vector3 position)
        {
            yield return new WaitForSeconds(respawnTime);
            
            GameObject newNode = _pool.Get();
            newNode.transform.localPosition = position;
            
            if (newNode.TryGetComponent<ResourceNode>(out var resourceNode))
            {
                resourceNode.Initialize(this);
            }
        }
    }
}
