using UnityEngine;

namespace Supercent.UI
{
    public class TargetPointer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform player;
        [SerializeField] private Vector3 offset = new Vector3(0, 0.1f, 0);
        [SerializeField] private Vector3 rotationOffset = new Vector3(0, 0, 0);
        [SerializeField] private float hideDistance = 3f; 
        [SerializeField] private float rotationSpeed = 10f;

        private Transform _target;
        private GameObject _visual;

        private void Awake()
        {
            _visual = transform.GetChild(0).gameObject; // 첫 번째 자식이 화살표 모델이라고 가정
            if (_visual != null) _visual.SetActive(false);
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            if (_visual != null) _visual.SetActive(_target != null);
        }

        // 모든 오브젝트의 이동이 완료된 후 실행되는 LateUpdate에서 처리
        private void LateUpdate()
        {
            if (_target == null || player == null)
            {
                if (_visual != null && _visual.activeSelf) _visual.SetActive(false);
                return;
            }

            // [팁] 화살표가 플레이어의 자식이라면 아래 포지션 설정 코드는 필요 없습니다.
            // 자식이 아닐 경우에만 아래 주석을 해제하세요. 
            // transform.position = player.position + offset;

            // 타겟 방향 계산 (Y축 고정)
            Vector3 direction = (_target.position - transform.position);
            direction.y = 0;

            if (direction.sqrMagnitude > 0.01f)
            {
                // 방향 계산 시 모델 고유의 오프셋 각도 적용
                Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(rotationOffset);
                
                // 회전 속도를 높여 더 빠릿하게 반응하도록 수정
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed * 2f);
            }

            // 거리에 따른 활성화 처리
            float distSqr = (player.position - _target.position).sqrMagnitude;
            bool shouldShow = distSqr > (hideDistance * hideDistance);
            
            if (_visual != null && _visual.activeSelf != shouldShow)
            {
                _visual.SetActive(shouldShow);
            }
        }
    }
}
