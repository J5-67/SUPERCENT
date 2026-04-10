using UnityEngine;
using UnityEngine.UI;

namespace Supercent.Systems
{
    public class MoneyManager : MonoBehaviour
    {
        public static MoneyManager Instance { get; private set; }

        [SerializeField] private Text moneyText;
        private int _currentMoney = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            UpdateUI();
        }

        public void AddMoney(int amount)
        {
            _currentMoney += amount;
            UpdateUI();
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
