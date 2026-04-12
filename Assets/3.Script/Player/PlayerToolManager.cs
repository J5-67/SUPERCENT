using UnityEngine;

namespace Supercent.Player
{
    public class PlayerToolManager : MonoBehaviour
    {
        [Header("Tools (Visuals)")]
        [SerializeField] private GameObject drillModel;
        [SerializeField] private GameObject drillCarModel;
        [SerializeField] private Transform drillCarPivot; 

        [Header("Mining Settings")]
        [SerializeField] private LayerMask resourceLayer;
        [SerializeField] private float defaultMiningRate = 1.0f;
        [SerializeField] private float drillMiningRate = 0.15f;
        [SerializeField] private int additionalStackLimit = 10;
        [SerializeField] private float minShowDuration = 1.0f; 
        [SerializeField] private float basicMiningRadius = 1.0f;
        [SerializeField] private float drillRadius = 2.0f;
        [SerializeField] private float drillCarRadius = 4.0f;

        private enum ToolType { None, Drill, DrillCar }
        private ToolType _currentTool = ToolType.None;

        private float _lastMiningTime;
        private float _drillHideTimer;
        private SphereCollider _drillCollider;
        private SphereCollider _drillCarCollider;

        public bool HasDrill => _currentTool != ToolType.None;

        private void Start()
        {
            if (drillModel != null)
            {
                drillModel.SetActive(false);
                _drillCollider = drillModel.GetComponent<SphereCollider>();
            }
            if (drillCarModel != null)
            {
                drillCarModel.SetActive(false);
                _drillCarCollider = drillCarModel.GetComponent<SphereCollider>();
            }
        }

        public void UnlockDrill()
        {
            if (_currentTool != ToolType.None) return;
            
            _currentTool = ToolType.Drill;
            if (drillModel != null)
            {
                drillModel.SetActive(false);
                if (_drillCollider == null) _drillCollider = drillModel.GetComponent<SphereCollider>();
            }

            if (TryGetComponent<PlayerStackHandler>(out var stackHandler))
            {
                stackHandler.IncreaseStackLimit(additionalStackLimit);
            }
        }

        public void UpgradeToDrillCar()
        {
            _currentTool = ToolType.DrillCar;
            
            if (drillModel != null) drillModel.SetActive(false);
            
            if (drillCarModel != null)
            {
                // 지정된 위치(피벗)가 있다면 해당 위치로 모델 이동 및 부착
                if (drillCarPivot != null)
                {
                    drillCarModel.transform.SetParent(drillCarPivot);
                    drillCarModel.transform.localPosition = Vector3.zero;
                    drillCarModel.transform.localRotation = Quaternion.identity;
                    drillCarModel.transform.localScale = Vector3.one;
                }

                drillCarModel.SetActive(false);
                if (_drillCarCollider == null) _drillCarCollider = drillCarModel.GetComponent<SphereCollider>();
            }

            // 드릴카 업그레이드 시 스택 한계 한 번 더 상향 (기획에 따라 조절)
            if (TryGetComponent<PlayerStackHandler>(out var stackHandler))
            {
                stackHandler.IncreaseStackLimit(additionalStackLimit);
            }
        }

        private void Update()
        {
            PerformMiningCheck();
        }

        private void PerformMiningCheck()
        {
            float currentRate = _currentTool == ToolType.None ? defaultMiningRate : drillMiningRate;
            if (Time.time < _lastMiningTime + currentRate) return;

            GameObject activeModel = _currentTool switch
            {
                ToolType.Drill => drillModel,
                ToolType.DrillCar => drillCarModel,
                _ => null
            };

            float radius = _currentTool switch
            {
                ToolType.DrillCar => drillCarRadius,
                ToolType.Drill => drillRadius,
                _ => basicMiningRadius
            };

            Vector3 center = transform.position;
            if (activeModel != null && activeModel.activeSelf)
            {
                if (_currentTool == ToolType.DrillCar && _drillCarCollider != null)
                    center = _drillCarCollider.transform.TransformPoint(_drillCarCollider.center);
                else if (_currentTool == ToolType.Drill && _drillCollider != null)
                    center = _drillCollider.transform.TransformPoint(_drillCollider.center);
            }

            Collider[] hits = Physics.OverlapSphere(center, radius, resourceLayer);
            bool isNearResource = false;

            if (hits.Length > 0)
            {
                if (TryGetComponent<PlayerStackHandler>(out var handler))
                {
                    _lastMiningTime = Time.time;
                    
                    if (_currentTool == ToolType.None)
                    {
                        // 곡괭이: 가장 가까운 노드 하나만 수집
                        var closestNode = FindClosestNode(center, hits);
                        if (closestNode != null)
                        {
                            closestNode.CollectBy(handler);
                            isNearResource = true;
                        }
                    }
                    else
                    {
                        // 드릴: 범위 내 모든 광물 타격
                        foreach (var hit in hits)
                        {
                            if (hit.TryGetComponent<Field.ResourceNode>(out var node))
                            {
                                node.CollectBy(handler);
                                isNearResource = true;
                            }
                        }
                    }
                }
            }

            // 시각적 연출 제어
            if (activeModel != null)
            {
                UpdateToolVisuals(isNearResource, activeModel);
            }
        }

        private Field.ResourceNode FindClosestNode(Vector3 center, Collider[] hits)
        {
            Field.ResourceNode closest = null;
            float minSqrDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Field.ResourceNode>(out var node))
                {
                    float sqrDist = (hit.transform.position - center).sqrMagnitude;
                    if (sqrDist < minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        closest = node;
                    }
                }
            }
            return closest;
        }

        private void UpdateToolVisuals(bool isNearResource, GameObject activeModel)
        {
            if (activeModel == null) return;

            if (isNearResource)
            {
                _drillHideTimer = Time.time + minShowDuration;
            }

            bool shouldShow = isNearResource || Time.time < _drillHideTimer;
            if (activeModel.activeSelf != shouldShow)
            {
                activeModel.SetActive(shouldShow);
            }
        }
    }
}
