using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Supercent.Player;

namespace Supercent.Field
{
    public class ResourceZone : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float dropInterval = 0.1f;
        [SerializeField] private Transform sellTarget; 
        [SerializeField] private float flyDuration = 0.3f;
        [SerializeField] private float verticalSpacing = 0.2f; // 위로 쌓이는 간격
        [SerializeField] private float columnSpacing = 0.6f;   // 두 줄 사이의 간격
        [SerializeField] private int columns = 2;           // 쌓을 열의 개수

        private List<Transform> _depositedItems = new List<Transform>();
        private Coroutine _sellCoroutine;
        private PlayerStackHandler _currentPlayer;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<PlayerStackHandler>(out var handler))
            {
                _currentPlayer = handler;
                if (_sellCoroutine == null) _sellCoroutine = StartCoroutine(SellRoutine());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<PlayerStackHandler>(out var handler))
            {
                if (_currentPlayer == handler)
                {
                    _currentPlayer = null;
                    if (_sellCoroutine != null)
                    {
                        StopCoroutine(_sellCoroutine);
                        _sellCoroutine = null;
                    }
                }
            }
        }

        public Transform ExtractItem()
        {
            if (_depositedItems.Count == 0) return null;

            int lastIndex = _depositedItems.Count - 1;
            Transform item = _depositedItems[lastIndex];
            _depositedItems.RemoveAt(lastIndex);
            return item;
        }

        private IEnumerator SellRoutine()
        {
            while (_currentPlayer != null)
            {
                GameObject item = _currentPlayer.PopFromStack();
                
                if (item != null)
                {
                    int index = _depositedItems.Count;
                    int col = index % columns;      
                    int row = index / columns;      

                    Vector3 offset = new Vector3(col * columnSpacing, row * verticalSpacing, 0);
                    Vector3 targetPos = sellTarget.position + offset;
                    
                    Transform itemTrm = item.transform;
                    _depositedItems.Add(itemTrm);
                    StartCoroutine(FlyToTarget(itemTrm, targetPos));
                    
                    yield return new WaitForSeconds(dropInterval);
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

        private IEnumerator FlyToTarget(Transform item, Vector3 targetPos)
        {
            Vector3 startPos = item.position;
            Quaternion startRot = item.rotation;
            float elapsed = 0f;

            // 소유권 변경 (플레이어 스택에서 분리)
            item.SetParent(transform);

            while (elapsed < flyDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flyDuration;
                
                // 포물선을 그리며 부드럽게 적재 위치로 이동
                item.position = Vector3.Lerp(startPos, targetPos, t) + (Vector3.up * Mathf.Sin(t * Mathf.PI) * 1f);
                item.rotation = Quaternion.Slerp(startRot, transform.rotation, t);
                
                yield return null;
            }

            item.position = targetPos;
        }
    }
}
