using UnityEngine;

namespace Supercent.Utils
{
    /// <summary>
    /// 오브젝트가 항상 카메라를 향하게 만드는 빌보드 스크립트
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private Transform _mainCamTransform;

        private void Start()
        {
            if (Camera.main != null)
            {
                _mainCamTransform = Camera.main.transform;
            }
        }

        private void LateUpdate()
        {
            if (_mainCamTransform == null) return;

            // 카메라의 회전값만 따름 (UI가 뒤집히지 않게 처리)
            transform.rotation = _mainCamTransform.rotation;
        }
    }
}
