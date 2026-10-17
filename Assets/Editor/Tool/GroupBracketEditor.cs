using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// 通用的“括号式”分组编辑器：当某脚本没有专门的CustomEditor时，就会用到它。
/// 支持 [GroupDropdownStart("XXX")] ... [GroupDropdownEnd("XXX")]，并在Inspector中用下拉框把属性包起来。
/// </summary>
[CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
[CanEditMultipleObjects]
public class GroupDropdownEditor : Editor
{
    // 标识是否在某个分组里
    private bool _inGroup = false;
    private string _currentGroupName = "";
    // 收集分组内字段
    private List<SerializedProperty> _currentGroupProps = new List<SerializedProperty>();

    // 记录每个分组的折叠状态
    private static Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();

    /// <summary>
    /// 结束当前分组，并绘制下拉框
    /// </summary>
    private void FinalizeGroup()
    {
        if (!_inGroup)
        {
            return;
        }

        // 如果还没有记录这个分组的折叠状态，默认折叠
        if (!_foldoutStates.ContainsKey(_currentGroupName))
        {
            _foldoutStates[_currentGroupName] = false;
        }

        // 在一个box里绘制分组标题（折叠）和字段
        EditorGUILayout.BeginVertical("box");
        _foldoutStates[_currentGroupName] = EditorGUILayout.Foldout(
            _foldoutStates[_currentGroupName],
            _currentGroupName,
            true
        );

        // 如果展开，则绘制组内字段
        if (_foldoutStates[_currentGroupName])
        {
            EditorGUI.indentLevel++;
            foreach (var prop in _currentGroupProps)
            {
                EditorGUILayout.PropertyField(prop, true);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();

        // 清空当前分组信息
        _currentGroupProps.Clear();
        _inGroup = false;
        _currentGroupName = "";
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制脚本引用，一般只读
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Script", 
                MonoScript.FromMonoBehaviour((MonoBehaviour)target), 
                typeof(MonoBehaviour), 
                false
            );
        }

        // 遍历所有字段
        var iterator = serializedObject.GetIterator();
        if (iterator.NextVisible(true))
        {
            do
            {
                // 跳过脚本引用
                if (iterator.name == "m_Script")
                    continue;

                // 通过反射拿到字段
                FieldInfo fieldInfo = target.GetType()
                    .GetField(iterator.name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                if (fieldInfo != null)
                {
                    // 分组开始
                    var startAttr = (GroupDropdownStartAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(GroupDropdownStartAttribute));
                    if (startAttr != null)
                    {
                        // 若之前有未结束的分组，先结束
                        FinalizeGroup();

                        // 开启新分组
                        _inGroup = true;
                        _currentGroupName = startAttr.GroupName;
                        // 这个字段本身通常只是占位，不需要显示
                        continue;
                    }

                    // 分组结束
                    var endAttr = (GroupDropdownEndAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(GroupDropdownEndAttribute));
                    if (endAttr != null)
                    {
                        // 如果正处于分组，且组名匹配，则结束
                        if (_inGroup && endAttr.GroupName == _currentGroupName)
                        {
                            FinalizeGroup();
                        }
                        continue;
                    }
                }

                // 如果在分组里，就将字段加到当前分组的列表
                if (_inGroup)
                {
                    _currentGroupProps.Add(iterator.Copy());
                }
                else
                {
                    // 不在分组，就默认绘制
                    EditorGUILayout.PropertyField(iterator, true);
                }

            } while (iterator.NextVisible(false));
        }

        // 如果遍历结束还在组里，就收尾
        FinalizeGroup();

        serializedObject.ApplyModifiedProperties();
    }
}
