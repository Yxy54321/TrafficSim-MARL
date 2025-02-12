using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;

public class CarControl : MonoBehaviour
{
    public float moveSpeed = 6f; // 汽车移动速度
    public float turnSpeed = 30f; // 汽车转向速度
    public Vector3 startPosition;
    public Vector3 targetPosition;
    private Quaternion rota;
    private List<float> result;
    private float count = 0;
    private float bei = 1;
    private int lineC=0;
    private int pointC=0;
    private int action = 0;

    public float reward = 0;
    private int index = 0;
    private HttpServer httpServer;
    private List<GameObject> objectsInside = new List<GameObject>(); // 保存进入触发器范围内的其他物体
    // 添加元素
    public void AddElement(int row, int column, float value)
    {
        if (httpServer == null)
        {
            return;
        }
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
        if (httpServer == null)
        {
            value = 0;
            return false;
        }
        return httpServer.ste.TryGetValue(index, out value);
    }
    public void AddElement_re(int index, float value)
    {
        if (httpServer == null)
        {
            return;
        }
        httpServer.re.AddOrUpdate(index, value, (key, oldValue) => value);
    }

    // 获取元素
    public bool TryGetElement_re(int index, out float value)
    {
        if (httpServer == null)
        {
            value = 0;
            return false;
        }
        return httpServer.re.TryGetValue(index, out value);
    }
    public void Start()
    {
        rota = transform.rotation;
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        // 遍历根对象
        foreach (GameObject rootObject in rootObjects)
        {
            if (rootObject.transform.name.Contains("Road Control"))
            {
                lineC++;
            }
            if (rootObject.transform.name.Contains("InterSection Control"))
            {
                pointC++;
            }
            if (rootObject.transform.name.Contains("Camera"))
            {
                httpServer = rootObject.GetComponent<HttpServer>();
            }
        }
        result = new List<float>();
        startPosition = transform.position;
        int i = 0;
        for (i = 0; i < 18; i++)
        {
            result.Add(10f);
        }
        
    }
    private void FixedUpdate()
    {
        transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
        int i = int.Parse(name.Split('(')[1].Split(')')[0]);
        index = i;
        int j = 0;
        List<float> see = getColider();
        for (j = 0; j < 18; j++)
        {
            AddElement(i, j, see[j]);
        }
        AddElement(i, 18, transform.position.x);
        AddElement(i, 19, transform.position.z);
        AddElement(i, 20, targetPosition.x);
        AddElement(i, 21, targetPosition.z);
        float tmp = 0;
        float di = Vector3.Distance(transform.position, targetPosition);
        TryGetElement_re(index+1, out reward);
        tmp = reward;
        TryGetElement_ste(i, out action);
        if (action == 0)
        {
            forward(0);
        }
        else if (action == 1)
        {
            back();
            reward -= 0.8f;
        }
        else if (action == 2)
        {
            left();
            reward -= 0.1f;
        }
        else if (action == 3)
        {
            right();
            reward -= 0.1f;
        }
        else if(action==4)
        {
            forward(1);
        }
        else
        {
            ;
        }
        getReward();
        if (Vector3.Distance(transform.position, targetPosition) < di)
        {
            reward += 0.5f;
        }
        else
        {
            reward -= 0.5f;
        }
        if (reward > -500 && reward < 500)
        {
            AddElement_re(index+1, reward);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        TryGetElement_re(index + 1, out reward);
        reward -= 1f;
        AddElement_re(index + 1, reward);
    }
    private void OnTriggerEnter(Collider other)
    {
        // 检测到碰撞，输出碰撞物体的名称
        TryGetElement_re(index + 1, out reward);
        reward -= 1f;
        AddElement_re(index + 1, reward);
    }
    private void OnCollisionEnter(Collision collision)
    {
        TryGetElement_re(index+1, out reward);
        reward -= 1f;
        AddElement_re(index+1, reward);
    }

    
    public List<float> getColider()
    {
        int rayCount = 18;
        float maxDistance = 20f;
        // 定义射线的起点
        Vector3 origin = transform.position;
        origin.y = 1.5f;

        // 计算每个射线的角度间隔
        float angleStep = 360f / rayCount;
        int i;
        // 发射射线，并获取碰撞信息
        for (i = 0; i < rayCount; i++)
        {
            // 计算当前射线的方向
            Vector3 direction = Quaternion.Euler(0, angleStep * i, 0) * transform.forward;

            // 创建射线
            Ray ray = new Ray(origin, direction);

            // 声明 RaycastHit 变量，用于存储射线碰撞信息
            RaycastHit hitInfo;
            // 发射射线，并检查是否碰撞到物体
            if (Physics.Raycast(ray, out hitInfo, maxDistance))
            {
                // 获取碰撞点与起点之间的距离
                float distance = hitInfo.distance;
                //Debug.DrawLine(transform.position, hitInfo.point, Color.red);
                if (distance < 20f)
                {
                    ray.origin = origin + direction * (distance + 0.1f);
                    if(Physics.Raycast(ray, out hitInfo, 20f - distance - 0.1f))
                    {
                        distance += 0.1f + hitInfo.distance;
                        Debug.DrawLine(ray.origin, hitInfo.point, Color.red);
                    }
                    else
                    {
                        distance = 20f;
                    }
                }
                else
                {
                    distance = 20f;
                }
                result[i] = distance;
            }
            else
            {
                result[i] = maxDistance;
            }
        }
        return result;
    }

    public float getReward()
    {
        count+=Time.deltaTime;
        
        Ray ray = new Ray(transform.position, transform.up);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 10f))
        {
            if (hit.collider.gameObject.name.Contains("Road") && Mathf.Abs(Vector3.Angle(hit.collider.gameObject.transform.right, transform.forward)) > 20)
            {
                
                Vector3 cross = Vector3.Cross(transform.forward, hit.collider.gameObject.transform.right);
                if (cross.y < 0) // 如果 cross.y 为负数，则应向左旋转
                {
                    transform.Rotate(Vector3.up, - turnSpeed * 2 * Time.deltaTime);
                }
                else if (cross.y != 0)
                {
                    transform.Rotate(Vector3.up, turnSpeed * 2 * Time.deltaTime);
                }
                reward -= Mathf.Abs(Vector3.Angle(hit.collider.gameObject.transform.right, transform.forward))/15;

            }
        }
        else
        {
            reward -= 1f;
            transform.position = startPosition;
            transform.rotation = rota;
        }


        if (!name.Contains("special") && Vector3.Distance(transform.position, targetPosition) < 10)
        {
            float gama = 1;
            if (count > 60f /count / bei)
            {
                gama = 60f / bei / count;
            }
            count = 0;
            Vector3 tmpPosition;
            rota=transform.rotation;
            tmpPosition = startPosition;
            startPosition = targetPosition;
            targetPosition = tmpPosition;
            reward += 10 * gama;
        }
        return reward;
    }
    public void forward(int i)
    {
        float sp = moveSpeed;
        if (i == 1)
        {
            sp = sp / 2;
        }
        
        
        transform.Translate(Vector3.forward * sp * Time.deltaTime, Space.Self);
        
    }
    public void back()
    {
        
        transform.Translate(Vector3.back * moveSpeed / 2f * moveSpeed *Time.deltaTime, Space.Self);
      
    }
    public void left()
    {
        /*Quaternion turn = Quaternion.Euler(transform.TransformDirection(new Vector3(0, -1 * turnSpeed * Time.deltaTime * 2 * bei, 0)));
        transform.GetComponent<Rigidbody>().MoveRotation(transform.rotation * turn);*/
        transform.Rotate(new Vector3(0, -1 * turnSpeed * Time.deltaTime, 0));
        //forward(1);
        /*if (objectsInside.Count > 0)
        {
            reward -= objectsInside.Count;
            transform.Rotate(Vector3.up, 1 * turnSpeed * Time.deltaTime * 3 * bei, Space.Self);
        }*/
    }
    public void right()
    {
        /*Quaternion turn = Quaternion.Euler(transform.TransformDirection(new Vector3(0, 1 * turnSpeed * Time.deltaTime * 2 * bei, 0)));
        transform.GetComponent<Rigidbody>().MoveRotation(transform.rotation * turn);
        forward(1);*/
        transform.Rotate(new Vector3(0, turnSpeed * Time.deltaTime, 0));
        /*if (objectsInside.Count > 0)
        {
            reward -= objectsInside.Count;
            transform.Rotate(Vector3.up, -1 * turnSpeed * Time.deltaTime * 3 * bei, Space.Self);
        }*/
    }

}
