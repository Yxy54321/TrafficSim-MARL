using UnityEngine;
using System.Net;
using System.Text;
using System.Collections.Specialized;
using System.Web;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

[System.Serializable]
public class InfoData
{
    public int n_actions;
    public int n_agents;
    public int state_shape;
    public int obs_shape;

    public int n_signal_agents;
    public int signal_state_shape;
    public int signal_obs_shape;
    public int n_signal_actions;
    public int episode_limit;
}



public class HttpServer : MonoBehaviour
{
    private Dictionary<string, int> cloneCounters = new Dictionary<string, int>();
    private bool isRunning = false;
    private float reward = 0;
    bool first = true;
    private List<GameObject> cars;
    private List<GameObject> point;
    private List<GameObject> line;
    private List<Vector3> cars_copy_trans;
    private List<Vector3> cars_copy_trans_tar;
    private List<Quaternion> cars_copy_rotate;
    private List<List<int>> point_copy;
    private HttpListener listener;
    private int innerSize;
    private int outerSize;
    private List<List<float>> ob;
    private InfoData infoData;
    private bool rese = false;
    const int ray_count = 18;
    private float count = 0;
    public float bei = 1;
    private int ty = 1;
    private object listenerLock = new object();
    public ConcurrentDictionary<int, ConcurrentDictionary<int, float>> threadSafeArray = new ConcurrentDictionary<int, ConcurrentDictionary<int, float>>();
    public ConcurrentDictionary<int, int> ste = new ConcurrentDictionary<int, int>();
    public ConcurrentDictionary<int, float> re = new ConcurrentDictionary<int, float>(); 
    public ConcurrentDictionary<int, ConcurrentDictionary<int, int>> ste_avail = new ConcurrentDictionary<int, ConcurrentDictionary<int, int>>();
    
    // 定义 signal_ste 字典，键为路灯ID，值为action状态
    public ConcurrentDictionary<int, int> signal_ste = new ConcurrentDictionary<int, int>();
    public ConcurrentDictionary<int, int> signal_re = new ConcurrentDictionary<int, int>();
    public ConcurrentDictionary<int, int> signal_state = new ConcurrentDictionary<int, int>();

    private Dictionary<string, string> simulationParameters;
    //------------------------------------visualization---------------------------------//
    private RLRewardVisualizer visualizer;

    //------------------------------------visualization---------------------------------//

    public GameObject CloneModelWithIncrementNumber(string modelName, Vector3 position, Vector3 rotation)
    {
        // 查找原始模型
        GameObject originalObject = GameObject.Find(modelName);

        // 检查原始对象是否存在
        if (originalObject == null)
        {
            Debug.LogError($"未找到名为 {modelName} 的模型!");
            return null;
        }

        // 复制对象
        GameObject clonedObject = Instantiate(originalObject, position, Quaternion.Euler(rotation));

        // 生成新的名称
        string newName = GenerateIncrementedName(modelName);
        clonedObject.name = newName;

        return clonedObject;
    }
    public void InitializeWithParameters(Dictionary<string, string> parameters)
    {
        simulationParameters = parameters;
        // 根据参数初始化模拟场景


        ApplyParameters();
    }

    private string GenerateIncrementedName(string originalName)
    {
        // 提取基础名称和数字
        string baseNameWithoutNumber = Regex.Replace(originalName, @"\d+", "");
        int currentNumber = ExtractNumber(originalName);

        // 初始化或递增计数器
        if (!cloneCounters.ContainsKey(baseNameWithoutNumber))
        {
            cloneCounters[baseNameWithoutNumber] = currentNumber > 0 ? currentNumber : 1;
        }
        else
        {
            cloneCounters[baseNameWithoutNumber]++;
        }

        // 构建新名称
        return $"{baseNameWithoutNumber}{cloneCounters[baseNameWithoutNumber]}";
    }

    /// <summary>
    /// 从名称中提取数字
    /// </summary>
    /// <param name="name">原始名称</param>
    /// <returns>提取的数字，如果没有数字则返回0</returns>
    private int ExtractNumber(string name)
    {
        // 使用正则表达式提取字符串中的数字
        Match match = Regex.Match(name, @"\d+");

        // 如果找到数字，转换为整数；否则返回0
        return match.Success ? int.Parse(match.Value) : 0;
    }

    private void ApplyParameters()
    {
        // 这里添加根据参数设置场景的逻辑
        foreach (var param in simulationParameters)
        {
            Debug.Log($"Parameter: {param.Key} = {param.Value}");
            // 根据参数名和值来设置场景中的组件
        }
    }


    private Thread thread;
    private bool reload = false;
    public string serverAddress = "http://localhost:8888/";

    public void AddElement_re(int index, float value)
    {
        re.AddOrUpdate(index, value, (key, oldValue) => value);
    }

    // ��ȡԪ��
    public bool TryGetElement_re(int index, out float value)
    {
        return re.TryGetValue(index, out value);
    }
    public void AddElement_ste(int index, int value)
    {
        ste.AddOrUpdate(index, value, (key, oldValue) => value);
    }

    public void AddElement_signal_ste(int index, int value)
    {
        signal_ste.AddOrUpdate(index, value, (key, oldValue) => value);
    }

    // ��ȡԪ��
    public bool TryGetElement_ste(int index, out int value)
    {
        return ste.TryGetValue(index, out value);
    }
    // ����Ԫ��
    public void AddElement(int row, int column, float value)
    {
        if (!threadSafeArray.ContainsKey(row))
        {
            threadSafeArray.TryAdd(row, new ConcurrentDictionary<int, float>());
        }
        threadSafeArray[row].AddOrUpdate(column, value, (key, oldValue) => value);
    }

    // ��ȡԪ��
    public bool TryGetElement(int row, int column, out float value)
    {
        value = 0;
        if (threadSafeArray.ContainsKey(row) && threadSafeArray[row].ContainsKey(column))
        {
            return threadSafeArray[row].TryGetValue(column, out value);
        }
        return false;
    }
    public void AddElement_avail(int row, int column, int value)
    {
        if (!ste_avail.ContainsKey(row))
        {
            ste_avail.TryAdd(row, new ConcurrentDictionary<int, int>());
        }
        ste_avail[row].AddOrUpdate(column, value, (key, oldValue) => value);
    }

    // ��ȡԪ��
    public bool TryGetElement_avail(int row, int column, out int value)
    {
        value = 0;
        if (ste_avail.ContainsKey(row) && ste_avail[row].ContainsKey(column))
        {
            return ste_avail[row].TryGetValue(column, out value);
        }
        return false;
    }




    //------------------------------判断合法性-------------------------------//
    private bool IsPointInRoadLaneBounds(Vector3 pointPosition, GameObject roadLane)
    {
        Renderer renderer = roadLane.GetComponent<Renderer>();
        if (renderer == null) return false;
        Bounds bounds = renderer.bounds;
        // 仅判断XZ平面
        bool xInRange = pointPosition.x >= bounds.min.x && pointPosition.x <= bounds.max.x;
        bool zInRange = pointPosition.z >= bounds.min.z && pointPosition.z <= bounds.max.z;
        return xInRange && zInRange;
    }

    /// <summary>
    /// 确定车辆的旋转方向
    /// </summary>
    /// <param name="pointPosition">需要判断的点坐标</param>
    /// <returns>车辆的旋转（Quaternion）</returns>
    public Vector3 DetermineVehicleRotation(Vector3 pointPosition)
    {
        // 找出所有包含 "Road Lane_" 的路面块模型
        GameObject[] roadLanes = GameObject.FindGameObjectsWithTag("Road");

        foreach (GameObject roadLane in roadLanes)
        {
            if (roadLane.name.Contains("Road Lane_") &&
                IsPointInRoadLaneBounds(pointPosition, roadLane))
            {
                // 获取路面块的朝向
                Vector3 roadLaneDirection = roadLane.transform.forward;

                // 计算点相对于路面块中心的水平位置
                Renderer renderer = roadLane.GetComponent<Renderer>();
                Vector3 roadLaneCenter = renderer.bounds.center;

                // 使用叉乘判断点是在路面块的左侧还是右侧
                Vector3 rightVector = Vector3.Cross(Vector3.up, roadLaneDirection);
                float sideValue = Vector3.Dot(pointPosition - roadLaneCenter, rightVector);

                // 根据点的位置确定车辆朝向
                Vector3 finalDirection = sideValue > 0 ? roadLaneDirection : -roadLaneDirection;

                // 将方向转换为欧拉角
                return new Vector3(0, Quaternion.LookRotation(finalDirection).eulerAngles.y, 0);
            }
        }

        // 如果没有找到匹配的路面块
        Debug.LogWarning("未找到包含点坐标的路面块");
        return new Vector3(0, 0, 0);
    }
    //------------------------------判断合法性-------------------------------//

    void Awake()
    {
        // 获取之前记录的所有点
        List<Vector3> recordedPoints = RawImageClickCoordinates.Instance.selectedPoints;
        visualizer = FindObjectOfType<RLRewardVisualizer>();
        // 遍历所有记录的点
        // 遍历所有记录的点，从第二个点开始
        for (int i = 1; i < recordedPoints.Count; i++)
        {
            if (i == 1)
            {
                GameObject originalObject = GameObject.Find("Vehicle&sp0");
                Vector3 point = recordedPoints[i];
                Vector3 rotation = DetermineVehicleRotation(point);
                // 设置位置
                originalObject.transform.position = point;
                // 设置旋转（欧拉角）
                originalObject.transform.rotation = Quaternion.Euler(rotation);
            }
            else
            {
                Vector3 point = recordedPoints[i];
                Vector3 rotation = DetermineVehicleRotation(point);
                GameObject clonedVehicle = CloneModelWithIncrementNumber(
                    "Vehicle&sp0",
                    point,
                    rotation
                );
            }
        }
    }
    void Start()
    {
        Debug.Log("in start");
        var clickmng = FindObjectOfType<RawImageClickCoordinates>();
        clickmng.ClearSelectedPoints();

        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        cars = new List<GameObject>();
        line = new List<GameObject>();
        point = new List<GameObject>();
        cars_copy_trans = new List<Vector3>();
        cars_copy_trans_tar = new List<Vector3>();
        cars_copy_rotate = new List<Quaternion>();
        point_copy = new List<List<int>>();
        foreach (GameObject rootObject in rootObjects)
        {
            if (rootObject.transform.name.Contains("Vehicle") && ty == 0) //ty
            {
                cars.Add(rootObject);
                cars_copy_trans.Add(rootObject.transform.position);
                cars_copy_trans_tar.Add(rootObject.GetComponent<NpcControl>().targetPosition);
                cars_copy_rotate.Add(rootObject.transform.rotation);
            }
            if (rootObject.transform.name.Contains("Vehicle") && ty == 1)//ty作用是什么？
            {
                if (rootObject.transform.name.Contains("sp"))
                {
                    cars.Add(rootObject);
                    cars_copy_trans.Add(rootObject.transform.position);
                    cars_copy_trans_tar.Add(rootObject.GetComponent<NpcControl>().targetPosition);
                    cars_copy_rotate.Add(rootObject.transform.rotation);
                }
            }
            if (rootObject.transform.name.Contains("Road Control"))
            {
                line.Add(rootObject);
            }
            if (rootObject.transform.name.Contains("InterSection Control"))
            {
                point.Add(rootObject);
                List<int> tmd = new List<int>();
                tmd.Add(rootObject.transform.GetComponent<SpecialRouting>().signal);
                tmd.Add((int)(rootObject.transform.GetComponent<SpecialRouting>().count));
                point_copy.Add(tmd);
            }
        }

        outerSize = cars.Count;

        innerSize = point.Count * 4 + 18 + 2 + 2;
        ob = new List<List<float>>();

  
        infoData = new InfoData();
        infoData.n_actions = 4;
        infoData.n_agents = cars.Count;


        infoData.n_signal_agents = point.Count;
        infoData.n_signal_actions = 3;
        infoData.signal_state_shape = cars.Count * 2 + 4;
        infoData.signal_obs_shape = cars.Count * 2 + 4;

 
        infoData.state_shape =4;
        infoData.obs_shape =4;

        infoData.episode_limit =6 * 60;

    
        int j = 0;
        int i = 0;
        for (i = 0; i < outerSize; i++)
        {
            for (j = 0; j < 4; j++)
            {
                if (j == 2)
                {
                    AddElement_avail(i, j, 1);

                }
                else
                {
                    AddElement_avail(i, j, 0);
                }
            }
        }
        for (i = 0; i < outerSize + 1; i++)
        {
            List<float> hh = new List<float>();
            if (i == 0)
            {
                for (j = 0; j < point.Count; j++)
                {
                    hh.Add(point[j].transform.position.x);
                    hh.Add(point[j].transform.position.z);
                    hh.Add(point[j].transform.GetComponent<SpecialRouting>().signal);
                    hh.Add(point[j].transform.GetComponent<SpecialRouting>().count);
                }
            }
            else
            {
                //List<float> see = cars[i].GetComponent<CarControl>().getColider();
                for (j = 0; j < 18; j++)
                {
                    hh.Add(20f);
                }
                i = i - 1;
                hh.Add(cars[i].transform.position.x);
                hh.Add(cars[i].transform.position.z);
                hh.Add(cars[i].transform.GetComponent<NpcControl>().targetPosition.x);
                hh.Add(cars[i].transform.GetComponent<NpcControl>().targetPosition.z);
             
                i = i + 1;
            }
            ob.Add(hh);

        }
        i = 0;
        j = 0;
        //把这个ob填到threadsafearray里
        foreach(List<float> ii in ob)
        {
            j = 0;
            foreach(float element in ii)
            {
                AddElement(i, j, ob[i][j]);
                j++;
            }
            i++;
        }
        AddElement_re(0, 0);
        AddElement_re(1, 0);
        for (i = 0; i < outerSize; i++)
        {
            AddElement_re(i+2, 0);
        }
        for (i = 0; i < point.Count; i++)
        {
            signal_ste.TryAdd(i, 0);
            Debug.Log(i);
        }

        thread = new Thread(StartServer);
        thread.Start();
        visualizer.StartNewSimulation();
    }

    // 创建场景加载完成后的回调函数
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("enter");
        // 找到ControlPanelManager并重置视图
        var controlPanelManager = FindObjectOfType<ControlPanelManager>();
        if (controlPanelManager != null)
        {
            Debug.Log("exe");
            controlPanelManager.ResetSimulationView();
        }

        // 重要：记得移除事件监听，避免重复执行
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        count += Time.deltaTime;
        if (count > 90)
        {
           
            AddElement_re(1, 1);
        }
        int shu = 0;
        int ji = 0;
        for (; shu < cars.Count; shu++)
        {
            if (cars[shu].transform.position==new Vector3(-100, 100, -100))
            {
                Debug.Log(cars[shu].name+" arrived!");
                ji++;
            }
        }
        if (ji == infoData.n_agents)
        {
            AddElement_re(1, 1);
            
            if (first)
            {
                var mng = FindObjectOfType<ControlPanelManager>();
                mng.TogglePause();
                first = false;
                visualizer.EndSimulation();
            }
        }

        // 在模拟过程中记录reward
        reward = 0;
        float step_re=0;
        for (int i = 0; i < cars.Count; i++)
        {
            TryGetElement_re(i + 2, out step_re);
            reward += step_re;
        }

        visualizer.RecordReward(reward);

        if (reload == true)
        {
            if (thread.IsAlive)
            {
                thread.Join();
            }
            Debug.Log("reset");

            // 同步卸载场景
            SceneManager.UnloadScene("simulation");

            SceneManager.LoadSceneAsync("simulation", LoadSceneMode.Additive).completed += (loadOp) =>
               {
                   // 获取新加载的场景
                   Scene simulationScene = SceneManager.GetSceneByName("simulation");
                   // 设置为激活场景，这样才会执行场景中对象的Start()函数
                   SceneManager.SetActiveScene(simulationScene);
                   // 检查 HttpServer 的状态
                   var httpServer = FindObjectOfType<HttpServer>();
                   if (httpServer != null)
                   {
                       Debug.Log($"HttpServer found, enabled: {httpServer.enabled}, gameObject active: {httpServer.gameObject.activeSelf}");
                       // 尝试强制重新初始化
                       httpServer.enabled = false;
                       httpServer.enabled = true;
                   }
                   else
                   {
                       Debug.Log("HttpServer not found!");
                   }
                   // 场景加载完成后直接执行重置
                   var controlPanelManager = FindObjectOfType<ControlPanelManager>();
                   if (controlPanelManager != null)
                   {
                       controlPanelManager.ResetSimulationView();
                   }
               };


            // 重置视图
            var controlPanelManager = FindObjectOfType<ControlPanelManager>();
            if (controlPanelManager != null)
            {
                controlPanelManager.ResetSimulationView();
            }
           
        }
        if (rese == true)
        {
            AddElement_re(1, 0);
            AddElement_re(0, 0);
            int i = 0;
            for (; i < cars.Count; i++)
            {
                AddElement_re(i + 2, 0);
            }
            count = 0;
            i = 0;
            foreach(GameObject car in cars)
            {
           
                car.transform.position = cars_copy_trans[i];
                car.transform.rotation = cars_copy_rotate[i];
                int cho = Mathf.RoundToInt(UnityEngine.Random.Range(0, line.Count));
                car.transform.GetComponent<NpcControl>().targetPosition = line[cho].transform.position;
                car.transform.GetComponent<NpcControl>().rese = true;
                car.transform.GetComponent<NpcControl>().arrived = false;
                i++;
            }
            i = 0;
            foreach (GameObject poin in point)
            {
                poin.transform.GetComponent<SpecialRouting>().signal = point_copy[i][0];
                poin.transform.GetComponent<SpecialRouting>().count = point_copy[i][1];
                i++;
            }
            rese = false;

        }
    }

    private void OnDestroy()
    {
        StopServer();
    }

    private void StopServer()
    {
        if (isRunning)
        {
            isRunning = false;

            if (listener != null)
            {
                listener.Stop();
                listener.Close();
            }

            if (thread != null && thread.IsAlive)
            {
                thread.Join();  // 等待线程结束
            }
        }
    }

    private void StartServer()
    {
        lock (listenerLock)
        {
            try
            {
                // 检查并清理旧的 listener
                if (listener != null)
                {
                    try
                    {
                        isRunning = false;
                        listener.Stop();
                        listener.Close();
                        Debug.Log("Old listener cleaned up");
                    }
                    catch (ObjectDisposedException)
                    {
                        Debug.Log("Old listener was already disposed");
                    }
                    finally
                    {
                        listener = null;
                    }
                }

                // 初始化新的 listener
                listener = new HttpListener();
                listener.Prefixes.Add(serverAddress);
                Debug.Log("infoadd");
                listener.Start();
                isRunning = true;
                Debug.Log("Server started at: " + serverAddress);

                while (isRunning)
                {
                    try
                    {
                        if (listener == null) break;
                        HttpListenerContext context = listener.GetContext();
                        HandleRequest(context);
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.Log("Error handling request: " + ex.Message);
                        break;
                    }
                }

                if (listener != null)
                {
                    try
                    {
                        listener.Stop();
                    }
                    catch (ObjectDisposedException) { }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Server error: {ex.Message}");
            }
        }
    }

    void HandleRequest(HttpListenerContext context)
    {
        // ��ȡ�����·��
        string requestPath = context.Request.Url.AbsolutePath.ToLower();

        // �������·��Ϊ /moveforward���������󷽷�Ϊ GET������ moveforward ����
        if (requestPath == "/step" && context.Request.HttpMethod == "GET")
        {
            Debug.Log("interacted!");
            // ִ�� moveforward ����
            NameValueCollection queryParams = HttpUtility.ParseQueryString(context.Request.Url.Query);

            // ��ȡ�ض�������ֵ
            string param1Value = queryParams["actions"];
            string param2Value = queryParams["actions_sig"];
            List<int> action = JsonConvert.DeserializeObject<List<int>>(param1Value);
            List<int> action_sig = JsonConvert.DeserializeObject<List<int>>(param2Value);
            int i = 0;
            float reward = 0;
            float tm = 0;  
            for(i=0;i<cars.Count;i++)
            {
                TryGetElement_re(i + 2, out tm);
                reward += tm;
            }
            AddElement_re(0, reward);
            // ���ͳɹ���Ӧ
            SendResponse(context, JsonConvert.SerializeObject(re));
            AddElement_re(0, 0);
            /*for (i = 0; i < cars.Count; i++)
            {
                AddElement_ste(i, 5);
            }*/
            for (i = 0; i < cars.Count; i++)
            {
                AddElement_ste(i, action[i]);
            } 
            for (i=0; i < cars.Count; i++)
            {
                AddElement_re(i + 2, 0);
            }
            //mod
            if (action_sig.Count > 0)
            {
                Debug.Log("sig_action_enabled");
                for (i = 0; i < point.Count; i++)
                {
                    AddElement_signal_ste(i, action_sig[i]);
                }
            }
        }
        else if (requestPath == "/get_re")
        {
            SendResponse(context, JsonConvert.SerializeObject(re));
            var keysToClear = re.Keys.OrderBy(k => k).Skip(2).ToList(); // 根据键排序后，跳过前两个元素

            foreach (var key in keysToClear)
            {
                re[key] = 0f; // 将值设置为 0
            }
        }
        else if(requestPath == "/get_obs")
        {
            SendResponse(context, JsonConvert.SerializeObject(threadSafeArray));
        }
        else if(requestPath == "/reset")
        {
            isRunning = false;
            reload = true;
            
            SendResponse(context, "done");
            
        }
        else if (requestPath == "/get_env_info")
        {
            
            SendResponse(context, JsonConvert.SerializeObject(infoData));
        }
        else if (requestPath == "/close")
        {
            Application.Quit();
        }
        else if (requestPath == "/avail")
        {
            SendResponse(context, JsonConvert.SerializeObject(ste_avail));
        }
        
        else
        {
            
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.Close();
        }
    }
    void SendResponse(HttpListenerContext context, string responseString)
    {
        byte[] responseBytes = Encoding.UTF8.GetBytes(responseString);

        
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = responseBytes.Length;

     
        context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
        context.Response.OutputStream.Close();
    }
}