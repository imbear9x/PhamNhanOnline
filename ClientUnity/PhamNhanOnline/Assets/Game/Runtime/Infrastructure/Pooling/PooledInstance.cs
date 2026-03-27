using UnityEngine;

namespace PhamNhanOnline.Client.Infrastructure.Pooling
{
    [DisallowMultipleComponent]
    public sealed class PooledInstance : MonoBehaviour
    {
        private ClientPoolService.PrefabPool ownerPool;

        internal void Bind(ClientPoolService.PrefabPool pool)
        {
            ownerPool = pool;
        }

        public void Release()
        {
            if (ownerPool == null)
            {
                Destroy(gameObject);
                return;
            }

            ownerPool.Release(this);
        }
    }
}
