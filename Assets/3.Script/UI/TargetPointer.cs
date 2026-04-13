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

        [Header("References")]
        [SerializeField] private GameObject visual;

        private Transform _target;

        private void Awake()
        {
            // Player가 인스펙터에 할당되지 않았다면 부모 오브젝트를 Player로 간주 (혹은 루트 오브젝트)
            if (player == null)
            {
                if (transform.parent != null)
                {
                    player = transform.parent;
                }
                else
                {
                    Debug.LogWarning("[TargetPointer] Player 변수가 할당되지 않았습니다. 인스펙터에서 플레이어 오브젝트를 연결해주세요.", this);
                }
            }

            if (visual == null)
            {
                Debug.LogWarning("[TargetPointer] 화살표 그래픽(Visual)이 할당되지 않았습니다! 인스펙터에서 화살표 오브젝트를 명시적으로 연결해주세요.", this);
            }
            else
            {
                visual.SetActive(false);
            }
        }

        [SerializeField] private float followRadius = 1.5f;

        public void SetTarget(Transform target)
        {
            _target = target;
            if (visual != null) visual.SetActive(_target != null);
        }

        private void LateUpdate()
        {
            if (_target == null || player == null || visual == null)
            {
                if (visual != null && visual.activeSelf) visual.SetActive(false);
                return;
            }

            // 거리 체크
            float distSqr = (player.position - _target.position).sqrMagnitude;
            bool shouldShow = distSqr > (hideDistance * hideDistance);
            
            if (visual.activeSelf != shouldShow)
            {
                visual.SetActive(shouldShow);
            }

            if (!shouldShow) return;

            // 방향 계산 (플레이어 중심 기준)
            Vector3 direction = (_target.position - player.position);
            direction.y = 0;

            if (direction.sqrMagnitude > 0.01f)
            {
                Vector3 dirNorm = direction.normalized;

                // 위치 업데이트: 플레이어로부터 Radius 만큼 타겟 방향으로 밀어내어 배치
                transform.position = player.position + (dirNorm * followRadius) + offset;

                // 회전 업데이트: 타겟을 바라보도록 설정 후 오프셋 적용
                Quaternion baseRotation = Quaternion.LookRotation(dirNorm);
                Quaternion targetRotation = baseRotation * Quaternion.Euler(rotationOffset);
                
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed * 2f);
            }
        }
    }
}
