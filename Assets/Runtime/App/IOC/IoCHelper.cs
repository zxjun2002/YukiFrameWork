using System.Reflection;
using UnityEngine;

namespace MIKUFramework.IOC
{
    public static class IoCHelper
    {
        public static MIKUIoC Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            Instance?.Dispose();
            Instance = null;
        }

        // 必须在热更程序集加载完成之后调用，否则业务服务不会被注册。
        public static void Initialize(params Assembly[] assemblies)
        {
            if (Instance != null) return;
            Instance = new MIKUIoC(new UnityIocLogger(), assemblies.Length == 0 ? null : assemblies);
        }
    }
}
