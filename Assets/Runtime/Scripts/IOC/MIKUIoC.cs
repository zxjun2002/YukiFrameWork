using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MIKUFramework.IOC
{
    public class MIKUIoC
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

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
                    // 注册进IoC容器
                    _services[type] = instance;

                    // 观察这个类是否实现了接口
                    var interfaces = type.GetInterfaces();
                    //如果有也要把全部接口作为key注册进IoC容器
                    //foreach (var @interface in interfaces)
                    //{
                    //    _services[@interface] = instance;
                    //}
                    // 如果有也要把(例如 A -> IA查找是否有匹配的接口)作为key注册进IoC容器
                    var interfaceName = "I" + type.Name; 
                    var matchedInterface = interfaces.FirstOrDefault(i => i.Name == interfaceName);
                    //Debug.LogWarning(type+"=====>"+matchedInterface);
                    if (matchedInterface != null)
                        _services[matchedInterface] = instance; // 只注册匹配的接口

                    // 观察这个类是否继承了父类，如果有也要把父类作为key注册进IoC容器
                    var baseType = type.BaseType;
                    if (baseType != null)
                    {
                        _services[baseType] = instance;
                    }

                    // 从待注册列表中移除
                    types.RemoveAt(i);
                    i--;
                }
            }

            // 开始进行字段的依赖注入
            foreach (var type in _services.Keys.ToList())
            {
                var instance = _services[type];
                var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.GetCustomAttributes(typeof(AutowiredAttribute), true).Length > 0);

                foreach (var field in fields)
                {
                    // 获取字段的类型
                    var serviceType = field.FieldType;
                    // 如果IoC容器中有这个类型的实例，就注入
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
            var type = instance.GetType();
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttributes(typeof(AutowiredAttribute), true).Length > 0);

            foreach (var field in fields)
            {
                // 获取字段的类型
                var serviceType = field.FieldType;
                // 如果IoC容器中有这个类型的实例，就注入
                if (_services.TryGetValue(serviceType, out var value))
                {
                    field.SetValue(instance, value);
                    //.LogWarning(type+"=====>"+serviceType);
                }
                else
                {
                    throw new Exception($"No service of type {serviceType} found for autowiring");
                }
            }
        }
    }
}
