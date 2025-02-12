using UnityEngine;
using UnityEngine.UI;
using XCharts.Runtime;
using System.Collections.Generic;

public class RLRewardVisualizer : MonoBehaviour
{
    [SerializeField]
    private ScrollRect scrollRect;
    [SerializeField]
    private GameObject chartPrefab;
    [SerializeField]
    private int targetDataPoints = 20;
    [SerializeField]
    private Button compareButton; // 新增：比较按钮
    [SerializeField]
    private float chartHeight = 150f;
    private float currentChartHeight;

    // 新增：存储多轮模拟的数据
    private List<List<RewardRecord>> allSimulationsData = new List<List<RewardRecord>>();
    private List<Color> simulationColors = new List<Color> {
        Color.blue, Color.red, Color.green, Color.yellow, Color.cyan
    }; // 不同轮次使用不同颜色


    private List<RewardRecord> rewardHistory = new List<RewardRecord>();
    private float currentTime = 0f;
    private bool isRecording = false;
    private LineChart currentChart;
    private GameObject currentChartObj;
    private float recordInterval = 0.1f;
    private float nextRecordTime = 0f;

    private struct RewardRecord
    {
        public float time;
        public float reward;
        public RewardRecord(float t, float r)
        {
            time = t;
            reward = r;
        }
    }

    private void Start()
    {
        currentChartHeight=chartHeight;
        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect reference is missing!");
            return;
        }

        // 设置比较按钮的监听器
        if (compareButton != null)
        {
            compareButton.onClick.AddListener(CompareSimulations);
        }


        // 修改 VerticalLayoutGroup 设置
        VerticalLayoutGroup layoutGroup = scrollRect.content.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.spacing = 10;
            layoutGroup.childControlHeight = false;  // 禁用高度控制
            layoutGroup.childForceExpandHeight = false;  // 禁用强制展开
        }
        // 设置Content的布局
        ContentSizeFitter sizeFitter = scrollRect.content.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = scrollRect.content.gameObject.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    public void StartNewSimulation()
    {
        rewardHistory.Clear();
        currentTime = 0f;
        isRecording = true;
        nextRecordTime = 0f;

        CreateNewChart();
    }

    public void RecordReward(float reward)
    {
        if (!isRecording) return;

        currentTime += Time.deltaTime;

        // 只在达到记录间隔时添加数据点
        if (currentTime >= nextRecordTime)
        {
            RewardRecord record = new RewardRecord(currentTime, reward);
            rewardHistory.Add(record);

            if (currentChart != null)
            {
                currentChart.AddData(0, currentTime, reward);
                currentChart.RefreshChart();
            }

            // 更新下一个记录时间点
            nextRecordTime = currentTime + recordInterval;

            // 动态调整记录间隔，以保持大约20个数据点
            if (rewardHistory.Count > targetDataPoints)
            {
                ResampleData();
            }
        }
    }

    private void ResampleData()
    {
        if (rewardHistory.Count <= targetDataPoints) return;

        float totalTime = currentTime;
        recordInterval = totalTime / targetDataPoints;

        List<RewardRecord> newHistory = new List<RewardRecord>();
        float sampleTime = 0f;

        for (int i = 0; i < targetDataPoints; i++)
        {
            float targetTime = sampleTime;
            float nearestReward = GetInterpolatedReward(targetTime);

            newHistory.Add(new RewardRecord(targetTime, nearestReward));
            sampleTime += recordInterval;
        }

        rewardHistory = newHistory;

        if (currentChart != null)
        {
            currentChart.ClearData();
            foreach (var record in rewardHistory)
            {
                currentChart.AddData(0, record.time, record.reward);
            }
            currentChart.RefreshChart();
        }
    }

    private float GetInterpolatedReward(float targetTime)
    {
        int index = 0;
        while (index < rewardHistory.Count && rewardHistory[index].time < targetTime)
        {
            index++;
        }

        if (index == 0) return rewardHistory[0].reward;
        if (index >= rewardHistory.Count) return rewardHistory[rewardHistory.Count - 1].reward;

        RewardRecord before = rewardHistory[index - 1];
        RewardRecord after = rewardHistory[index];
        float t = (targetTime - before.time) / (after.time - before.time);
        return Mathf.Lerp(before.reward, after.reward, t);
    }

    private void CreateNewChart()
    {
        if (chartPrefab == null)
        {
            Debug.LogError("Chart prefab is missing!");
            return;
        }

        currentChartObj = Instantiate(chartPrefab, scrollRect.content);
        currentChart = currentChartObj.GetComponent<LineChart>();

        // 设置图表大小
        RectTransform chartRect = currentChartObj.GetComponent<RectTransform>();
        if (chartRect != null)
        {
            float scrollViewWidth = scrollRect.GetComponent<RectTransform>().rect.width;
            float targetWidth = scrollViewWidth - 40f;

            Debug.Log($"Setting chart height to: {chartHeight}"); // 添加调试日志

            // 强制设置布局
            LayoutElement layoutElement = currentChartObj.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = currentChartObj.AddComponent<LayoutElement>();
            }
            layoutElement.minHeight = chartHeight;
            layoutElement.preferredHeight = chartHeight;
            layoutElement.flexibleHeight = 0; // 防止被拉伸

            // 设置 RectTransform
            chartRect.sizeDelta = new Vector2(targetWidth, chartHeight);

            // 确保不会被布局组件影响
            RectTransform parentRect = chartRect.parent as RectTransform;
            if (parentRect != null)
            {
                VerticalLayoutGroup vlg = parentRect.GetComponent<VerticalLayoutGroup>();
                if (vlg != null)
                {
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                }
            }
        }

        InitializeChart();
    }

    private void InitializeChart()
    {
        if (currentChart == null) return;

        currentChart.Init();


        var title = currentChart.EnsureChartComponent<Title>();
        title.text = "RL Reward History";
        title.subText = "Simulation Time: " + System.DateTime.Now.ToString();
        // 设置主标题样式
        title.labelStyle.textStyle.fontSize = 10;  // 设置主标题字体大小
        
        // 设置副标题样式
        title.subLabelStyle.textStyle.fontSize = 10;  // 设置副标题字体大小



        var xAxis = currentChart.EnsureChartComponent<XAxis>();
        var yAxis = currentChart.EnsureChartComponent<YAxis>();

        xAxis.show = true;
        yAxis.show = true;
        xAxis.type = Axis.AxisType.Value;
        yAxis.type = Axis.AxisType.Value;
        xAxis.minMaxType = Axis.AxisMinMaxType.Default;
        yAxis.minMaxType = Axis.AxisMinMaxType.Default;



        // 添加轴标签
        xAxis.axisName.show = true;
        xAxis.axisName.name = "Time";
        xAxis.axisName.labelStyle.textStyle.fontSize = 10;
        xAxis.axisLabel.textStyle.fontSize = 5;
        yAxis.axisName.show = true;
        yAxis.axisName.name = "Reward";
        yAxis.axisName.labelStyle.textStyle.fontSize = 10;
        yAxis.axisLabel.textStyle.fontSize = 5;

        var tooltip = currentChart.EnsureChartComponent<Tooltip>();
        tooltip.show = true;
        

        currentChart.RemoveData();
        currentChart.RemoveAllSerie();

        var serie = currentChart.AddSerie<Line>();
        serie.show = true;
        serie.symbol.show = true;
        serie.symbol.type = SymbolType.Circle;
        serie.symbol.size = 3f;
        serie.lineType = LineType.Smooth;
        serie.lineStyle.width = 1f;
        serie.lineStyle.color = Color.blue;
        serie.animation.enable = true;

        var grid = currentChart.EnsureChartComponent<GridCoord>();
        grid.show = true;
        grid.backgroundColor = new Color32(240, 240, 240, 255);

        currentChart.RefreshChart();
    }

    public void EndSimulation()
    {
        isRecording = false;
        // 保存当前轮次的数据
        allSimulationsData.Add(new List<RewardRecord>(rewardHistory));
        FinalizeChart();
    }

    // 新增：比较模拟数据的方法
    public void CompareSimulations()
    {
        if (allSimulationsData.Count < 2)
        {
            Debug.LogWarning("Need at least 2 simulations to compare!");
            return;
        }

        CreateComparisonChart();
    }

    private void CreateComparisonChart()
    {
        GameObject comparisonChartObj = Instantiate(chartPrefab, scrollRect.content);
        LineChart comparisonChart = comparisonChartObj.GetComponent<LineChart>();

        // 设置图表大小
        RectTransform chartRect = comparisonChartObj.GetComponent<RectTransform>();
        if (chartRect != null)
        {
            float scrollViewWidth = scrollRect.GetComponent<RectTransform>().rect.width;
            float targetWidth = scrollViewWidth - 40f;

            LayoutElement layoutElement = comparisonChartObj.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = comparisonChartObj.AddComponent<LayoutElement>();
            }
            layoutElement.minHeight = chartHeight;
            layoutElement.preferredHeight = chartHeight;
            layoutElement.flexibleHeight = 0;

            chartRect.sizeDelta = new Vector2(targetWidth, chartHeight);
        }

        InitializeComparisonChart(comparisonChart);
    }

    private void InitializeComparisonChart(LineChart chart)
    {
        chart.Init();

        var title = chart.EnsureChartComponent<Title>();
        title.text = "Simulations Comparison";
        title.subText = "Multiple Runs Comparison";
        title.labelStyle.textStyle.fontSize = 10;
        title.subLabelStyle.textStyle.fontSize = 10;

        var xAxis = chart.EnsureChartComponent<XAxis>();
        var yAxis = chart.EnsureChartComponent<YAxis>();

        xAxis.show = true;
        yAxis.show = true;
        xAxis.type = Axis.AxisType.Value;
        yAxis.type = Axis.AxisType.Value;
        xAxis.minMaxType = Axis.AxisMinMaxType.Default;
        yAxis.minMaxType = Axis.AxisMinMaxType.Default;

        xAxis.axisName.show = true;
        xAxis.axisName.name = "Time";
        xAxis.axisName.labelStyle.textStyle.fontSize = 10;
        xAxis.axisLabel.textStyle.fontSize = 5;
        yAxis.axisName.show = true;
        yAxis.axisName.name = "Reward";
        yAxis.axisName.labelStyle.textStyle.fontSize = 10;
        yAxis.axisLabel.textStyle.fontSize = 5;

        var tooltip = chart.EnsureChartComponent<Tooltip>();
        tooltip.show = true;

        var legend = chart.EnsureChartComponent<Legend>();
        legend.show = true;
        legend.labelStyle.textStyle.fontSize = 10;
        chart.ClearData();
        chart.RemoveData();  // 清除第一个 series

        // 为每个模拟数据添加一条线
        for (int i = 0; i < allSimulationsData.Count; i++)
        {
            var serie = chart.AddSerie<Line>();
            serie.serieName = $"Simulation {i + 1}";
            serie.show = true;

            // 获取当前模拟的颜色
            Color currentColor = simulationColors[i % simulationColors.Count];

            // 设置点的属性
            serie.symbol.show = true;
            serie.symbol.type = SymbolType.Circle;
            serie.symbol.size = 3f;
            serie.symbol.color = currentColor;  // 设置点的颜色

            // 设置 itemStyle 颜色
            serie.itemStyle.color = currentColor;

            // 设置线的属性
            serie.lineType = LineType.Smooth;
            serie.lineStyle.width = 1f;
            serie.lineStyle.color = currentColor;  // 设置线的颜色
            serie.animation.enable = true;

            // 添加数据点
            foreach (var record in allSimulationsData[i])
            {
                serie.AddData(record.time, record.reward);
            }
        }
        var grid = chart.EnsureChartComponent<GridCoord>();
        grid.show = true;
        grid.backgroundColor = new Color32(240, 240, 240, 255);

        chart.RefreshChart();
    }

    // 新增：清除所有历史数据
    public void ClearAllData()
    {
        allSimulationsData.Clear();
        ClearAllCharts();
    }

    private void FinalizeChart()
    {
        if (currentChart == null) return;

        float avgReward = 0f;
        float maxReward = float.MinValue;
        float minReward = float.MaxValue;

        foreach (var record in rewardHistory)
        {
            avgReward += record.reward;
            maxReward = Mathf.Max(maxReward, record.reward);
            minReward = Mathf.Min(minReward, record.reward);
        }

        if (rewardHistory.Count > 0)
        {
            avgReward /= rewardHistory.Count;

            var title = currentChart.GetChartComponent<Title>();
            title.subText += $"\nAvg: {avgReward:F2} Max: {maxReward:F2} Min: {minReward:F2}";

            currentChart.RefreshChart();
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public void ClearAllCharts()
    {
        foreach (Transform child in scrollRect.content)
        {
            Destroy(child.gameObject);
        }
    }
}