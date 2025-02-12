using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

[Serializable]
public class LaneBoundsData
{
    public string laneId;
    public float minX;
    public float maxX;
    public float minZ;
    public float maxZ;

    public LaneBoundsData(string id, Bounds bounds)
    {
        laneId = id;
        minX = bounds.min.x;
        maxX = bounds.max.x;
        minZ = bounds.min.z;
        maxZ = bounds.max.z;
    }
}

public class RoadLaneBoundsManager : MonoBehaviour
{
    [SerializeField]
    private bool autoSaveOnStart = true;

    [SerializeField]
    private string roadTag = "Road";

    private const string DEFAULT_BOUNDS_FILE_PATH = "lane_bounds.json";
    private string boundsFilePath;
    private Dictionary<string, LaneBoundsData> laneBoundsCache;

    private void Awake()
    {
        LoadBoundsData();
    }

    private void Start()
    {
        if (autoSaveOnStart)
        {
            SaveAllRoadLanes();
        }
    }

    public void SetCustomSavePath(string relativePath)
    {
        if (!relativePath.EndsWith(".json"))
        {
            relativePath += ".json";
        }
        boundsFilePath = relativePath;
    }

    public void SaveAllRoadLanes()
    {
        GameObject[] roadLanes = GameObject.FindGameObjectsWithTag(roadTag);
        Debug.Log($"Found {roadLanes.Length} road lanes to save");

        if (laneBoundsCache == null)
        {
            laneBoundsCache = new Dictionary<string, LaneBoundsData>();
        }

        foreach (GameObject roadLane in roadLanes)
        {
            Renderer renderer = roadLane.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogWarning($"Road lane {roadLane.name} has no Renderer component");
                continue;
            }

            string laneId = roadLane.name;
            LaneBoundsData boundsData = new LaneBoundsData(laneId, renderer.bounds);
            laneBoundsCache[laneId] = boundsData;
            Debug.Log($"Saved bounds for lane: {laneId}");
        }

        SaveToFile();
        Debug.Log("All road lanes bounds data saved successfully");
    }

    private void LoadBoundsData()
    {
        string filePath;
        if (string.IsNullOrEmpty(boundsFilePath))
        {
            filePath = Path.Combine(Application.persistentDataPath, DEFAULT_BOUNDS_FILE_PATH);
        }
        else
        {
            filePath = Path.Combine(Application.dataPath, boundsFilePath);
        }

        if (!File.Exists(filePath))
        {
            Debug.Log($"No existing bounds data file found at: {filePath}");
            laneBoundsCache = new Dictionary<string, LaneBoundsData>();
            return;
        }

        try
        {
            string jsonData = File.ReadAllText(filePath, Encoding.UTF8);
            var boundsDataList = JsonConvert.DeserializeObject<List<LaneBoundsData>>(jsonData);

            laneBoundsCache = new Dictionary<string, LaneBoundsData>();
            foreach (var boundsData in boundsDataList)
            {
                laneBoundsCache[boundsData.laneId] = boundsData;
            }
            Debug.Log($"Successfully loaded bounds data from: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading bounds data: {e.Message}");
            laneBoundsCache = new Dictionary<string, LaneBoundsData>();
        }
    }

    private void SaveToFile()
    {
        string filePath;
        if (string.IsNullOrEmpty(boundsFilePath))
        {
            filePath = Path.Combine(Application.persistentDataPath, DEFAULT_BOUNDS_FILE_PATH);
        }
        else
        {
            filePath = Path.Combine(Application.dataPath, boundsFilePath);
        }
        //"C:/Users/31303/AppData/LocalLow/DefaultCompany/My project (4)\\lane_bounds.json"
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        try
        {
            var boundsDataList = new List<LaneBoundsData>(laneBoundsCache.Values);
            string jsonData = JsonConvert.SerializeObject(boundsDataList, Formatting.Indented);
            File.WriteAllText(filePath, jsonData, Encoding.UTF8);
            Debug.Log($"Successfully saved bounds data to: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving bounds data: {e.Message}");
        }
    }

    // 新增：检查坐标是否在任意车道上
    public bool IsPointInAnyRoadLane(Vector3 pointPosition)
    {
        

        if (laneBoundsCache == null)
        {
            Debug.LogWarning("No road lanes data loaded");
            return false;
        }

        foreach (var kvp in laneBoundsCache)
        {
            LaneBoundsData boundsData = kvp.Value;
            bool xInRange = pointPosition.x >= boundsData.minX && pointPosition.x <= boundsData.maxX;
            bool zInRange = pointPosition.z >= boundsData.minZ && pointPosition.z <= boundsData.maxZ;

            if (xInRange && zInRange)
            {
                return true;
            }
        }

        return false;
    }
}