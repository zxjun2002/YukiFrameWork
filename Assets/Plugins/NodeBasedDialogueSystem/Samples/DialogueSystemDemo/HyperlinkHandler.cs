using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HyperlinkHandler : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI textMeshPro;

    void Start()
    {
        if (textMeshPro == null)
            textMeshPro = GetComponent<TextMeshProUGUI>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 使用 PointerEventData 的点击位置
        Vector2 localMousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            textMeshPro.rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localMousePosition
        );

        // 检测点击位置是否在超链接上
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(textMeshPro, eventData.position, eventData.pressEventCamera);
        
        if (linkIndex != -1) // 如果点击了超链接
        {
            // 获取超链接信息
            TMP_LinkInfo linkInfo = textMeshPro.textInfo.linkInfo[linkIndex];
            string linkID = linkInfo.GetLinkID(); // 获取超链接的 ID

            // 根据 ID 处理逻辑
            HandleLink(linkID);
        }
    }

    private void HandleLink(string linkID)
    {
        switch (linkID)
        {
            case "help":
                Debug.Log("打开帮助页面");
                Application.OpenURL("https://www.baidu.com");
                // 执行跳转或其他逻辑
                break;

            case "home":
                Debug.Log("返回主页");
                // 执行跳转或其他逻辑
                break;

            default:
                Debug.Log($"未定义的链接：{linkID}");
                break;
        }
    }
}