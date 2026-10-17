using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class UGUIBindingHierarchy
{
    private static readonly Dictionary<int, string> Badges = new Dictionary<int, string>();
    private static bool dirty = true;

    static UGUIBindingHierarchy()
    {
        EditorApplication.hierarchyWindowItemOnGUI += Draw;
        EditorApplication.hierarchyChanged += Invalidate;
        EditorApplication.projectChanged += Invalidate;
        Undo.undoRedoPerformed += Invalidate;
        UGUIBindingAccess.Changed += _ => Invalidate();
    }

    private static void Invalidate()
    {
        dirty = true;
        if (!Application.isBatchMode) EditorApplication.RepaintHierarchyWindow();
    }

    private static void Refresh()
    {
        dirty = false;
        Badges.Clear();
        var roots = new HashSet<GameObject>(Resources.FindObjectsOfTypeAll<BasePanel>()
            .Where(panel => panel != null && panel.gameObject.scene.IsValid()).Select(panel => panel.gameObject));
        foreach (string guid in AssetDatabase.FindAssets("t:UGUIScriptGeneratorBindings"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<UGUIScriptGeneratorBindings>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset == null) continue;
            var root = UGUIBindingAccess.ResolveRoot(asset);
            if (root != null) roots.Add(root);
        }
        foreach (var root in roots)
        {
            var settings = UGUIBindingAccess.SettingsFor(root);
            if (settings == null) continue;
            foreach (var entry in settings.entries.Where(entry => entry.active))
            {
                var target = UGUIBindingAccess.ResolveTarget(root, entry);
                if (target == null) continue;
                string events = string.Join(", ", entry.events.Where(e => e.enabled).Select(e => e.eventName));
                string tooltip = entry.fieldName + (events.Length > 0 ? "\n监听：" + events : "\n仅引用");
                if (Badges.TryGetValue(target.GetInstanceID(), out var previous)) tooltip = previous + "\n" + tooltip;
                Badges[target.GetInstanceID()] = tooltip;
            }
        }
    }

    private static void Draw(int instanceId, Rect row)
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (Event.current.type != EventType.Repaint && Event.current.type != EventType.MouseDown) return;
        if (dirty) Refresh();
        if (!Badges.TryGetValue(instanceId, out var tooltip)) return;
        var badge = new Rect(row.xMax - 30, row.y, 28, row.height);
        var previousColor = GUI.contentColor;
        GUI.contentColor = new Color(0.4f, 0.8f, 1f);
        GUI.Label(badge, new GUIContent("UI", tooltip + "\n选中对象后可在 Inspector 顶部直接编辑。"), EditorStyles.miniBoldLabel);
        GUI.contentColor = previousColor;
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && badge.Contains(Event.current.mousePosition))
        {
            Selection.activeObject = EditorUtility.InstanceIDToObject(instanceId);
            Event.current.Use();
        }
    }
}
