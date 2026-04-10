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

        private List<GameObject> _moneyStack = new List<GameObject>();
        private bool _isCollecting = false;
        private PlayerStackHandler _playerInside;

        public void SpawnMoney()
        {
            int index = _moneyStack.Count;
            int col = index % columns;
            int row = index / columns;

            Vector3 offset = new Vector3((col - 1) * columnSpacing, row * verticalSpacing, 0);
            Vector3 targetPos = spawnPivot.position + offset;

            GameObject newMoney = Instantiate(moneyPrefab, spawnPivot.position, Quaternion.identity);
            newMoney.transform.position = targetPos;
            _moneyStack.Add(newMoney);
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
            while (_playerInside != null || _moneyStack.Count > 0)
            {
                if (_playerInside != null && _moneyStack.Count > 0)
                {
                    int lastIdx = _moneyStack.Count - 1;
                    GameObject moneyObj = _moneyStack[lastIdx];
                    _moneyStack.RemoveAt(lastIdx);

                    if (moneyObj.TryGetComponent<Money>(out var money))
                    {
                        _playerInside.AddMoneyToStack(money.transform);
                        MoneyManager.Instance.AddMoney(money.Value);
                    }
                    yield return new WaitForSeconds(0.05f); // 수거 속도
                }
                else
                {
                    // 수거할 게 없거나 플레이어가 나가면 잠시 대기
                    yield return new WaitForSeconds(0.1f);
                    if (_playerInside == null && _moneyStack.Count == 0) break;
                }
            }

            _isCollecting = false;
        }
    }
}
