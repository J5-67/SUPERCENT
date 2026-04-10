using System.Collections;
using UnityEngine;
using Supercent.Systems;

namespace Supercent.Field
{
    public class Money : MonoBehaviour
    {
        [SerializeField] private int value = 100;
        public int Value => value;
        private bool _isCollected = false;

        public void PlayCollectEffect(Transform targetStackPos, System.Action onComplete)
        {
            if (_isCollected) return;
            _isCollected = true;
            
            // 이제 여기서는 사라지는 로직 대신, 스택 위치로 날아가는 연출만 수행할 수도 있고
            // 지금은 PlayerStackHandler가 Lerp로 위치를 잡아주고 있으므로 즉시 완료 처리만 합니다.
            onComplete?.Invoke();
        }
    }
}
