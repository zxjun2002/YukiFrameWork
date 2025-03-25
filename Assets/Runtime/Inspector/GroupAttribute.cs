using System;
using UnityEngine;

/// <summary>
/// 标记一个分组的开始
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class GroupDropdownStartAttribute : PropertyAttribute
{
    public string GroupName;
    public GroupDropdownStartAttribute(string groupName)
    {
        GroupName = groupName;
    }
}

/// <summary>
/// 标记一个分组的结束
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class GroupDropdownEndAttribute : PropertyAttribute
{
    public string GroupName;
    public GroupDropdownEndAttribute(string groupName)
    {
        GroupName = groupName;
    }
}