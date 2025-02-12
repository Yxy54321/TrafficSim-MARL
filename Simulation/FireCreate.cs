using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireCreate : MonoBehaviour
{
    public float speed = 0.01f;
    public float fireStation = 0;
    public int hurtNum = 0;
    public GameObject fireDepart;
    private int count = 0;
    private int count2 = 0;
    // Start is called before the first frame update
    void Start()
    {
        fireDepart.GetComponent<fireDepartment>().firePlace.Add(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        count2++;
        if (count > 60)
        {
            count = 0;
            if (fireStation < 1f)
            {
                fireStation += speed;
            }
            else
            {
                fireStation = 1f;
            }
            float randomNum = Random.Range(0, 1);
            float possibility = fireStation * 0.7f;
            if (count2 > 20f * 60)
            {
                count2 = 0;
                hurtNum++;
            }
            if (randomNum < possibility)
            {
                GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

                // 遍历根对象
                foreach (GameObject rootObject in rootObjects)
                {
                    if (rootObject.name.Contains("Building") && rootObject.GetComponent<FireCreate>() == null && Vector3.Distance(transform.position, rootObject.transform.position) < 22)
                    {
                        rootObject.AddComponent<FireCreate>();
                        break;
                    }
                }
            }
        }
        else
        {
            count++;
        }
    }
}
