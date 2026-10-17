using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Yuki.HotUpdate
{
    public static class HotfixInitialization
    {
        public static void Invoke(IEnumerable<Assembly> assemblies, bool afterSceneLoad)
        {
            var methods = new List<(int phase, int order, MethodInfo method)>();
            foreach (var assembly in assemblies)
            foreach (var type in assembly.GetTypes())
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public |
                                                   BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                var attribute = method.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
                if (attribute == null || (attribute.loadType == RuntimeInitializeLoadType.AfterSceneLoad) != afterSceneLoad)
                    continue;
                if (type.ContainsGenericParameters || method.ContainsGenericParameters ||
                    method.ReturnType != typeof(void) || method.GetParameters().Length != 0)
                    throw new InvalidOperationException($"不支持的热更初始化方法：{type.FullName}.{method.Name}");
                int order = method.GetCustomAttribute<HotfixInitOrderAttribute>()?.Order ?? 0;
                methods.Add((GetPhase(attribute.loadType), order, method));
            }

            foreach (var item in methods.OrderBy(item => item.phase).ThenBy(item => item.order)
                         .ThenBy(item => item.method.DeclaringType.FullName + "." + item.method.Name, StringComparer.Ordinal))
            {
                try { item.method.Invoke(null, null); }
                catch (TargetInvocationException exception)
                {
                    throw new InvalidOperationException(
                        $"热更初始化失败：{item.method.DeclaringType.FullName}.{item.method.Name}", exception.InnerException);
                }
            }
        }

        private static int GetPhase(RuntimeInitializeLoadType phase)
        {
            switch (phase)
            {
                case RuntimeInitializeLoadType.SubsystemRegistration: return 0;
                case RuntimeInitializeLoadType.AfterAssembliesLoaded: return 1;
                case RuntimeInitializeLoadType.BeforeSplashScreen: return 2;
                case RuntimeInitializeLoadType.BeforeSceneLoad: return 3;
                default: return 4;
            }
        }
    }
}
