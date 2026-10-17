using System;

namespace Yuki.HotUpdate
{
    /// <summary>热更程序集内 RuntimeInitializeOnLoadMethod 的同阶段执行顺序。</summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class HotfixInitOrderAttribute : Attribute
    {
        public int Order { get; }
        public HotfixInitOrderAttribute(int order) => Order = order;
    }
}
