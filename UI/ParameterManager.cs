using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using UnityEngine.EventSystems;

public class AlgorithmInfo
{
    public string name;        // 算法名称
    public string description; // 算法描述
}
public class ParameterManager : MonoBehaviour
{
    [SerializeField] private GameObject parameterEntryPrefab;
    [SerializeField] private Transform parameterContent;
    [SerializeField] private TMP_Dropdown algorithmDropdown;  // 在Inspector中引用
    private List<SimulationParameter> parameters = new List<SimulationParameter>();

    private readonly AlgorithmInfo[] algorithms = new AlgorithmInfo[]
    {
        new AlgorithmInfo {
            name = "qmix",
            description = "QMIX算法：一种基于值分解的多智能体强化学习算法，通过混合网络实现全局值函数的单调分解。"
        },
        new AlgorithmInfo {
            name = "iql",
            description = "IQL (Independent Q-Learning)算法：每个智能体独立学习其Q函数，不考虑其他智能体的行为。"
        }
    };

    

    // 新增变量
    [SerializeField] private RawImage algorithmImage;  // 用于显示算法介绍图片
    [SerializeField] private ScrollRect scrollView;    // 引用ScrollView组件

    private Dictionary<string, string> algorithmImagePaths = new Dictionary<string, string>()
    {
        { "qmix", "D:\\School\\MFCO\\full_proj\\My project (4)\\Assets\\UI\\qmix.png" },
        { "iql",  "D:\\School\\MFCO\\full_proj\\My project (4)\\Assets\\UI\\iql.png" }
    };

    private void Start()
    {
        // 设置算法下拉框选项
        if (algorithmDropdown != null)
        {
            algorithmDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (var algo in algorithms)
            {
                options.Add(new TMP_Dropdown.OptionData(algo.name));
            }
            algorithmDropdown.AddOptions(options);

            // 添加选择变化事件
            algorithmDropdown.onValueChanged.AddListener(OnAlgorithmSelected);

            // 显示初始算法图片
            OnAlgorithmSelected(0);
        }
    }

    private void OnAlgorithmSelected(int index)
    {
        string selectedAlgorithm = algorithms[index].name;
        if (algorithmImagePaths.ContainsKey(selectedAlgorithm))
        {
            LoadAlgorithmImage(algorithmImagePaths[selectedAlgorithm]);
        }
    }

    private void LoadAlgorithmImage(string imagePath)
    {
        // 加载图片文件
        byte[] imageData = System.IO.File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(4096, 4096);
        texture.LoadImage(imageData);

        // 显示图片
        if (algorithmImage != null)
        {
            // 清除旧的texture以防内存泄漏
            if (algorithmImage.texture != null)
            {
                Destroy(algorithmImage.texture);
            }

            algorithmImage.texture = texture;

            // 调整RawImage的大小以适应图片比例
            float imageRatio = (float)texture.width / texture.height;
            RectTransform imageRect = algorithmImage.GetComponent<RectTransform>();

            // 获取ScrollView的宽度作为最大宽度
            float maxWidth = scrollView.GetComponent<RectTransform>().rect.width;

            // 设置图片大小
            float width = maxWidth;
            float height = width / imageRatio;
            imageRect.sizeDelta = new Vector2(width, height);
        }
    }

    private void OnDestroy()
    {
        // 清理资源
        if (algorithmImage != null && algorithmImage.texture != null)
        {
            Destroy(algorithmImage.texture);
        }
    }
    //private void CreateAlgorithmDropdown()
    //{
    //    // 使用现有的参数条目预制体
    //    GameObject entryObject = Instantiate(parameterEntryPrefab, parameterContent);
    //    entryObject.transform.SetAsFirstSibling();

    //    // 设置参数名
    //    var inputFields = entryObject.GetComponentsInChildren<TMPro.TMP_InputField>();
    //    if (inputFields.Length >= 1)
    //    {
    //        inputFields[0].text = "Algorithm";
    //    }

    //    if (inputFields.Length >= 2)
    //    {
    //        // 获取第二个输入框的GameObject
    //        GameObject valueInputObject = inputFields[1].gameObject;
    //        DestroyImmediate(inputFields[1]);

    //        // 添加下拉框组件
    //        algorithmDropdown = valueInputObject.AddComponent<TMP_Dropdown>();

    //        // 添加下拉框模板
    //        CreateDropdownTemplate(algorithmDropdown);

    //        // 设置选项
    //        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
    //        foreach (var algo in algorithms)
    //        {
    //            options.Add(new TMP_Dropdown.OptionData(algo.name));
    //        }
    //        algorithmDropdown.options = options;
    //        algorithmDropdown.value = 0;

    //        // 添加鼠标悬浮事件
    //        EventTrigger trigger = valueInputObject.AddComponent<EventTrigger>();

    //        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
    //        enterEntry.eventID = EventTriggerType.PointerEnter;
    //        enterEntry.callback.AddListener((data) => ShowTooltip());
    //        trigger.triggers.Add(enterEntry);

    //        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
    //        exitEntry.eventID = EventTriggerType.PointerExit;
    //        exitEntry.callback.AddListener((data) => HideTooltip());
    //        trigger.triggers.Add(exitEntry);
    //    }
    //}


    //   public void AddParameterEntry(string paramName = "", string paramValue = "")
    //   {
    //       GameObject entry = Instantiate(parameterEntryPrefab, parameterContent);

    //       // 获取输入框组件
    //       var inputFields = entry.GetComponentsInChildren<TMPro.TMP_InputField>();
    //       if (inputFields.Length >= 2)
    //       {
    //           // 设置参数名
    //           inputFields[0].text = paramName;
    //           // 设置参数值
    //           inputFields[1].text = paramValue;
    //       }

    //       // 设置删除按钮
    //       Button deleteButton = entry.GetComponentInChildren<Button>();
    //       deleteButton.onClick.AddListener(() => DeleteParameterEntry(entry));

    //       // 添加到参数列表
    //       var parameter = new SimulationParameter
    //       {
    //           parameterName = paramName,
    //           value =
    //paramValue
    //       };
    //       parameters.Add(parameter);

    //       // 设置输入框的值改变事件
    //       if (inputFields.Length >= 2)
    //       {
    //           int index = parameters.Count - 1;
    //           inputFields[0].onValueChanged.AddListener((newName) => parameters[index].parameterName = newName);
    //           inputFields[1].onValueChanged.AddListener((newValue) => parameters[index].value = newValue);
    //       }
    //   }

    //public void AddParameterEntry()
    //{
    //    GameObject entry = Instantiate(parameterEntryPrefab, parameterContent);
    //    Button deleteButton = entry.GetComponentInChildren<Button>();  // 获取删除按钮
    //    deleteButton.onClick.AddListener(() => DeleteParameterEntry(entry));  // 添加点击事件
    //    // 设置输入框的事件监听等
    //}

    //public bool ValidateParameters()
    //{
    //    // 检查参数是否完备
    //    return parameters.All(p => !string.IsNullOrEmpty(p.value));
    //}
    //public void DeleteParameterEntry(GameObject entry)
    //{
    //    // 从parameters列表中移除对应的参数
    //    var inputFields = entry.GetComponentsInChildren<TMPro.TMP_InputField>();
    //    string parameterName = inputFields[0].text;
    //    parameters.RemoveAll(p => p.parameterName == parameterName);

    //    // 销毁UI对象
    //    Destroy(entry);
    //}
    //public Dictionary<string, string> GetParameters()
    //{
    //    return parameters.ToDictionary(p => p.parameterName, p => p.value);
    //}
}