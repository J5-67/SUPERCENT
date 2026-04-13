using UnityEngine;
using UnityEngine.UI;
using Supercent.Field;
using Supercent.Core;

namespace Supercent.Systems
{
    public class MoneyManager : MonoBehaviour
    {
        public static MoneyManager Instance { get; private set; }

        [SerializeField] private Text moneyText;
        [SerializeField] private GameObject moneyPrefab;
        [SerializeField] private GameObject drillUpgradeZone;

        private int _currentMoney = 0;
        private bool _isFirstMoneyUnlocked = false;
        
        private UnityEngine.Pool.IObjectPool<GameObject> _moneyPool;

        public int CurrentMoney => _currentMoney;

        /* 
        private void OnGUI()
        {
            // 씬 내의 모든 업그레이드 존을 찾아 버튼 표시
            UpgradeZone[] zones = FindObjectsByType<UpgradeZone>(FindObjectsSortMode.None);
            if (zones == null) return;

            int count = 0;
            foreach (var zone in zones)
            {
                if (!zone.gameObject.activeInHierarchy) continue;

                // 이미 완료된(사라질 예정인) 존은 제외
                if (GUI.Button(new Rect(10, 10 + (count * 40), 180, 35), $"Unlock: {zone.name}"))
                {
                    zone.ForceCompleteUpgrade();
                }
                count++;
            }
        }
        */

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializePool();
        }

        private void InitializePool()
        {
            _moneyPool = new UnityEngine.Pool.ObjectPool<GameObject>(
                createFunc: () => Instantiate(moneyPrefab),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                defaultCapacity: 50,
                maxSize: 200
            );
        }

        public GameObject GetMoney() => _moneyPool?.Get();
        public void ReleaseMoney(GameObject moneyObj) => _moneyPool?.Release(moneyObj);

        private void Start()
        {
            UpdateUI();
            // 드릴 업그레이드 존은 처음에 숨김
            if (drillUpgradeZone != null) drillUpgradeZone.SetActive(false);
        }

        public void AddMoney(int amount)
        {
            _currentMoney += amount;
            UpdateUI();

            // 처음으로 돈을 얻었을 때 업그레이드 존 해금 및 카메라 연출
            if (!_isFirstMoneyUnlocked && _currentMoney > 0)
            {
                _isFirstMoneyUnlocked = true;
                if (drillUpgradeZone != null) 
                {
                    drillUpgradeZone.SetActive(true);
                    
                    // 카메라 연출 추가
                    if (TopDownCamera.Instance != null)
                    {
                        TopDownCamera.Instance.ShowTargetTemporarily(drillUpgradeZone.transform, 2.0f);
                    }
                }
            }
        }

        public bool SpendMoney(int amount)
        {
            if (_currentMoney < amount) return false;
            _currentMoney -= amount;
            UpdateUI();
            return true;
        }

        public void GiveVisualMoney(Vector3 startPos, Vector3 targetPos)
        {
            GameObject money = GetMoney();
            if (money != null)
            {
                StartCoroutine(FlyMoneyRoutine(money.transform, startPos, targetPos));
            }
        }

        private System.Collections.IEnumerator FlyMoneyRoutine(Transform item, Vector3 startPos, Vector3 targetPos)
        {
            float elapsed = 0f;
            float flyDuration = 0.3f;
            item.position = startPos;
            item.localScale = Vector3.one;

            while (elapsed < flyDuration)
            {
                if (item == null) yield break;
                
                elapsed += Time.deltaTime;
                float t = elapsed / flyDuration;

                // 포물선 궤적
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
                currentPos.y += Mathf.Sin(t * Mathf.PI) * 1.2f;
                
                item.position = currentPos;
                item.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.5f, t);

                yield return null;
            }

            ReleaseMoney(item.gameObject);
        }

        private void UpdateUI()
        {
            if (moneyText != null)
            {
                moneyText.text = _currentMoney.ToString("N0");
            }
        }
    }
}
