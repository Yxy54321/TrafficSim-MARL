using UnityEngine;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    [SerializeField] private GameObject tooltipPanel;      // 提示框Panel
    [SerializeField] private TextMeshProUGUI tooltipText;  // 提示文本组件
    [SerializeField] private Camera camera;  // 提示文本组件

    private RectTransform rectTransform;
    private Vector2 offset = new Vector2(10, 10);         // 鼠标偏移量
    private bool isVisible = true;   // 是否显示

    public string tip1 = "cllick on the road to select a target place";
    public string tip2 = "cllick on the road to select a vehical start position";


    private void Start()
    {

        tooltipPanel.SetActive(isVisible);
        rectTransform = tooltipPanel.GetComponent<RectTransform>();


        // 同时禁用文本的射线检测
        if (tooltipText != null)
        {
            tooltipText.raycastTarget = false;
        }

        SetTooltipText(tip1);
    }

    private void Update()
    {
        if (!isVisible) return;

        // 获取鼠标位置（屏幕坐标）
        Vector2 mousePos = Input.mousePosition;

        // 将屏幕坐标转换为世界坐标
        Vector3 worldMousePos = camera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));

        Vector2 panelSize = rectTransform.sizeDelta;

        // 调整偏移量（World Space下可能需要不同的偏移计算）
        Vector3 offset = new Vector3(panelSize.x / 2, -panelSize.y, 0);

        // 设置提示框位置
        rectTransform.position = worldMousePos + offset;
    }

    public void SetTooltipText(string text)
    {
        tooltipText.text = text;
    }

    // 设置是否显示
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        tooltipPanel.SetActive(visible);
    }

    // 切换显示状态
    public void ToggleVisible()
    {
        SetVisible(!isVisible);
    }

    // 获取当前显示状态
    public bool IsVisible()
    {
        return isVisible;
    }
}