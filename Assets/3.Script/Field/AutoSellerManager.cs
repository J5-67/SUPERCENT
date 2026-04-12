using UnityEngine;
using System.Collections.Generic;

namespace Supercent.Field
{
    /// <summary>
    /// 업그레이드 시스템과 연동하여 자동 판매원(AutoSeller)을 생성하고 관리하는 클래스
    /// </summary>
    public class AutoSellerManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CollectorZone sourceCollectorZone;
        [SerializeField] private SellZone targetSellZone;
        [SerializeField] private GameObject sellerPrefab;
        [SerializeField] private Transform spawnPoint;

        private readonly List<AutoSeller> _activeSellers = new List<AutoSeller>();

        /// <summary>
        /// 새로운 자동 판매원을 추가합니다. (UpgradeZone의 UnityEvent에서 호출 가능)
        /// </summary>
        public void AddSeller()
        {
            if (IsSetupInvalid()) return;

            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
            
            // 지면 높이 보정 (기본 높이가 낮아 땅에 묻히는 현상 방지)
            if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            {
                spawnPosition = hit.point;
            }

            GameObject sellerObject = Instantiate(sellerPrefab, spawnPosition, Quaternion.identity);
            
            if (sellerObject.TryGetComponent<AutoSeller>(out var seller))
            {
                seller.Initialize(sourceCollectorZone, targetSellZone);
                _activeSellers.Add(seller);
            }
        }

        private bool IsSetupInvalid()
        {
            if (sellerPrefab == null) return true;
            if (sourceCollectorZone == null) return true;
            if (targetSellZone == null) return true;
            
            return false;
        }
    }
}
