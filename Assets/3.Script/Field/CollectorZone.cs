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

        [Header("Settings")]
        [SerializeField] private float pickupInterval = 0.2f;

        private float _lastPickupTime;

        private void OnTriggerStay(Collider other)
        {
            // 수거 주기 체크
            if (Time.time < _lastPickupTime + pickupInterval) return;

            if (other.TryGetComponent<PlayerStackHandler>(out var handler))
            {
                // 플레이어 인벤토리에 여유가 있고 가공기에 결과물이 있다면
                if (handler.CanAdd)
                {
                    GameObject item = targetProcessor.PopProcessedItem();
                    if (item != null)
                    {
                        handler.AddExistingToStack(item.transform);
                        _lastPickupTime = Time.time;
                    }
                }
            }
        }
    }
}
