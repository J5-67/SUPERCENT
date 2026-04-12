using UnityEngine;

namespace Supercent.UI
{
    public class FloatingArrow : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.5f;
        [SerializeField] private Vector3 rotateSpeed = new Vector3(0, 50, 0);

        private Vector3 _startPos;

        private void Start()
        {
            _startPos = transform.localPosition;
        }

        private void Update()
        {
            // 상하 부메랑 움직임
            float newY = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = new Vector3(_startPos.x, newY, _startPos.z);

            // 회전 연출 (필요 시)
            transform.Rotate(rotateSpeed * Time.deltaTime);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
