using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MIKUFramework.IOC
{
    public class MIKUIoC
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        // 诊断：记录每个键对应的注册来源类型列表，以及覆盖告警去重
        private readonly Dictionary<Type, List<Type>> _keySources = new Dictionary<Type, List<Type>>();
        private readonly HashSet<Type> _keysOverwritten = new HashSet<Type>();

        private void RegisterKey(Type key, object instance, Type componentType)
        {
            if (key == null || instance == null || componentType == null) return;
            if (_services.TryGetValue(key, out var existing) && existing != null && existing.GetType() != componentType)
            {
                if (_keysOverwritten.Add(key))
                {
                    Debug.LogError(
                        $"[IOC] 键 {key.FullName} 被覆盖: {existing.GetType().FullName} -> {componentType.FullName}");
                }
            }

            _services[key] = instance;
            if (!_keySources.TryGetValue(key, out var list))
            {
                list = new List<Type>();
                _keySources[key] = list;
            }

            if (!list.Contains(componentType))
            {
                list.Add(componentType);
            }
        }

        private void RunDiagnostics()
        {
#if !UNITY_EDITOR
            // 非编辑器构建不运行诊断以避免不必要的性能开销
            return;
#endif
            // 检测：同一键来源于多个组件类型时给出告警
            foreach (var kv in _keySources)
            {
                var types = kv.Value;
                if (types != null && types.Count > 1)
                {
                    var joined = string.Join(", ", types.Select(t => t.FullName));
                    var final = _services.TryGetValue(kv.Key, out var value) ? value?.GetType().FullName : "<null>";
                    Debug.LogError($"[IOC] 键 {kv.Key.FullName} 绑定了多个组件类型: {joined}。最终使用: {final}");
                }
            }

            // 检测：注入覆盖率（标注了 [Autowired] 但没有可用服务键）
            foreach (var instance in _services.Values.Distinct())
            {
                var type = instance.GetType();
                var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.GetCustomAttributes(typeof(AutowiredAttribute), true).Length > 0);

                foreach (var field in fields)
                {
                    var serviceType = field.FieldType;
                    if (!_services.ContainsKey(serviceType))
                    {
                        Debug.LogError(
                            $"[IOC] 未找到可注入服务: {type.FullName}.{field.Name} 类型 {serviceType.FullName}。考虑在实现类上使用 [Component] RegisterAs 或调整 RegisterSelf/Interfaces/Naming。");
                    }
                }
            }

            // 额外检测：全域扫描所有类型的 [Autowired] 字段，定位潜在缺失键（无需等待具体注入点）
            var requestedTypes = new HashSet<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] asmTypes;
                try
                {
                    asmTypes = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    asmTypes = ex.Types.Where(t => t != null).ToArray();
                }

                foreach (var t in asmTypes)
                {
                    var fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                        .Where(f => f.GetCustomAttributes(typeof(AutowiredAttribute), true).Length > 0);
                    foreach (var f in fields)
                    {
                        if (f.FieldType != null)
                        {
                            requestedTypes.Add(f.FieldType);
                        }
                    }
                }
            }

            foreach (var serviceType in requestedTypes)
            {
                if (!_services.ContainsKey(serviceType))
                {
                    // 查找已注册实例中可兼容的实现候选（可能因命名约定或开关未注册为该键）
                    var candidateTypes = _services.Values.Distinct()
                        .Select(v => v?.GetType())
                        .Where(ct => ct != null && serviceType.IsAssignableFrom(ct))
                        .Distinct()
                        .ToList();

                    if (candidateTypes.Count > 0)
                    {
                        var candidatesText = string.Join(", ", candidateTypes.Select(ct => ct.FullName));
                        Debug.LogError(
                            $"[IOC] 未注册键 {serviceType.FullName}，但存在实现候选: {candidatesText}。请在实现类上使用 [Component(typeof({serviceType.Name}))] 或开启 RegisterInterfaces。");
                    }
                    else
                    {
                        Debug.LogError($"[IOC] 未注册键 {serviceType.FullName}（未发现实现候选）。请确保存在标注 [Component] 的实现并正确注册。");
                    }
                }
            }
        }

        public void DumpRegistry()
        {
            foreach (var kv in _services)
            {
                Debug.Log($"[IOC] {kv.Key.FullName} -> {kv.Value?.GetType().FullName}");
            }
        }

        public MIKUIoC()
        {
            // 扫描所有程序集中打了Component特性的类
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttributes(typeof(ComponentAttribute), true).Length > 0).ToList();

            // 先进行实例化（包括构造函数的依赖注入）
            while (types.Count > 0)
            {
                for (var i = 0; i < types.Count; i++)
                {
                    var type = types[i];
                    // 获取构造函数
                    var constructors = type.GetConstructors();
                    // 实例
                    object instance = null;

                    // 遍历构造函数，找到可以实例化的构造函数
                    foreach (var constructor in constructors)
                    {
                        // 获取构造函数的参数
                        var parameters = constructor.GetParameters();
                        // 构造函数的参数实例
                        var parameterInstances = new object[parameters.Length];

                        for (var j = 0; j < parameters.Length; j++)
                        {
                            var parameterType = parameters[j].ParameterType;
                            // 如果IoC容器中有这个参数的实例，就注入
                            if (_services.TryGetValue(parameterType, out var parameterInstance))
                            {
                                parameterInstances[j] = parameterInstance;
                            }
                            else
                            {
                                break;
                            }
                        }

                        // 如果有参数没有实例化，就跳过这个构造函数
                        if (parameterInstances.Contains(null)) continue;
                        instance = constructor.Invoke(parameterInstances);
                        break;
                    }

                    // 如果没有找到可以实例化的构造函数，就找无参构造函数
                    if (instance == null && type.GetConstructor(Type.EmptyTypes) != null)
                    {
                        instance = Activator.CreateInstance(type);
                    }

                    if (instance == null) continue;
                    // 注册进IoC容器（跟踪诊断）
                    RegisterKey(type, instance, type);

                    // 读取 [Component] 配置以控制注册键
                    var compAttr = type.GetCustomAttributes(typeof(ComponentAttribute), true)
                        .FirstOrDefault() as ComponentAttribute;

                    // 显式 RegisterAs：仅按指定类型注册（始终可选保留自身类型）
                    if (compAttr != null && compAttr.RegisterAs != null && compAttr.RegisterAs.Length > 0)
                    {
                        if (!compAttr.RegisterSelf)
                        {
                            // 用户关闭自身类型注册时移除自身键（诊断源仍保留）
                            _services.Remove(type);
                        }

                        foreach (var regType in compAttr.RegisterAs)
                        {
                            if (regType == null) continue;
                            if (regType.IsAssignableFrom(type))
                            {
                                RegisterKey(regType, instance, type);
                            }
                            else
                            {
                                Debug.LogError($"[IOC] {type.FullName} 未实现/继承 {regType.FullName}，忽略该 RegisterAs 类型。");
                            }
                        }
                    }
                    else
                    {
                        // 默认行为：根据开关注册接口/父类/命名约定接口
                        var interfaces = type.GetInterfaces();

                        bool registerInterfaces = compAttr?.RegisterInterfaces ?? true;
                        bool registerBaseTypes = compAttr?.RegisterBaseTypes ?? true;
                        bool namedOnly = compAttr?.RegisterNamedInterfaceOnly ?? false;

                        // 命名约定接口独立生效：当 namedOnly=true 时，仅注册 I+类名（不受 registerInterfaces 影响）
                        if (namedOnly)
                        {
                            var interfaceName = "I" + type.Name;
                            var matchedInterface = interfaces.FirstOrDefault(i => i.Name == interfaceName);
                            if (matchedInterface != null)
                            {
                                RegisterKey(matchedInterface, instance, type);
                            }
                        }
                        else if (registerInterfaces)
                        {
                            foreach (var @interface in interfaces)
                            {
                                RegisterKey(@interface, instance, type);
                            }
                        }

                        if (registerBaseTypes)
                        {
                            var baseType = type.BaseType;
                            if (baseType != null)
                            {
                                RegisterKey(baseType, instance, type);
                            }
                        }
                    }

                    // 从待注册列表中移除
                    types.RemoveAt(i);
                    i--;
                }
            }

            // 运行非侵入式诊断（仅日志，不改变行为）
            RunDiagnostics();

            // 开始进行字段的依赖注入（按实例类型遍历，避免 RegisterSelf=false 时漏注入）
            foreach (var instance in _services.Values.Distinct())
            {
                var type = instance.GetType();
                var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.GetCustomAttributes(typeof(AutowiredAttribute), true).Length > 0);

                foreach (var field in fields)
                {
                    var serviceType = field.FieldType;
                    if (_services.TryGetValue(serviceType, out var value))
                    {
                        field.SetValue(instance, value);
                    }
                    else
                    {
                        throw new Exception($"No service of type {serviceType} found for autowiring");
                    }
                }
            }
        }

        public T GetBean<T>()
        {
            return (T)_services[typeof(T)];
        }

        /// <summary>
        /// 这个方法一般用于找到所以正在运行的MonoBehaviour，然后进行字段的依赖注入
        /// </summary>
        /// <param name="instance">MonoBehaviour实例</param>
        /// <exception cref="Exception">没有找到对应的实例</exception>
        public void Inject(object instance)
        {
            var current = instance.GetType();
            while (current != null)
            {
                var fields = current.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(f => f.GetCustomAttributes(typeof(AutowiredAttribute), true).Length > 0);

                foreach (var field in fields)
                {
                    var serviceType = field.FieldType;
                    if (_services.TryGetValue(serviceType, out var value))
                    {
                        field.SetValue(instance, value);
                    }
                    else
                    {
                        throw new Exception($"No service of type {serviceType} found for autowiring");
                    }
                }

                current = current.BaseType;
            }
        }
    }
}