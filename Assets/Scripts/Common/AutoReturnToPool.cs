using System.Threading.Tasks;
using UnityEngine;

namespace Scripts.Common
{
    public class AutoReturnToPool : MonoBehaviour
    {
        [SerializeField] private float timeToReturnToPool = 1f;

        private async void Awake()
        {
            await Task.Delay(Mathf.RoundToInt(timeToReturnToPool * 1000));
            ObjectPool.Instance.Return(gameObject);
        }
    }
}
