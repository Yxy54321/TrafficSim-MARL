using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;

public class SpecialRouting : MonoBehaviour
{
    public int signal;

    public float count = 0;

    public int interval;

    public GameObject[] signControl=new GameObject[4];

    public GameObject[] sign;

    public GameObject[] roadIn;

    public GameObject[] roadOut;

    private HttpServer httpServer;

    private int bei = 1;
    // 添加元素
    public void AddElement(int row, int column, float value)
    {
        if (!httpServer.threadSafeArray.ContainsKey(row))
        {
            httpServer.threadSafeArray.TryAdd(row, new ConcurrentDictionary<int, float>());
        }
        httpServer.threadSafeArray[row].AddOrUpdate(column, value, (key, oldValue) => value);
    }

    // 获取元素
    public bool TryGetElement(int row, int column, out float value)
    {
        value = 0;
        if (httpServer.threadSafeArray.ContainsKey(row) && httpServer.threadSafeArray[row].ContainsKey(column))
        {
            return httpServer.threadSafeArray[row].TryGetValue(column, out value);
        }
        return false;
    }
    public void AddElement_ste(int index, int value)
    {
        httpServer.ste.AddOrUpdate(index, value, (key, oldValue) => value);
    }

    // 获取元素
    public bool TryGetElement_ste(int index, out int value)
    {
        return httpServer.ste.TryGetValue(index, out value);
    }
    public void AddElement_re(int index, float value)
    {
        httpServer.re.AddOrUpdate(index, value, (key, oldValue) => value);
    }

    // 获取元素
    public bool TryGetElement_re(int index, out float value)
    {
        return httpServer.re.TryGetValue(index, out value);
    }
    // Start is called before the first frame update
    void Start()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        // 遍历根对象
        foreach (GameObject rootObject in rootObjects)
        {
            if (rootObject.transform.name.Contains("Camera"))
            {
                httpServer = rootObject.GetComponent<HttpServer>();
            }
        }
        signal = Mathf.RoundToInt(Random.Range(0, 1));
        interval=(int)FindObjectOfType<ControlPanelManager>().parameter1Value;
    }

    // Update is called once per frame
    void Update()
    {
        int i = int.Parse(name.Split('(')[1].Split(')')[0]);
        count += Time.deltaTime;

        // 获取当前路灯的action
        int action;
        httpServer.signal_ste.TryGetValue(i - 1, out action);

        // 判断action状态并决定是否切换信号
        if (action == 1)
        {
            //当前信号延长3s
            count -= 3;
            httpServer.signal_ste.AddOrUpdate(i - 1, 0, (key, oldValue) => 0);
            AddElement(0, i * 4 + 2 - 4, signal);
            AddElement(0, i * 4 + 3 - 4, count);
        }
        else if (action == 2 || count >= interval / bei)  // action为1或计时器到达周期时切换
        {
            count = 0;  // 重置计时器

            // 切换信号
            signal = (signal == 1) ? 0 : 1;

            // 如果是通过 signal_ste 的 action 触发的切换，重置 signal_ste 的值为 0
            if (action == 1)
            {
                httpServer.signal_ste.AddOrUpdate(i - 1, 0, (key, oldValue) => 0);
            }

            // 更新 HttpServer 的数据
            AddElement(0, i * 4 + 2 - 4, signal);
            AddElement(0, i * 4 + 3 - 4, count);
        }

        // 路灯的显示控制逻辑
        int tmp = signal;
        foreach (GameObject l in signControl)
        {
            l.SetActive(tmp == 1);  // 如果 tmp 为 1 则激活
            tmp = (tmp == 1) ? 0 : 1;
        }

        // 更新 sign 数组中的信号
        foreach (GameObject eachS in sign)
        {
            eachS.GetComponent<SignalScript>().signal = tmp;
            tmp = (tmp == 1) ? 0 : 1;
        }
    }

}
