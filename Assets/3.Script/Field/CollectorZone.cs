using System.Collections;
using UnityEngine;
using Supercent.Player;

namespace Supercent.Field
{
    /// <summary>
    /// 가공기에서 결과물을 수거하는 전용 구역 스크립트
    /// </summary>
    public class CollectorZone : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ResourceProcessor targetProcessor;
        public ResourceProcessor TargetProcessor => targetProcessor;

        [Header("Settings")]
        [SerializeField] private float pickupInterval = 0.2f;

        private float _lastPickupTime;

        private void Start()
        {
            if (targetProcessor == null) targetProcessor = GetComponent<ResourceProcessor>();
            if (targetProcessor == null) targetProcessor = GetComponentInParent<ResourceProcessor>();
        }

        private void OnTriggerStay(Collider other)
        {
            TryPickup(other);
        }

        private void OnCollisionStay(Collision collision)
        {
            TryPickup(collision.collider);
        }

        private void TryPickup(Collider foreign)
        {
            if (Time.time < _lastPickupTime + pickupInterval) return;

            // 1. 직접 시도
            if (foreign.TryGetComponent<PlayerStackHandler>(out var handler))
            {
                ExecutePickup(handler);
                return;
            }

            // 2. 부모 단위 안전 탐색
            handler = foreign.GetComponentInParent<PlayerStackHandler>();
            if (handler != null)
            {
                ExecutePickup(handler);
            }
        }

        private void ExecutePickup(PlayerStackHandler handler)
        {
            if (handler.CanAdd && handler.CanAddFront)
            {
                GameObject item = targetProcessor.PopProcessedItem();
                if (item != null)
                {
                    bool added = handler.AddExistingToStack(item.transform);
                    if (added)
                    {
                        _lastPickupTime = Time.time;
                    }
                }
            }
            else
            {
                // 스택이 가득 찼는데 획득을 시도하면 MAX 표시
                handler.ShowMaxIndicator();
                _lastPickupTime = Time.time; // 알림 중복 방지를 위해 약간의 쿨타임 적용
            }
        }
    }
}
