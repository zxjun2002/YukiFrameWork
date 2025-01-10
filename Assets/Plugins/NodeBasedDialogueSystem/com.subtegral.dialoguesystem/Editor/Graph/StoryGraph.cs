using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Subtegral.DialogueSystem.DataContainers;

namespace Subtegral.DialogueSystem.Editor
{
    public class StoryGraph : EditorWindow
    {
        private string _fileName = "New Narrative";

        private StoryGraphView _graphView;
        private DialogueContainer _dialogueContainer;
        private bool _hasUnsavedChanges = false; // 标记是否有未保存的更改

        [MenuItem("Graph/Narrative Graph")]
        public static void CreateGraphViewWindow()
        {
            var window = GetWindow<StoryGraph>();
            window.titleContent = new GUIContent("Narrative Graph");
        }

        private void ConstructGraphView()
        {
            _graphView = new StoryGraphView(this)
            {
                name = "Narrative Graph",
            };
            _graphView.StretchToParentSize();

            // 监听节点或边缘的增删操作
            _graphView.graphViewChanged = changes =>
            {
                if (changes.elementsToRemove != null && changes.elementsToRemove.Any())
                {
                    MarkAsDirty();
                }

                if (changes.edgesToCreate != null && changes.edgesToCreate.Any())
                {
                    MarkAsDirty();
                }

                if (changes.movedElements != null && changes.movedElements.Any())
                {
                    MarkAsDirty();
                }

                return changes;
            };

            rootVisualElement.Add(_graphView);
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();

            var fileNameTextField = new TextField("File Name:");
            fileNameTextField.SetValueWithoutNotify(_fileName);
            fileNameTextField.MarkDirtyRepaint();
            fileNameTextField.RegisterValueChangedCallback(evt => _fileName = evt.newValue);
            toolbar.Add(fileNameTextField);

            toolbar.Add(new Button(() =>
            {
                RequestDataOperation(true);
                _hasUnsavedChanges = false; // 保存后重置标记
            }) { text = "Save Data" });
            
            toolbar.Add(new Button(() => RequestDataOperation(false)) {text = "Load Data"});
            
            // 新增的按钮：选择文件加载
            toolbar.Add(new Button(OpenFileAndLoadData) {text = "Select File and Load Data"});
            // toolbar.Add(new Button(() => _graphView.CreateNewDialogueNode("Dialogue Node")) {text = "New Node",});
            rootVisualElement.Add(toolbar);
        }
        
        private void OpenFileAndLoadData()
        {
            // 打开文件对话框，让用户选择文件
            string path = EditorUtility.OpenFilePanel("Select Dialogue Data File", "Assets/Resources", "asset");

            if (!string.IsNullOrEmpty(path))
            {
                // 将路径转换为相对路径
                string relativePath = path.Replace(Application.dataPath, "Assets");
                DialogueContainer container = AssetDatabase.LoadAssetAtPath<DialogueContainer>(relativePath);

                if (container != null)
                {
                    // 将文件名设置为相对路径中的文件名（无后缀）
                    _fileName = Path.GetFileNameWithoutExtension(path);
                    var saveUtility = GraphSaveUtility.GetInstance(_graphView);
                    saveUtility.LoadNarrative(_fileName); // 加载数据

                    titleContent.text = _fileName; // 更新标题
                    _hasUnsavedChanges = false; // 加载后没有未保存更改
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "The selected file is not a valid DialogueContainer.", "OK");
                }
            }
        }

        private void RequestDataOperation(bool save)
        {
            if (!string.IsNullOrEmpty(_fileName))
            {
                var saveUtility = GraphSaveUtility.GetInstance(_graphView);
                if (save)
                {
                    saveUtility.SaveGraph(_fileName);
                    _hasUnsavedChanges = false; // 保存后重置标记
                    titleContent.text = _fileName; // 移除标题上的星号
                }
                else
                {
                    saveUtility.LoadNarrative(_fileName);
                    titleContent.text = _fileName; // 更新标题
                    _hasUnsavedChanges = false; // 加载后没有未保存更改
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid File name", "Please Enter a valid filename", "OK");
            }
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
            GenerateMiniMap();
            GenerateBlackBoard();
        }

        private void GenerateMiniMap()
        {
            var miniMap = new MiniMap {anchored = true};
            var cords = _graphView.contentViewContainer.WorldToLocal(new Vector2(this.maxSize.x - 10, 30));
            miniMap.SetPosition(new Rect(cords.x, cords.y, 200, 140));
            _graphView.Add(miniMap);
        }

        private void GenerateBlackBoard()
        {
            var blackboard = new Blackboard(_graphView);
            blackboard.Add(new BlackboardSection {title = "Exposed Variables"});
            blackboard.addItemRequested = _blackboard =>
            {
                _graphView.AddPropertyToBlackBoard(ExposedProperty.CreateInstance(), false);
            };
            blackboard.editTextRequested = (_blackboard, element, newValue) =>
            {
                var oldPropertyName = ((BlackboardField) element).text;
                if (_graphView.ExposedProperties.Any(x => x.PropertyName == newValue))
                {
                    EditorUtility.DisplayDialog("Error", "This property name already exists, please chose another one.",
                        "OK");
                    return;
                }

                var targetIndex = _graphView.ExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
                _graphView.ExposedProperties[targetIndex].PropertyName = newValue;
                ((BlackboardField) element).text = newValue;
            };
            blackboard.SetPosition(new Rect(10,30,200,300));
            _graphView.Add(blackboard);
            _graphView.Blackboard = blackboard;
        }

        private void OnDisable()
        {
            if (_hasUnsavedChanges) // 检查是否有未保存的更改
            {
                bool saveChanges = EditorUtility.DisplayDialog(
                    "Unsaved Changes",
                    "You have unsaved changes. Would you like to save before exiting?",
                    "Save", "Don't Save");

                if (saveChanges)
                {
                    RequestDataOperation(true); // 调用保存逻辑
                }
            }

            rootVisualElement.Remove(_graphView);
        }
        
        public void MarkAsDirty()
        {
            _hasUnsavedChanges = true; // 标记为有未保存的更改
            titleContent.text = $"{(string.IsNullOrEmpty(_fileName) ? "Narrative Graph" : _fileName)}*";
        }
    }
}