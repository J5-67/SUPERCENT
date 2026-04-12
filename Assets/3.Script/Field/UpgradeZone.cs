using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Supercent.Player;
using Supercent.Systems;

namespace Supercent.Field
{
    [System.Serializable]
    public struct UpgradePhase
    {
        public string phaseName;
        public int cost;
        public Sprite icon;
        public UnityEvent onComplete;
    }

    public class UpgradeZone : MonoBehaviour
    {
        [Header("Phase Settings")]
        [SerializeField] private List<UpgradePhase> phases = new List<UpgradePhase>();
        [SerializeField] private bool disableOnAllComplete = true;

        [Header("Logic Settings")]
        [SerializeField] private float depositInterval = 0.1f;
        [SerializeField] private Transform moneyReceiverPivot;

        [Header("UI")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text needText;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject completeEffectPrefab;

        private int _currentPhaseIndex = 0;
        private int _currentMoney = 0;
        private bool _isUpgraded = false;
        private PlayerStackHandler _playerInside;
        private Coroutine _depositCoroutine;

        private void Start()
        {
            SetupCurrentPhase();
        }

        private void SetupCurrentPhase()
        {
            if (_currentPhaseIndex >= phases.Count)
            {
                if (disableOnAllComplete) gameObject.SetActive(false);
                return;
            }

            _isUpgraded = false;
            _currentMoney = 0;

            var phase = phases[_currentPhaseIndex];
            if (iconImage != null && phase.icon != null)
            {
                iconImage.sprite = phase.icon;
            }

            UpdateUI();
        }

        // 특정 인덱스로 강제 이동 (필요시)
        public void SetPhase(int index)
        {
            _currentPhaseIndex = index;
            SetupCurrentPhase();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isUpgraded) return;
            
            if (other.TryGetComponent<PlayerStackHandler>(out var handler))
            {
                _playerInside = handler;
                if (_depositCoroutine == null) 
                    _depositCoroutine = StartCoroutine(DepositRoutine());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<PlayerStackHandler>(out var handler))
            {
                if (_playerInside == handler)
                {
                    _playerInside = null;
                    // Coroutine을 여기서 직접 중지하지 않고, 
                    // DepositRoutine 내부의 while 조건(_playerInside != null)에서 루프가 자연스럽게 종료되도록 유도합니다.
                    // 이를 통해 현재 날아가고 있는 돈이 공중에 멈추는 현상을 방지합니다.
                }
            }
        }

        private IEnumerator DepositRoutine()
        {
            float lastVisualTime = 0f;
            float visualInterval = 0.05f; // 비주얼 돈뭉치가 날라가는 간격

            while (_playerInside != null && !_isUpgraded)
            {
                if (_currentPhaseIndex >= phases.Count) break;
                var currentPhase = phases[_currentPhaseIndex];
                
                int remainingNeed = currentPhase.cost - _currentMoney;
                if (remainingNeed <= 0)
                {
                    CompleteUpgrade();
                    break;
                }

                // 1. 먼저 전역 지갑(MoneyManager)의 돈을 확인
                if (MoneyManager.Instance.CurrentMoney > 0)
                {
                    // 소모할 금액 결정 (남은 금액이 적으면 그만큼만)
                    int amountToTake = 1;
                    if (MoneyManager.Instance.SpendMoney(amountToTake))
                    {
                        _currentMoney += amountToTake;
                        UpdateUI();

                        // 일정 시간 간격으로 비주얼 돈뭉치 날리기
                        if (Time.time > lastVisualTime + visualInterval)
                        {
                            PopAndFlyVisualMoney();
                            lastVisualTime = Time.time;
                        }
                    }
                }
                // 2. 전역 지갑에 돈이 없는데 플레이어 스택에 돈뭉치가 있다면 지갑으로 옮김
                else if (_playerInside.MoneyStackCount > 0)
                {
                    GameObject poppedMoney = _playerInside.PopMoneyFromStack();
                    if (poppedMoney != null)
                    {
                        if (poppedMoney.TryGetComponent<Money>(out var moneyComp))
                        {
                            MoneyManager.Instance.AddMoney(moneyComp.Value);
                        }
                        MoneyManager.Instance.ReleaseMoney(poppedMoney);
                    }
                }
                else
                {
                    // 돈이 아예 없으면 잠시 대기
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }

                // 초고속 연사를 위해 매우 짧은 대기
                yield return new WaitForSeconds(0.01f);
            }
            
            _depositCoroutine = null;
        }

        private void PopAndFlyVisualMoney()
        {
            // 사실적인 연출을 위해 플레이어 위치에서 리시버로 날아가는 가짜 돈뭉치 생성
            GameObject visualMoney = MoneyManager.Instance.GetMoney();
            if (visualMoney != null)
            {
                visualMoney.transform.position = _playerInside.transform.position + Vector3.up;
                StartCoroutine(FlyToReceiver(visualMoney.transform, moneyReceiverPivot != null ? moneyReceiverPivot.position : transform.position, true));
            }
        }

        private IEnumerator FlyToReceiver(Transform item, Vector3 targetPos, bool releaseAtEnd = false)
        {
            Vector3 startPos = item.position;
            float elapsed = 0f;
            float flyDuration = 0.25f;

            item.SetParent(null);

            while (elapsed < flyDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flyDuration;

                // 포물선 궤적
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
                currentPos.y += Mathf.Sin(t * Mathf.PI) * 1.5f;
                
                item.position = currentPos;
                item.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.5f, t);

                yield return null;
            }

            if (releaseAtEnd)
            {
                MoneyManager.Instance.ReleaseMoney(item.gameObject);
            }
            else
            {
                item.position = targetPos;
            }
        }

        private void UpdateUI()
        {
            if (_currentPhaseIndex >= phases.Count) return;
            var currentPhase = phases[_currentPhaseIndex];

            if (progressSlider != null)
            {
                progressSlider.maxValue = currentPhase.cost;
                progressSlider.value = _currentMoney;
            }

            if (needText != null)
            {
                needText.text = (currentPhase.cost - _currentMoney).ToString();
            }
        }

        public void ForceCompleteUpgrade()
        {
            CompleteUpgrade();
        }

        private void CompleteUpgrade()
        {
            if (_isUpgraded || _currentPhaseIndex >= phases.Count) return;

            _isUpgraded = true;
            
            // 현재 단계의 이벤트 실행
            phases[_currentPhaseIndex].onComplete?.Invoke();
            
            if (completeEffectPrefab != null)
            {
                Instantiate(completeEffectPrefab, transform.position, Quaternion.identity);
            }

            // 다음 단계로 자동 전환
            _currentPhaseIndex++;
            Invoke(nameof(SetupCurrentPhase), 0.1f);
        }
    }
}
