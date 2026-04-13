using UnityEngine;
using Supercent.Player;
using Supercent.UI;
using Supercent.Field;

namespace Supercent.Systems
{
    public class GuideManager : MonoBehaviour
    {
        private enum TutorialStep
        {
            MineMinerals,       // 광물 6개 캐기
            ToProcessorInput,   // 가공기 입구로 이동
            ToProcessorOutput,  // 가공된 물건 집기
            ToSellZone,         // 판매대로 이동
            CollectMoney,       // 돈 줍기
            Completed           // 튜토리얼 종료
        }

        [Header("Tutorial State")]
        [SerializeField] private TutorialStep currentStep = TutorialStep.MineMinerals;

        [Header("Targets")]
        [SerializeField] private PlayerStackHandler playerStack;
        [SerializeField] private ResourceProcessor processor;
        [SerializeField] private MoneyZone moneyZone;
        [SerializeField] private TargetPointer navigationPointer;
        
        [Header("Guide Arrows")]
        [SerializeField] private FloatingArrow toResourceField;
        [SerializeField] private FloatingArrow toProcessorInput;
        [SerializeField] private FloatingArrow toProcessorOutput;
        [SerializeField] private FloatingArrow toSellZone;
        [SerializeField] private FloatingArrow toMoneyZone;

        private void Start()
        {
            // 모든 화살표 초기화 (한 번에 하나씩만 켤 것임)
            HideAllArrows();
            UpdatePointerTarget();
        }

        private void Update()
        {
            if (currentStep == TutorialStep.Completed) return;

            UpdateTutorialSequence();
        }

        private void UpdateTutorialSequence()
        {
            switch (currentStep)
            {
                case TutorialStep.MineMinerals:
                    ShowOnly(toResourceField);
                    if (playerStack.BackStackCount >= 6) 
                        TransitionTo(TutorialStep.ToProcessorInput);
                    break;

                case TutorialStep.ToProcessorInput:
                    ShowOnly(toProcessorInput);
                    // 가공기에 물건을 넣기 시작하거나(가방 비워짐) 결과물이 나왔을 때 다음으로
                    if (playerStack.BackStackCount == 0 || (processor != null && processor.ProcessedItemCount > 0))
                        TransitionTo(TutorialStep.ToProcessorOutput);
                    break;

                case TutorialStep.ToProcessorOutput:
                    ShowOnly(toProcessorOutput);
                    // 플레이어가 가공품을 하나라도 들었을 때
                    if (playerStack.FrontStackCount > 0)
                        TransitionTo(TutorialStep.ToSellZone);
                    break;

                case TutorialStep.ToSellZone:
                    ShowOnly(toSellZone);
                    // 돈이 필드에 생성되었을 때
                    if (moneyZone != null && moneyZone.HasMoney)
                        TransitionTo(TutorialStep.CollectMoney);
                    break;

                case TutorialStep.CollectMoney:
                    ShowOnly(toMoneyZone);
                    // 돈을 모두 주웠을 때 튜토리얼 끝
                    if (moneyZone != null && !moneyZone.HasMoney)
                        TransitionTo(TutorialStep.Completed);
                    break;
            }
        }

        private void TransitionTo(TutorialStep nextStep)
        {
            Debug.Log($"<color=cyan>[Tutorial]</color> Step Changed: {currentStep} -> {nextStep}");
            currentStep = nextStep;
            
            UpdatePointerTarget();

            if (currentStep == TutorialStep.Completed)
            {
                HideAllArrows();
            }
        }

        private void UpdatePointerTarget()
        {
            if (navigationPointer == null) return;

            // 특정 단계(광물 수거 후 가공기로 유도할 때)에서만 길안내 화살표 표시
            FloatingArrow targetArrow = currentStep switch
            {
                TutorialStep.ToProcessorInput => toProcessorInput,
                TutorialStep.ToProcessorOutput => toProcessorOutput,
                _ => null
            };

            navigationPointer.SetTarget(targetArrow != null ? targetArrow.transform : null);
        }

        private void ShowOnly(FloatingArrow target)
        {
            if (toResourceField != null) toResourceField.SetVisible(toResourceField == target);
            if (toProcessorInput != null) toProcessorInput.SetVisible(toProcessorInput == target);
            if (toProcessorOutput != null) toProcessorOutput.SetVisible(toProcessorOutput == target);
            if (toSellZone != null) toSellZone.SetVisible(toSellZone == target);
            if (toMoneyZone != null) toMoneyZone.SetVisible(toMoneyZone == target);
        }

        private void HideAllArrows()
        {
            if (toResourceField != null) toResourceField.SetVisible(false);
            if (toProcessorInput != null) toProcessorInput.SetVisible(false);
            if (toProcessorOutput != null) toProcessorOutput.SetVisible(false);
            if (toSellZone != null) toSellZone.SetVisible(false);
            if (toMoneyZone != null) toMoneyZone.SetVisible(false);
        }
    }
}
