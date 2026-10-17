using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UINotchAdaptive), true)]
public class UINotchAdaptiveEditor : Editor {

    UINotchAdaptive uiNotchAdaptive;

    UINotchAdaptive.AdaptiveMode[] enumValues;
    string[] displayNames;
    bool showDropdown = false;

    GUIStyle chooseModeGuiStyle;

    void Awake() {
        this.uiNotchAdaptive = this.target as UINotchAdaptive;
        enumValues = (UINotchAdaptive.AdaptiveMode[])Enum.GetValues(typeof(UINotchAdaptive.AdaptiveMode));
        displayNames = new string[] { "None", "整体-向下移动", "整体-向上移动", "下边缘-向下拉伸", "上边缘-向上拉伸", "下边缘-向上缩短", "上边缘-向下缩短", "上下边缘-向内部缩短", "上下边缘-向外部拉伸" };
    }

    void OnEnable() {
        this.chooseModeGuiStyle = new GUIStyle() { alignment = TextAnchor.MiddleCenter };
        this.chooseModeGuiStyle.normal.textColor = Color.white;
    }

    public override void OnInspectorGUI() {

        using (new GUILayout.VerticalScope(GUI.skin.box)) {
            GUILayout.Label("向下移动、向下拉伸、向下缩短，对应的距离是：顶部刘海屏非安全区域的高度");
            GUILayout.Label("向上移动、向上拉伸，向上缩短，对应的距离是：底部圆角非安全区域的高度");

            if (Application.isPlaying)
            {
                var disTop = uiNotchAdaptive.disTop;
                var disBottom = uiNotchAdaptive.disBottom;
                GUILayout.Label($"顶部刘海屏高度：{disTop}");
                GUILayout.Label($"底部刘海屏高度：{disBottom}");
            }
        }
        GUILayout.Space(30);

        // 按钮控制下拉框显示/隐藏
        if (GUILayout.Button("自适应模式: " + displayNames[(int)uiNotchAdaptive.mode])) {
            showDropdown = !showDropdown;
        }

        // 显示下拉选项
        if (showDropdown) {
            using (new GUILayout.VerticalScope(GUI.skin.box)) {
                for (int i = 0; i < enumValues.Length; i++) {
                    using (new GUILayout.HorizontalScope()) {
                        GUILayout.Space(100);
                        if ((int)this.uiNotchAdaptive.mode == i) {
                            GUILayout.Label(displayNames[i], this.chooseModeGuiStyle);
                        } else {
                            if (GUILayout.Button(displayNames[i])) {
                                this.uiNotchAdaptive.mode = enumValues[i];
                                EditorUtility.SetDirty(this.uiNotchAdaptive);
                                showDropdown = false; // 关闭下拉框
                            }
                        }
                        GUILayout.Space(100);
                    }
                }
            }
        }

        if (Application.isPlaying) {
            GUILayout.Space(20);
            GUILayout.Label("修改锚点、轴心点需要重启");
            GUILayout.Space(5);
            if (GUILayout.Button("立即生效（运行时）", GUILayout.Height(30))) {
                this.uiNotchAdaptive.ApplicationAdaptive(this.uiNotchAdaptive.anchoredPosition, this.uiNotchAdaptive.sizeDelta, this.uiNotchAdaptive.anchorMax, this.uiNotchAdaptive.anchorMin, this.uiNotchAdaptive.offsetMax, this.uiNotchAdaptive.offsetMin);
            }
        }

    }
}