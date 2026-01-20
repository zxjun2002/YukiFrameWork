using UnityEngine;

namespace MIKUFramework.IOC
{
    public abstract class InjectableMonoBehaviour : MonoBehaviour
    {
        /// <summary>
        /// 在Start方法中注入依赖
        /// </summary>
        private void Start()
        {
            IoCHelper.Instance.Inject(this);
            OnStart();
        }

        /// <summary>
        /// 自己的Start方法
        /// </summary>
        protected abstract void OnStart();
    }
}