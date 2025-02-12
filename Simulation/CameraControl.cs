using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine.UIElements;

public class CameraControl : MonoBehaviour
{

    public float moveSpeed = 12f;
    public float sensitivity = 2f;
    public GameObject cube;
    public GameObject light1;
    public GameObject light2;
    private List<GameObject> cars;
    private List<GameObject> point;
    private List<GameObject> line;
    private float[,] ob;
    private int ty = 1;

    private int innerSize;
    private int outerSize;

    private float rotationX = 0f;
    private float rotationY = 0f;

    private void Awake()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetSceneByName("simulation").GetRootGameObjects();
        foreach (GameObject rootObject in rootObjects)
        {
            if (rootObject.transform.name.Contains("Vehicle"))
            {
                if (!(rootObject.transform.name.Contains("sp")))
                {
                    float randomValue = Random.value;
                    // If random number < 0.5, destroy the object
                    if (randomValue < (1-FindObjectOfType<ControlPanelManager>().parameter3Value))
                    {
                        GameObject.Destroy(rootObject);  // Removes object completely
                    }
                }
            }
        }
    }
    public void AddElement(int row, int column, float value)
    {
        if (!GetComponent<HttpServer>().threadSafeArray.ContainsKey(row))
        {
            GetComponent<HttpServer>().threadSafeArray.TryAdd(row, new ConcurrentDictionary<int, float>());
        }
        GetComponent<HttpServer>().threadSafeArray[row].AddOrUpdate(column, value, (key, oldValue) => value);
    }


    public bool TryGetElement(int row, int column, out float value)
    {
        value = 0;
        if (GetComponent<HttpServer>().threadSafeArray.ContainsKey(row) && GetComponent<HttpServer>().threadSafeArray[row].ContainsKey(column))
        {
            return GetComponent<HttpServer>().threadSafeArray[row].TryGetValue(column, out value);
        }
        return false;
    }
    public void AddElement_ste(int index, int value)
    {
        GetComponent<HttpServer>().ste.AddOrUpdate(index, value, (key, oldValue) => value);
    }


    public bool TryGetElement_ste(int index, out int value)
    {
        return GetComponent<HttpServer>().ste.TryGetValue(index, out value);
    }
    public void AddElement_re(int index, float value)
    {
        GetComponent<HttpServer>().re.AddOrUpdate(index, value, (key, oldValue) => value);
    }

 
    public bool TryGetElement_re(int index, out float value)
    {
        return GetComponent<HttpServer>().re.TryGetValue(index, out value);
    }
    int SortByName(GameObject obj1, GameObject obj2)
    {
        return obj1.name.CompareTo(obj2.name);
    }
    private GameObject CreateRedDotMark3D(Vector3 position)
    {
        // 创建一个球体
        GameObject redDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        redDot.name = "RedDot3D";

        // 设置位置
        redDot.transform.position = position;

        // 设置大小
        redDot.transform.localScale = new Vector3(5f, 5f, 5f); // 调整大小

        // 设置材质为红色
        Renderer renderer = redDot.GetComponent<Renderer>();
        renderer.material.color = Color.red;

        return redDot;
    }
    void Start()
    {
        AddElement_re(0, 0);

        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        List<Vector3> targetPosition = new List<Vector3>();
        cars = new List<GameObject>();
        line = new List<GameObject>();
        point = new List<GameObject>();
        

        foreach (GameObject rootObject in rootObjects)
        {
            if(rootObject.name.Contains("InterSection Control"))
            {
                rootObject.GetComponent<SpecialRouting>().signControl[0].transform.position = rootObject.transform.position + new Vector3(-10f, 0, -2.5f);
                rootObject.GetComponent<SpecialRouting>().signControl[1].transform.position = rootObject.transform.position + new Vector3(-2.5f, 0, 10f);
                rootObject.GetComponent<SpecialRouting>().signControl[2].transform.position = rootObject.transform.position + new Vector3(10f, 0, 2.5f);
                rootObject.GetComponent<SpecialRouting>().signControl[3].transform.position = rootObject.transform.position + new Vector3(2.5f, 0, -10f);
            }
            if (rootObject.transform.name.Contains("Vehicle"))
            {
                NpcControl npcControl = rootObject.AddComponent<NpcControl>();
               
            }
            if (rootObject.transform.name.Contains("Control"))
            {
             
                Vector3 newPosition = rootObject.transform.position;
                if (!(rootObject.transform.name.Contains("Light") || rootObject.transform.name.Contains("Side")))
                {
                    newPosition.y = 8;
                }
                else
                {
                    newPosition.y = 1.5f;
                }
                rootObject.transform.position = newPosition;
                if(rootObject.transform.name.Contains("Road Control"))
                {
                    targetPosition.Add(rootObject.transform.position);
                }
            }
        }
        int coun = targetPosition.Count;
        int cho=0;

        //------------------------*--------------------------//
        Vector3 chosp = RawImageClickCoordinates.Instance.selectedPoints[0];
        CreateRedDotMark3D(chosp);
        //------------------------*--------------------------//


        foreach (GameObject rootObject in rootObjects)
        {
            if (rootObject.name.Contains("Control") || rootObject.transform.name.Contains("Corner") || rootObject.transform.name.Contains("Vehicle")|| rootObject.transform.name.Contains("Building"))
            {
                Rigidbody rb = rootObject.AddComponent<Rigidbody>();
                BoxCollider boxCollider = rootObject.AddComponent<BoxCollider>();

                    rb.mass = 30;
                boxCollider.isTrigger = true;
                if (!rootObject.transform.name.Contains("Vehicle")) 
                {
                    rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
                }
                else
                {
                    rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                }
            }
            if (rootObject.transform.name.Contains("Vehicle"))
            {
                if (rootObject.transform.name.Contains("Vehicle&sp"))
                {
                    rootObject.GetComponent<NpcControl>().targetPosition = chosp;
                }
                else
                {
                    if (cho > coun - 1)
                    {
                        cho = 0;
                    }
                    rootObject.GetComponent<NpcControl>().targetPosition = targetPosition[cho];
                    cho++;
                }
            }
        }
        int k = 0;
        int dam = 0;
        foreach (GameObject rootObject in rootObjects)
        {

            if (rootObject.transform.name.Contains("Vehicle") && ty == 0)
            {
                k++;
                cars.Add(rootObject);
                rootObject.name = "Vehicle" + "(" + k.ToString() + ")";
            }

            if (rootObject.transform.name.Contains("Vehicle") && ty == 1)
            {
                if (rootObject.transform.name.Contains("sp"))
                {
                    k++;
                    cars.Add(rootObject);
                    rootObject.name = "Vehicle&sp" + "(" + k.ToString() + ")";
                }
                else
                {
                    dam++;
                    rootObject.name = "Vehicle(7)" + dam.ToString();
                }
            }

            if (rootObject.transform.name.Contains("Road Control"))
            {
                line.Add(rootObject);
            }
            if (rootObject.transform.name.Contains("InterSection Control"))
            {
                point.Add(rootObject);
            }
        }
        point.Sort(SortByName);
        cars.Sort(SortByName);


        outerSize = cars.Count;
 
        innerSize = point.Count * 4 + line.Count * 4 + 18 + 2 + 5 * 2;
        ob = new float[outerSize, innerSize];
    }

    private void Move(float horizontalInput, float verticalInput)
    {

        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
    }

    private void RotateView()
    {

        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        rotationY += mouseX;

        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }
}
