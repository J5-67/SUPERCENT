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
        [Header("Visualization")]
        [SerializeField] private GameObject completeEffectPrefab;
        private UnityEngine.Pool.IObjectPool<GameObject> _effectPool;

        private void Awake()
        {
            if (completeEffectPrefab != null)
            {
                _effectPool = new UnityEngine.Pool.ObjectPool<GameObject>(
                    createFunc: () => Instantiate(completeEffectPrefab),
                    actionOnGet: (obj) => obj.SetActive(true),
                    actionOnRelease: (obj) => obj.SetActive(false),
                    defaultCapacity: 1
                );
            }
        }

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
            float visualInterval = 0.05f; 
            Vector3 targetPos = moneyReceiverPivot != null ? moneyReceiverPivot.position : transform.position;

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

                // [중요] 모든 결제는 전역 지갑(MoneyManager)의 숫자에서만 이루어집니다.
                // 손에 든 돈뭉치는 이미 지갑 숫자에 포함되어 있으므로, 연출용으로만 사용합니다.
                if (MoneyManager.Instance.CurrentMoney > 0)
                {
                    if (MoneyManager.Instance.SpendMoney(1))
                    {
                        _currentMoney += 1;
                        UpdateUI();

                        // 일정 주기로 돈이 날아가는 연출 수행
                        if (Time.time > lastVisualTime + visualInterval)
                        {
                            // 플레이어 손에 물리 돈뭉치가 있다면 그것을 꺼내서 날림
                            if (_playerInside.MoneyStackCount > 0)
                            {
                                GameObject visualMoney = _playerInside.PopMoneyFromStack();
                                if (visualMoney != null)
                                {
                                    MoneyManager.Instance.GiveVisualMoney(visualMoney.transform.position, targetPos);
                                    MoneyManager.Instance.ReleaseMoney(visualMoney);
                                }
                            }
                            else
                            {
                                // 손에 돈이 없으면 가상 돈뭉치를 생성해서 날림
                                PopAndFlyVisualMoney();
                            }
                            lastVisualTime = Time.time;
                        }
                    }
                }
                else
                {
                    // 돈이 아예 없으면 잠시 대기
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }

                // 결제 속도 조절
                yield return new WaitForSeconds(0.015f);
            }
            
            _depositCoroutine = null;
        }

        private void PopAndFlyVisualMoney()
        {
            // MoneyManager가 대신 연출을 관리하도록 위임 (존이 파괴되거나 비활성화되어도 돈이 허공에 멈추지 않음)
            Vector3 startPos = _playerInside.transform.position + Vector3.up;
            Vector3 targetPos = moneyReceiverPivot != null ? moneyReceiverPivot.position : transform.position;
            
            MoneyManager.Instance.GiveVisualMoney(startPos, targetPos);
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
            // 완료 연출 (풀링 사용)
            if (_effectPool != null)
            {
                GameObject effect = _effectPool.Get();
                effect.transform.position = transform.position;
                effect.transform.rotation = Quaternion.identity;
                StartCoroutine(ReleaseEffectWithDelay(effect, 2f));
            }

            // 다음 단계로 자동 전환
            _currentPhaseIndex++;
            Invoke(nameof(SetupCurrentPhase), 0.1f);
        }

        private IEnumerator ReleaseEffectWithDelay(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            _effectPool.Release(effect);
        }
    }
}
