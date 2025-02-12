using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;

public class NpcControl : MonoBehaviour
{
    public float speed = 16f;

    private float old_distance = 1000;

    public float rotationSpeed = 1f; // ��ת�ٶ�

    public int end = 0;

    public Vector3 targetPosition;

    public GameObject road;

    public int num;

    public GameObject fireDepart;

    public GameObject colliderObject;

    public int direction;

    private Vector3 startPosition;

    private int action = 0;

    private bool isRotating = false; // �Ƿ�������ת

    private bool stop = false;

    private int peopleNum = 0;

    private List<GameObject> objectsInside = new List<GameObject>(); // ������봥������Χ�ڵ���������

    private int pointC = 0;

    private int ty = 1;
    public bool arrived=false;
    public float reward = 0;
    private HttpServer httpServer;
    private int index = 0;
    private float count = 0;
    public bool rese = false;
    public void AddElement(int row, int column, float value)
    {
        if (!transform.name.Contains("Vehicle&sp") && ty == 1)
        {
            return;
        }
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

    // ��ȡԪ��
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

    // ��ȡԪ��
    public bool TryGetElement_ste(int index, out int value)
    {
        if (!transform.name.Contains("Vehicle&sp") && ty == 1)//�������������������������������������������������ػ������aiֻ�������⳵��
        {
            value = 0;
            return true;
        }
        if (httpServer == null)
        {
            value = 0;
            return false;
        }
        return httpServer.ste.TryGetValue(index, out value);
    }
    public void AddElement_avail(int row, int column, int value)
    {
        if (!transform.name.Contains("Vehicle&sp") && ty == 1)//�������������������������������������������������ػ������aiֻ�������⳵��
        {
            return;
        }
        if (httpServer == null)
        {
            return;
        }
        if (!httpServer.ste_avail.ContainsKey(row))
        {
            httpServer.ste_avail.TryAdd(row, new ConcurrentDictionary<int, int>());
        }
        httpServer.ste_avail[row].AddOrUpdate(column, value, (key, oldValue) => value);
    }

    //
    public bool TryGetElement_avail(int row, int column, out int value)
    {
        value = 0;
        if (httpServer.ste_avail.ContainsKey(row) && httpServer.ste_avail[row].ContainsKey(column))
        {
            return httpServer.ste_avail[row].TryGetValue(column, out value);
        }
        return false;
    }
    public void AddElement_re(int index, float value)
    {
        if (!transform.name.Contains("Vehicle&sp") && ty == 1)//�������������������������������������������������ػ������aiֻ�������⳵��
        {
            return;
        }
        if (httpServer == null)
        {
            return;
        }
        httpServer.re.AddOrUpdate(index, value, (key, oldValue) => value);
    }

    // ��ȡԪ��
    public bool TryGetElement_re(int index, out float value)
    {
        if (!transform.name.Contains("Vehicle&sp") && ty == 1)//�������������������������������������������������ػ������aiֻ�������⳵��
        {
            value = 0;
            return true;
        }
        if (httpServer == null)
        {
            value = 0;
            return false;
        }
        return httpServer.re.TryGetValue(index, out value);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name.Contains("Vehicle"))
        {
            TryGetElement_re(index + 1, out reward);
            reward -= 0.05f;
            AddElement_re(index + 1, reward);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.name.Contains("Vehicle"))
        {
            TryGetElement_re(index + 1, out reward);
            reward -= 0.003f;
            AddElement_re(index + 1, reward);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.name.Contains("Vehicle"))
        {
            TryGetElement_re(index + 1, out reward);
            reward += 0.02f;
            AddElement_re(index + 1, reward);
        }
    }
    public List<float> getColider()//->返回18个距离，20以内代表碰撞了
    {
        int rayCount = 18;
        float maxDistance = 20f;
       
        Vector3 origin = transform.position;
        origin.y = 1.5f;

       
        float angleStep = 360f / rayCount;
        int i;
        List<float> result = new List<float>();

        
        for (i = 0; i < rayCount; i++)
        {
            
            Vector3 direction = Quaternion.Euler(0, angleStep * i, 0) * transform.forward;

           
            Ray ray = new Ray(origin, direction);

           
            RaycastHit hitInfo;
           
            if (Physics.Raycast(ray, out hitInfo, maxDistance))
            {
               
                float distance = hitInfo.distance;
                result.Add(distance);
            }
            else
            {
                result.Add(maxDistance);
            }
        }
        return result;
    }

    public float getReward() 
    {
        if (!arrived)
        {
            float cur_distance = Mathf.Abs(targetPosition.x - transform.position.x) + Mathf.Abs(targetPosition.z - transform.position.z);
            count += Time.deltaTime;
          
            if (!name.Contains("special") && Vector3.Distance(transform.position, targetPosition) < 10)
            {
                float gama = 1;
                float tmp_speed = 12f;
                if (name.Contains("&sp"))
                {
                    tmp_speed = 18f;
                }
                if (count > (Mathf.Abs(targetPosition.x - startPosition.x) + Mathf.Abs(targetPosition.z - startPosition.z)) / tmp_speed)
                {
                    gama = (Mathf.Abs(targetPosition.x - startPosition.x) + Mathf.Abs(targetPosition.z - startPosition.z)) / tmp_speed / count;
                }
                count = 0;
                if (arrived == false)
                {
                    reward += 100;
                    arrived = true;
                }
            }
            else
            {
                if (cur_distance < old_distance)
                {
                    float small_re = (old_distance - cur_distance) * 0.15f;
                    if (small_re > 3)
                    {
                        small_re = 3;
                    }
                  
                    reward += small_re;
                }
                old_distance = cur_distance;
            }
            reward -= 0.001f;

        }
        float step_reward = reward;
        
        return step_reward;
    }

    private IEnumerator RotateCoroutine(float forwa)
    {
        isRotating = true;
        Quaternion startRotation = transform.rotation;
        Vector3 eulerAngle = transform.rotation.eulerAngles;
        if (forwa != -180)
        {
            eulerAngle += new Vector3(0f, forwa, 0f); // ��y�᷽����������ת�Ƕ�
        }
        else
        {
            eulerAngle += new Vector3(0f, -90f, 0f); // ��y�᷽����������ת�Ƕ�
        }
        Quaternion targetRotation = Quaternion.Euler(eulerAngle);
        int count = 0;
        float t = 0f;
        int i = 0;
        for (i = 0; i < 2; i++)
        {
            while (t < 1f)
            {
                if (rese == true)
                {
                    isRotating = false;
                    break;
                }
                if (stop == false)
                {
                    speed = 6f;
                    Vector3 mov = transform.forward * speed * Time.deltaTime;
                    count++;
                    if (count * Time.deltaTime < 1f/speed*4f)
                    {
                        if (forwa != 90f)
                        {
                            mov = transform.forward * speed * Time.deltaTime + (-transform.right) * 0.9f * speed / 4f * Time.deltaTime;
                        }
                        else
                        {
                            mov = transform.forward * speed * Time.deltaTime;
                        }
                        transform.Translate(mov, Space.World);
                    }
                    else
                    {
                        speed = 6f;
                        if (action == 2)
                        {
                            speed = 6f;
                        }
                        if (action == 3)
                        {
                            speed = 2f;
                            mov = mov / 3;
                        }
                        rotationSpeed = speed / 11.5f / Mathf.PI * 2;


                        if (forwa == -90f)
                        {
                            t += Time.deltaTime * rotationSpeed;
                        }
                        else if (forwa == -180f)
                        {
                            t += Time.deltaTime * rotationSpeed * 6;//*****************************************************�ȴ���֤����
                        }
                        else if (forwa == 90f)
                        {
                            t += Time.deltaTime * rotationSpeed * 11.5f / 6.5f;
                        }
                        else
                        {
                            t += Time.deltaTime * rotationSpeed;
                        }
                        transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

                        transform.Translate(mov, Space.World);
                    }
                }
                yield return null;
            }
            if (rese == true)
            {
                rese = false;
                break;
            }
            if (forwa == -180f)
            {
                startRotation = transform.rotation;
                eulerAngle = transform.rotation.eulerAngles;
                eulerAngle += new Vector3(0f, -90f, 0f);
                targetRotation = Quaternion.Euler(eulerAngle);
                t = 0f;
            }
            else
            {
                break;
            }
        }
        speed=16f;
        if (name.Contains("&sp"))
        {
            speed = FindObjectOfType<ControlPanelManager>().parameter2Value;
        }
        isRotating = false;
    }
    // Start is called before the first frame update
    void Start()
    {
        if (name.Contains("&sp"))
        {

            speed = FindObjectOfType<ControlPanelManager>().parameter2Value;
        }
        startPosition = transform.position;
        rotationSpeed = speed / 11.5f / Mathf.PI * 2;

        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        // ����������
        foreach (GameObject rootObject in rootObjects)
        {
            if (rootObject.transform.name.Contains("InterSection Control"))
            {
                pointC++;
            }
            if (rootObject.transform.name.Contains("Camera"))
            {
                httpServer = rootObject.GetComponent<HttpServer>();
            }
        }
    }
    // Update is called once per frame
    void Update()
    {

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
        TryGetElement_re(index + 1, out reward);
        TryGetElement_ste(i, out action);
        getReward();
        if (reward > -500 && reward < 500)
        {
            AddElement_re(index + 1, reward);
        }

        if (!name.Contains("special") && Vector3.Distance(transform.position, targetPosition) < 10)
        {
            if (name.Contains("Vehicle&sp"))
            {
                transform.position = new Vector3(-100, 100, -100);
            }
            Vector3 tmpPosition;
            tmpPosition = startPosition;
            startPosition = targetPosition;
            targetPosition = tmpPosition;
        }

        Ray ray = new Ray(transform.TransformPoint(0, 4f, 4f), transform.up);
        Ray ray2 = new Ray(transform.TransformPoint(0, 1f, 0f), transform.forward);
        Ray ray3 = new Ray(transform.TransformPoint(0, 4f, 0), transform.up);
        RaycastHit hit;//前方向上
        RaycastHit hit2;//向前
        RaycastHit hit3;//向上
        GameObject objectFU=new GameObject();
        GameObject objectForward = new GameObject();
        GameObject objectUp = new GameObject();
        GameObject objectLFU = new GameObject();
        int randomInt;
        int tmd = 10;
        AddElement_avail(index-1, 0, 0);
        AddElement_avail(index-1, 1, 0);
        AddElement_avail(index-1, 2, 1);
        AddElement_avail(index-1, 3, 0);
        if (Physics.Raycast(ray3, out hit3, 10f))
        {
            objectUp = hit3.collider.gameObject;
        }
        if (Physics.Raycast(ray, out hit, 10f))
        {
            objectFU = hit.collider.gameObject;
            if (objectFU.name.Contains("Road"))
            {
                AddElement_avail(index-1, 0, 1);
                AddElement_avail(index-1, 1, 1);
                AddElement_avail(index-1, 2, 0);
                AddElement_avail(index-1, 3, 0);
            }
            else
            {
                AddElement_avail(index - 1, 0, 0);
                AddElement_avail(index - 1, 1, 0);
                AddElement_avail(index - 1, 2, 1);
                AddElement_avail(index - 1, 3, 1);
            }
        }
        ray.origin = transform.TransformPoint(-6f, 4f, 4f); //右前方向上

        if (Physics.Raycast(ray, out hit, 10f))
        {
            objectLFU = hit.collider.gameObject;
        }
        stop = false;
        bool real = Physics.Raycast(ray2, out hit2, 5.5f);
        if (real)
        {
            colliderObject = hit2.collider.gameObject;
            if (colliderObject.name.Contains("forwardCollider"))
            {
                colliderObject = colliderObject.transform.parent.gameObject;
            }
        }
        if (real && colliderObject.GetComponent<NpcControl>() != null && !(colliderObject.GetComponent<NpcControl>().colliderObject != null && colliderObject.GetComponent<NpcControl>().colliderObject.name.Contains(name) && colliderObject.GetComponent<NpcControl>().direction <= direction) || end == 1)
        {
            objectForward = colliderObject;
            stop = true;
            float minDistance = 10000;
            GameObject choose = gameObject;
            if (name.Contains("specialF") && end == 1)
            {
                foreach (GameObject fi in fireDepart.GetComponent<fireDepartment>().firePlace)
                {
                    if (Vector3.Distance(transform.position, fi.transform.position) < 30 && Vector3.Distance(transform.position, fi.transform.position) < minDistance)
                    {
                        minDistance = Vector3.Distance(transform.position, fi.transform.position);
                        choose = fi;
                    }
                }
                if (choose != gameObject)
                {
                    choose.GetComponent<FireCreate>().fireStation -= 0.02f * Time.deltaTime;
                }
            }
            if (name.Contains("specialS") && end == 1 && peopleNum < 2)
            {
                foreach (GameObject Si in fireDepart.GetComponent<fireDepartment>().firePlace)
                {
                    if (Vector3.Distance(transform.position, Si.transform.position) < 30 && Vector3.Distance(transform.position, Si.transform.position) < minDistance)
                    {
                        minDistance = Vector3.Distance(transform.position, Si.transform.position);
                        choose = Si;
                    }
                }
                if (choose != gameObject && choose.GetComponent<FireCreate>().hurtNum > 0)
                {
                    choose.GetComponent<FireCreate>().hurtNum -= 1;
                    peopleNum++;
                }
            }
        }

        else if (objectFU.name.Contains("Road Control"))
        {
            if (isRotating == false)
            {
                road = objectFU;
                stop = false;
                tmd = 0;
                Vector3 movement = transform.forward * speed * Time.deltaTime;
                transform.Translate(movement, Space.World);
                if (objectFU.transform.position.z == objectLFU.transform.position.z && Mathf.Abs(transform.position.x - (objectLFU.transform.position.x + objectFU.transform.position.x) / 2) > 2.5 || objectFU.transform.position.x == objectLFU.transform.position.x && Mathf.Abs(transform.position.z - (objectFU.transform.position.z + objectLFU.transform.position.z) / 2) > 2.5)
                {
                    transform.Translate(-transform.right * 0.1f, Space.World);
                }
                if (objectFU.transform.position.z == objectLFU.transform.position.z && Mathf.Abs(transform.position.x - (objectLFU.transform.position.x + objectFU.transform.position.x) / 2) < 2.5 || objectFU.transform.position.x == objectLFU.transform.position.x && Mathf.Abs(transform.position.z - (objectFU.transform.position.z + objectLFU.transform.position.z) / 2) < 2.5)
                {
                    transform.Translate(transform.right * 0.1f, Space.World);
                }
            }
        }
        else if (objectFU.name.Contains("nterSection") || objectFU.name.Contains("Corner"))
        {
            stop = false;
            if (isRotating == true)
            {
                return;
            }
            randomInt = 0;
            tmd = 10;
            List<int> damn = new List<int>();
            if (objectFU.name.Contains("nterSection"))
            {
                int getIn = road.transform.GetComponent<RoudControl>().target[1];
                int getOut = 0;
                List<int> tmp = new List<int>();
                Vector3 localPosition = transform.InverseTransformPoint(targetPosition);
                if (-localPosition.x <= localPosition.z && localPosition.x >= localPosition.z)
                {
                    randomInt = 1;
                    getOut = (getIn - 1 - 1) % 4 + 1;
                    if (localPosition.z >= 0)
                    {
                        tmp.Add(1);
                        tmp.Add(0);
                        tmp.Add(-2);
                        tmp.Add(-1);
                    }
                    else
                    {
                        tmp.Add(1);
                        tmp.Add(-2);
                        tmp.Add(0);
                        tmp.Add(-1);
                    }
                }
                else if (localPosition.x <= localPosition.z && -localPosition.x >= localPosition.z)
                {
                    randomInt = -1;
                    getOut = (getIn - 1 + 1) % 4 + 1;
                    if (localPosition.z >= 0)
                    {
                        tmp.Add(-1);
                        tmp.Add(0);
                        tmp.Add(-2);
                        tmp.Add(1);
                    }
                    else
                    {
                        tmp.Add(-1);
                        tmp.Add(-2);
                        tmp.Add(0);
                        tmp.Add(1);
                    }
                }
                else if (localPosition.x < localPosition.z && -localPosition.x < localPosition.z)
                {
                    randomInt = 0;
                    getOut = (getIn - 1 + 2) % 4 + 1;
                    if (localPosition.x >= 0)
                    {
                        tmp.Add(0);
                        tmp.Add(1);
                        tmp.Add(-1);
                        tmp.Add(-2);
                    }
                    else
                    {
                        tmp.Add(0);
                        tmp.Add(-1);
                        tmp.Add(1);
                        tmp.Add(-2);
                    }
                }
                else
                {
                    randomInt = -2;
                    getOut = getIn;
                    if (localPosition.x >= 0)
                    {
                        tmp.Add(-2);
                        tmp.Add(1);
                        tmp.Add(-1);
                        tmp.Add(0);
                    }
                    else
                    {
                        tmp.Add(-2);
                        tmp.Add(-1);
                        tmp.Add(1);
                        tmp.Add(0);
                    }
                }
                int tmc = 0;
                int outB = 0;
                foreach (int tm in tmp)
                {
                    foreach (GameObject gou in objectFU.transform.GetComponent<SpecialRouting>().roadOut)
                    {
                        tmc = -tm;
                        if (tm == -2)
                        {
                            tmc = 4;
                        }
                        if (tm == 0)
                        {
                            tmc = 2;
                        }
                        getOut = (getIn - 1 + tmc) % 4 + 1;
                        if (gou.GetComponent<RoudControl>().start[1] == getOut)
                        {
                            if (gou.GetComponent<RoudControl>().control[0] == 1)
                            {
                                outB = 1;
                                tmd = tm;
                                if (tmd != 1)
                                {
                                    foreach (GameObject ling in objectFU.GetComponent<SpecialRouting>().sign)
                                    {
                                        if (ling.GetComponent<SignalScript>().roudNum == getIn && ling.GetComponent<SignalScript>().signal == 0)
                                        {
                                            tmd = 11;
                                        }
                                    }
                                }
                                damn.Add(tmd);
                                //break;
                            }

                        }
                    }
                    /*if (outB == 1)
                    {
                        break;
                    }*/
                }
                /*if (objectBelow.transform.forward == -transform.forward)
                {
                    randomInt = Random.Range(-1, 0);
                    if (randomInt == 0)
                    {
                        randomInt = 1;
                    }
                }
                else if (objectBelow.transform.right == transform.forward)
                {
                    randomInt = Random.Range(-1, 0);
                }
                else
                {
                    randomInt = Random.Range(0, 1);
                }*/
            }
            else if (objectFU.name.Contains("Corner"))
            {
                if (transform.forward == objectFU.transform.right)
                {
                    tmd = -1;
                }
                else
                {
                    tmd = 1;
                }
                damn.Add(tmd);
            }
            if (transform.name.Contains("Special"))
            {
                tmd = 10;
                //�����������⳵��
                if (name.Contains("SpecialF"))
                {

                    foreach (GameObject gou in objectFU.transform.GetComponent<SpecialRouting>().roadOut)
                    {
                        foreach (int FC in gou.GetComponent<RoudControl>().controlF)
                        {
                            if (num == FC)
                            {
                                tmd = -(gou.GetComponent<RoudControl>().start[1] - road.transform.GetComponent<RoudControl>().target[1]) % 4;
                                if (tmd == -2)
                                {
                                    tmd = 0;
                                }
                                if (tmd == 0)
                                {
                                    tmd = -2;
                                }
                                break;
                            }
                        }
                        if (tmd != 10)
                        {
                            break;
                        }
                    }
                }
                if (name.Contains("SpecialY"))
                {

                    foreach (GameObject gou in objectFU.transform.GetComponent<SpecialRouting>().roadOut)
                    {
                        foreach (int YC in gou.GetComponent<RoudControl>().controlY)
                        {
                            if (num == YC)
                            {
                                tmd = -(gou.GetComponent<RoudControl>().start[1] - road.transform.GetComponent<RoudControl>().target[1]) % 4;
                                if (tmd == -2)
                                {
                                    tmd = 0;
                                }
                                if (tmd == 0)
                                {
                                    tmd = -2;
                                }
                                break;
                            }
                        }
                        if (tmd != 10)
                        {
                            break;
                        }
                    }
                }
                if (name.Contains("SpecialS"))
                {

                    foreach (GameObject gou in objectFU.transform.GetComponent<SpecialRouting>().roadOut)
                    {
                        foreach (int SC in gou.GetComponent<RoudControl>().controlS)
                        {
                            if (num == SC)
                            {
                                tmd = -(gou.GetComponent<RoudControl>().start[1] - road.transform.GetComponent<RoudControl>().target[1]) % 4;
                                if (tmd == -2)
                                {
                                    tmd = 0;
                                }
                                if (tmd == 0)
                                {
                                    tmd = -2;
                                }
                                break;
                            }
                        }
                        if (tmd != 10)
                        {
                            break;
                        }
                    }
                }
                StartCoroutine(RotateCoroutine(90f * tmd));
            }
            else
            {
                if (damn.Count < 2)
                {
                    damn.Add(damn[0]);
                }
                tmd = damn[0];
                if (action<2)
                {
                    tmd = damn[action];
                }
                if (isRotating == false && objectUp.transform.name.Contains("Road") && tmd != 11)
                {
                    StartCoroutine(RotateCoroutine(90f * tmd));
                }

            }
        }
        direction = tmd;
        if (stop || tmd==11)
        {
            if (objectUp.name.Contains("Roud Control"))
            {
                int exist = 0;
                foreach (GameObject eac in objectUp.GetComponent<RoudControl>().stopCar)
                {
                    if (eac == gameObject)
                    {
                        exist = 1;
                    }
                }
                if (exist == 0)
                {
                    objectUp.GetComponent<RoudControl>().stopCar.Add(gameObject);
                    Debug.Log("special car stopped!");
                }
            }
        }
        else
        {
            if (objectUp.name.Contains("Roud Control"))
            {
                foreach (GameObject eac in objectUp.GetComponent<RoudControl>().stopCar)
                {
                    if (eac == gameObject)
                    {
                        objectUp.GetComponent<RoudControl>().stopCar.Remove(eac);
                    }
                }
            }
        }
    }
}
