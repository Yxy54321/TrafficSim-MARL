using UnityEngine;
using XCharts.Runtime;

[ExecuteInEditMode]
public class ChartPrefabSetup : MonoBehaviour
{
    private void Reset()
    {
        SetupChart();
    }

    private void SetupChart()
    {
        // 获取或添加LineChart组件
        var chart = gameObject.GetComponent<LineChart>();
        if (chart == null)
            chart = gameObject.AddComponent<LineChart>();

        // 设置RectTransform
        var rectTransform = GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(800f, 400f);

        // 初始化图表
        chart.Init();

        // 设置标题
        var title = chart.EnsureChartComponent<Title>();
        title.show = true;
        title.text = "Reward History";

        // 设置提示框
        var tooltip = chart.EnsureChartComponent<Tooltip>();
        tooltip.show = true;
        tooltip.type = Tooltip.Type.Line;

        // 设置X轴
        var xAxis = chart.EnsureChartComponent<XAxis>();
        xAxis.show = true;
        xAxis.type = Axis.AxisType.Value;
        xAxis.splitNumber = 5;
        xAxis.axisLabel.formatter = "{value}s"; // 显示秒数
        xAxis.axisName.show = true;
        xAxis.axisName.name = "Time";

        // 设置Y轴
        var yAxis = chart.EnsureChartComponent<YAxis>();
        yAxis.show = true;
        yAxis.type = Axis.AxisType.Value;
        yAxis.splitNumber = 5;
        yAxis.axisName.show = true;
        yAxis.axisName.name = "Reward";

        // 清除默认数据系列
        chart.RemoveData();

        // 添加新的数据系列
        var serie = chart.AddSerie<Line>();
        serie.symbol.show = true;
        serie.symbol.type = SymbolType.Circle;
        serie.symbol.size = 5f;
        serie.lineType = LineType.Smooth;
        serie.animation.enable = true;
        serie.lineStyle.width = 2f;
        serie.lineStyle.color = Color.cyan;

        // 刷新图表
        chart.RefreshChart();

        // 完成后销毁该脚本
        DestroyImmediate(this);
    }
}