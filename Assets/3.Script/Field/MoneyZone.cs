using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Supercent.Player;
using Supercent.Systems;

namespace Supercent.Field
{
    public class MoneyZone : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject moneyPrefab;
        [SerializeField] private Transform spawnPivot;
        [SerializeField] private float verticalSpacing = 0.1f;
        [SerializeField] private int columns = 3;
        [SerializeField] private float columnSpacing = 0.4f;
        [SerializeField] private float rowSpacing = 0.4f;

        private List<Transform> _stackedMoney = new List<Transform>();
        private bool _isCollecting = false;
        private PlayerStackHandler _playerInside;

        public bool HasMoney => _stackedMoney.Count > 0;

        public void SpawnMoney()
        {
            if (MoneyManager.Instance == null) return;
            
            GameObject newMoney = MoneyManager.Instance.GetMoney();
            if (newMoney == null) return;

            int index = _stackedMoney.Count;
            int col = index % columns;
            int row = index / columns;

            Vector3 offset = new Vector3(col * columnSpacing, row * verticalSpacing, row * rowSpacing);
            newMoney.transform.position = spawnPivot.position + offset;
            newMoney.transform.rotation = spawnPivot.rotation;

            _stackedMoney.Add(newMoney.transform);
        }

        private void OnTriggerEnter(Collider other)
        {
            var handler = other.GetComponentInParent<PlayerStackHandler>();
            if (handler != null)
            {
                _playerInside = handler;
                if (!_isCollecting) StartCoroutine(CollectRoutine());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var handler = other.GetComponentInParent<PlayerStackHandler>();
            if (handler != null && _playerInside == handler)
            {
                _playerInside = null;
            }
        }

        private IEnumerator CollectRoutine()
        {
            _isCollecting = true;

            // 플레이어가 구역 안에 있는 동안은 계속해서 수거 시도
            while (_playerInside != null || _stackedMoney.Count > 0)
            {
                if (_playerInside != null && _stackedMoney.Count > 0)
                {
                    int lastIdx = _stackedMoney.Count - 1;
                    Transform moneyTrm = _stackedMoney[lastIdx];
                    _stackedMoney.RemoveAt(lastIdx);

                    if (moneyTrm.TryGetComponent<Money>(out var money))
                    {
                        _playerInside.AddMoneyToStack(moneyTrm);
                        MoneyManager.Instance.AddMoney(money.Value);
                    }
                    yield return new WaitForSeconds(0.05f); // 수거 속도
                }
                else
                {
                    // 수거할 게 없거나 플레이어가 나가면 잠시 대기
                    yield return new WaitForSeconds(0.1f);
                    if (_playerInside == null && _stackedMoney.Count == 0) break;
                }
            }

            _isCollecting = false;
        }
    }
}
