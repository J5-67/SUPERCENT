using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace Supercent.UI
{
    /// <summary>
    /// New Input System의 OnScreenControl을 활용한 모바일 가상 조이스틱
    /// </summary>
    public class VirtualJoystick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("UI References")]
        [SerializeField] private RectTransform container; // 조이스틱 전체 부모 (일반적으로 투명 레이어)
        [SerializeField] private RectTransform handle;

        [Header("Settings")]
        [SerializeField] private float movementRange = 100f;
        [SerializeField] private bool hideOnRelease = true;
        
        [InputControl(layout = "Vector2")]
        [SerializeField] private string _controlPath;

        protected override string controlPathInternal
        {
            get => _controlPath;
            set => _controlPath = value;
        }

        private Vector2 _originalHandlePosition;
        private CanvasGroup _canvasGroup;

        private void Start()
        {
            if (container == null) container = GetComponent<RectTransform>();
            
            // handle이 할당되지 않았다면 자식 오브젝트에서 검색 시도
            if (handle == null)
            {
                handle = transform.Find("Handle")?.GetComponent<RectTransform>();
                if (handle == null) handle = transform.GetComponentInChildren<RectTransform>();
            }

            if (handle == null)
            {
                Debug.LogError($"{gameObject.name}: Joystick Handle이 할당되지 않았습니다! Hierarchy에서 할당해 주세요.");
                enabled = false;
                return;
            }

            _originalHandlePosition = Vector2.zero;
            
            if (hideOnRelease)
            {
                if (!TryGetComponent(out _canvasGroup))
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
                _canvasGroup.alpha = 0f; // 시작 시 숨김
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null) return;

            // 터치한 위치로 조이스틱 컨테이너 이동 (Floating 효과)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)container.parent, 
                eventData.position, 
                eventData.pressEventCamera, 
                out var localPos
            );
            
            container.anchoredPosition = localPos;
            handle.anchoredPosition = Vector2.zero; // 핸들을 컨테이너 중심으로 초기화

            if (hideOnRelease && _canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container, 
                eventData.position, 
                eventData.pressEventCamera, 
                out var currentPosition
            );

            // 배경(Ring) 반지름을 자동으로 계산 (movementRange가 0보다 크면 그 값을 사용, 아니면 반지름 사용)
            float radius = movementRange > 0 ? movementRange : container.rect.width * 0.5f;
            Vector2 movement = Vector2.ClampMagnitude(currentPosition, radius);
            
            handle.anchoredPosition = movement;
            
            // Input System에 값 전달 (-1 ~ 1 사이의 값으로 정규화)
            SendValueToControl(movement / radius);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (hideOnRelease && _canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }

            handle.anchoredPosition = Vector2.zero;
            SendValueToControl(Vector2.zero);
        }
    }
}
