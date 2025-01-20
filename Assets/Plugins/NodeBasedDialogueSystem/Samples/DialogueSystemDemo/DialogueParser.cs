using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Subtegral.DialogueSystem.DataContainers;

namespace Subtegral.DialogueSystem.Runtime
{
    public class DialogueParser : MonoBehaviour
    {
        // 引用的对话容器，包含对话节点和链接信息。
        [SerializeField] private DialogueContainer dialogue;

        // 用于显示对话内容的 TextMeshPro UI 元素。
        [SerializeField] private TextMeshProUGUI dialogueText;

        // 用于显示 CommentBlock 标题的 TextMeshPro UI 元素。
        [SerializeField] private TextMeshProUGUI titleText;

        // 用于生成选项按钮的预制体。
        [SerializeField] private Button choicePrefab;

        // 选项按钮的容器，按钮会作为其子对象生成。
        [SerializeField] private Transform buttonContainer;

        private void Start()
        {
            dialogue.SetValue("Name","蔡徐坤");
            // 获取对话的入口节点（Entrypoint），即对话的开始点。
            var narrativeData = dialogue.NodeLinks.First(); // 找到第一个链接，通常是入口节点。
            // 进入指定节点的对话内容。
            ProceedToNarrative(narrativeData.TargetNodeGUID);
        }

        /// <summary>
        /// 处理对话逻辑，更新对话内容并生成对应的选项按钮。
        /// </summary>
        /// <param name="narrativeDataGUID">当前对话节点的唯一标识符 (GUID)。</param>
        private void ProceedToNarrative(string narrativeDataGUID)
        {
            // 查找目标节点的对话文本。
            var text = dialogue.DialogueNodeData.Find(x => x.NodeGUID == narrativeDataGUID).DialogueText;

            // 更新标题，如果当前节点属于某个 CommentBlock。
            UpdateTitle(narrativeDataGUID);

            // 查找从当前节点出发的所有连接（即选项）。
            var choices = dialogue.NodeLinks.Where(x => x.BaseNodeGUID == narrativeDataGUID);

            // 更新对话文本（并替换其中的动态属性）。
            dialogueText.text = ProcessProperties(text);

            // 清除现有的按钮，防止之前的选项残留。
            var buttons = buttonContainer.GetComponentsInChildren<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Destroy(buttons[i].gameObject);
            }

            // 为每个选项生成一个按钮。
            foreach (var choice in choices)
            {
                // 实例化按钮预制体，并将其作为按钮容器的子对象。
                var button = Instantiate(choicePrefab, buttonContainer);

                // 设置按钮的文本，显示选项名称。
                button.GetComponentInChildren<Text>().text = ProcessProperties(choice.PortName);

                // 为按钮添加点击事件监听器，点击后跳转到目标节点。
                button.onClick.AddListener(() => ProceedToNarrative(choice.TargetNodeGUID));
            }
        }

        /// <summary>
        /// 更新标题，如果当前节点属于某个 CommentBlock。
        /// </summary>
        /// <param name="nodeGuid">当前节点的 GUID。</param>
        private void UpdateTitle(string nodeGuid)
        {
            // 查找包含该节点的 CommentBlock。
            var commentBlock = dialogue.CommentBlockData.FirstOrDefault(cb => cb.ChildNodes.Contains(nodeGuid));

            if (commentBlock != null)
            {
                // 如果找到对应的 CommentBlock，更新标题。
                titleText.text = commentBlock.Title;
            }
            else
            {
                // 如果没有对应的 CommentBlock，则清空标题或显示默认标题。
                titleText.text = "Default Title";
            }
        }

        /// <summary>
        /// 处理动态属性替换，将文本中的占位符替换为实际值。
        /// </summary>
        /// <param name="text">需要处理的文本内容。</param>
        /// <returns>替换后的文本。</returns>
        private string ProcessProperties(string text)
        {
            // 遍历对话中的所有暴露属性，将文本中的占位符替换为实际值。
            foreach (var exposedProperty in dialogue.ExposedProperties)
            {
                text = text.Replace($"[{exposedProperty.PropertyName}]", exposedProperty.PropertyValue);
            }
            return text;
        }
    }
}
