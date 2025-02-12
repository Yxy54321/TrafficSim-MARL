using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class RawImageClickCoordinates : MonoBehaviour, IPointerClickHandler
{
    public static RawImageClickCoordinates Instance { get; private set; }
    public RoadLaneBoundsManager boundsManager;
    public RawImage rawImage;
    // 记录选中点坐标的数组
    public List<Vector3> selectedPoints = new List<Vector3>();
    private TooltipManager TTMng;
    // 添加红点相关变量
    [SerializeField] private GameObject pointMarkPrefab; // 红点预制体
    private List<GameObject> pointMarks = new List<GameObject>(); // 记录所有红点标记

    void Awake()
    {
        TTMng = FindObjectOfType<TooltipManager>();
        boundsManager = FindObjectOfType<RoadLaneBoundsManager>();
        // 使用DontDestroyOnLoad保证跨场景
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        // 如果没有在Inspector中拖拽，尝试获取
        if (rawImage == null)
        {
            rawImage = GetComponent<RawImage>();
        }
        // 确保Raycast Target开启
        if (rawImage != null)
        {
            rawImage.raycastTarget = true;
        }

        // 如果没有指定红点预制体，创建一个默认的
        if (pointMarkPrefab == null)
        {
            CreateDefaultPointMarkPrefab();
        }
    }

    private void CreateDefaultPointMarkPrefab()
    {
        // 创建一个默认的红点预制体
        GameObject prefab = new GameObject("PointMark");
        Image image = prefab.AddComponent<Image>();
        image.color = Color.red;
        RectTransform rect = prefab.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(10, 10); // 设置红点大小
        pointMarkPrefab = prefab;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (rawImage == null) return;
        // 将屏幕坐标转换为RectTransform的本地坐标
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImage.rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );
        // 获取RawImage的Rect
        Rect rect = rawImage.rectTransform.rect;
        // 计算归一化坐标
        Vector2 normalizedCoord = new Vector2(
            (localPoint.x - rect.x) / rect.width,
            (localPoint.y - rect.y) / rect.height
        );
        // 转换为像素坐标（500*500）
        Vector2 pixelCoord = new Vector2(
            normalizedCoord.x * 500,
            normalizedCoord.y * 500
        );
        // 计算实际坐标
        float x = (pixelCoord.x - 37.3f) * 0.577f;
        float z = (pixelCoord.y - 67.82f) * 0.577f;
        // 创建坐标点
        Vector3 worldPoint = new Vector3(x, 0, z);

        if (boundsManager.IsPointInAnyRoadLane(worldPoint))
        {
            //添加到选中点数组
            if (selectedPoints.Count == 0)
                TTMng.SetTooltipText(TTMng.tip2);
            
            selectedPoints.Add(worldPoint);
            CreatePointMark(localPoint); // 创建红点标记
            Debug.Log($"像素坐标: {pixelCoord}");
            Debug.Log($"实际坐标: {worldPoint}");
        }
        else
        {
            Debug.Log("无效点");
        }
    }

    // 创建红点标记
    private void CreatePointMark(Vector2 position)
    {
        GameObject pointMark = Instantiate(pointMarkPrefab, rawImage.transform);
        RectTransform rectTransform = pointMark.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        pointMarks.Add(pointMark);
    }

    // 清除所有选中点和标记
    public void ClearAllMarks()
    {
        // 清除所有红点标记
        foreach (var mark in pointMarks)
        {
            if (mark != null)
            {
                Destroy(mark);
            }
        }
        pointMarks.Clear();
        // 清除所有选中点
        selectedPoints.Clear();
        TTMng.SetTooltipText(TTMng.tip1);
    }

    // 清除所有选中点
    public void ClearSelectedPoints()
    {
        ClearAllMarks(); // 修改原有方法，调用新的清除方法
    }

    // 获取最后一个选中点
    public Vector3 GetLastSelectedPoint()
    {
        if (selectedPoints.Count > 0)
        {
            return selectedPoints[selectedPoints.Count - 1];
        }
        return Vector3.zero;
    }

    private void OnDestroy()
    {
        ClearAllMarks();
    }
}