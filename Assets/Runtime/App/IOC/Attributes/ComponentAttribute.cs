using System;

namespace MIKUFramework.IOC
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ComponentAttribute : Attribute
    {
        /// <summary>
        /// 指定该组件在 IoC 中注册为哪些类型（接口/父类/具体类型）。
        /// 若提供，则仅注册这些类型（始终包含自身类型除非显式关闭 <see cref="RegisterSelf"/>）。
        /// </summary>
        public Type[] RegisterAs { get; }

        /// <summary>
        /// 是否注册自身类型键（默认 true）。
        /// </summary>
        public bool RegisterSelf { get; set; } = true;

        /// <summary>
        /// 是否自动注册其实现的全部接口（默认 true）。
        /// 当 <see cref="RegisterAs"/> 提供时，忽略该设置。
        /// </summary>
        public bool RegisterInterfaces { get; set; } = false;

        /// <summary>
        /// 是否自动注册其直接父类（默认 true）。
        /// 当 <see cref="RegisterAs"/> 提供时，忽略该设置。
        /// </summary>
        public bool RegisterBaseTypes { get; set; } = false;

        /// <summary>
        /// 是否仅注册命名约定接口（I + 类名）。
        /// 当为 true 时，总是仅注册命名约定接口（若存在），不受 <see cref="RegisterInterfaces"/> 开关影响；
        /// 当提供 <see cref="RegisterAs"/> 时，该设置被忽略。
        /// </summary>
        public bool RegisterNamedInterfaceOnly { get; set; } = true;

        public ComponentAttribute(params Type[] registerAs)
        {
            RegisterAs = registerAs ?? Array.Empty<Type>();
        }
    }
}