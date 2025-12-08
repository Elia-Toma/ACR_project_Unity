using RosMessageTypes.Nav;
using RosMessageTypes.Std;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [Header("Bounds")]
    public Transform boundsTransform;
    public Vector3 boundsSize = new Vector3(40f, 2f, 20f);

    [Header("Grid")]
    public float cellSize = 0.5f;
    public LayerMask obstacleLayer;

    [Header("Simulation Configuration")]
    public string configTopic = "/simulation/config";
    public LayerMask shelfLayer;
    public LayerMask robotLayer;
    public LayerMask deliveryLayer;
    public int packagesPerShelf = 5;

    // Internal grid
    private int[,] occupancyGrid;
    private int gridWidth, gridHeight;
    private Vector3 originWorld;

    // ROS
    private ROSConnection ros;
    public string mapTopic = "/map";

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        ros.RegisterPublisher<OccupancyGridMsg>(mapTopic);
        ros.RegisterPublisher<StringMsg>(configTopic);

        GenerateGrid();
        StartCoroutine(PublishSequence());
    }

    private System.Collections.IEnumerator PublishSequence()
    {
        // First publish the map
        PublishOccupancyGrid();
        
        // Wait a short delay to ensure map is received before config
        yield return new WaitForSeconds(0.5f);
        
        // Then publish the simulation config
        PublishSimulationConfig();
    }

    public void GenerateGrid()
    {
        Vector3 center = boundsTransform != null ? boundsTransform.position : transform.position;
        Vector3 size = boundsSize;

        originWorld = new Vector3(center.x - size.x / 2f,
                                  center.y,
                                  center.z - size.z / 2f);

        gridWidth = Mathf.CeilToInt(size.x / cellSize);
        gridHeight = Mathf.CeilToInt(size.z / cellSize);
        occupancyGrid = new int[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 cellCenter = GridToWorld(x, z);
                float half = cellSize * 0.45f;
                Vector3 boxHalf = new Vector3(half, size.y / 2f, half);

                Collider[] hits = Physics.OverlapBox(
                    cellCenter + Vector3.up * (size.y / 2f - 0.01f),
                    boxHalf,
                    Quaternion.identity,
                    obstacleLayer
                );

                occupancyGrid[x, z] = hits.Length > 0 ? 1 : 0;
            }
        }

        UnityEngine.Debug.Log($"[GridGenerator] Grid generated: {gridWidth}x{gridHeight}");
    }

    public void PublishOccupancyGrid()
    {
        int totalCells = gridWidth * gridHeight;
        sbyte[] data = new sbyte[totalCells];

        // Convert 2D → ROS OccupancyGrid (row-major)
        for (int z = 0; z < gridHeight; z++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                int idx = z * gridWidth + x;

                if (occupancyGrid[x, z] == 1)
                    data[idx] = 100;   // Occupied
                else
                    data[idx] = 0;     // Free
            }
        }

        // ROS message construction
        MapMetaDataMsg meta = new MapMetaDataMsg
        {
            resolution = cellSize,
            width = (uint)gridWidth,
            height = (uint)gridHeight,
            origin = new RosMessageTypes.Geometry.PoseMsg
            {
                position = new RosMessageTypes.Geometry.PointMsg(originWorld.x, 0.0, originWorld.z),
                orientation = new RosMessageTypes.Geometry.QuaternionMsg(0, 0, 0, 1)
            }
        };

        OccupancyGridMsg map = new OccupancyGridMsg
        {
            header = new HeaderMsg
            {
                frame_id = "map",
                stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg()
            },
            info = meta,
            data = data
        };

        ros.Publish(mapTopic, map);
        UnityEngine.Debug.Log("[GridGenerator] OccupancyGrid published on " + mapTopic);
    }

    public void PublishSimulationConfig()
    {
        // Get layer indices from LayerMask
        int shelfLayerIndex = (int)Mathf.Log(shelfLayer.value, 2);
        int robotLayerIndex = (int)Mathf.Log(robotLayer.value, 2);
        int deliveryLayerIndex = (int)Mathf.Log(deliveryLayer.value, 2);

        // Find all GameObjects in scene
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        // Detect Shelves by layer - get unique root objects only
        var shelfObjects = allObjects
            .Where(obj => obj.layer == shelfLayerIndex)
            .Select(obj => obj.transform.root.gameObject)  // Get root of hierarchy
            .Distinct()  // Remove duplicates (children pointing to same root)
            .OrderBy(g => g.name)
            .ToList();

        List<ShelfConfig> shelfList = new List<ShelfConfig>();
        for (int i = 0; i < shelfObjects.Count; i++)
        {
            GameObject s = shelfObjects[i];
            shelfList.Add(new ShelfConfig
            {
                id = i + 1,
                x = s.transform.position.x,
                z = s.transform.position.z,
                count = packagesPerShelf
            });
        }
        UnityEngine.Debug.Log($"[GridGenerator] Found {shelfList.Count} shelves by layer");

        // Detect Robots by layer - get unique root objects only
        var robotObjects = allObjects
            .Where(obj => obj.layer == robotLayerIndex)
            .Select(obj => obj.transform.root.gameObject)  // Get root of hierarchy
            .Distinct()  // Remove duplicates
            .ToList();

        List<RobotConfig> robotList = new List<RobotConfig>();
        foreach (var r in robotObjects)
        {
            robotList.Add(new RobotConfig
            {
                name = r.name,
                x = r.transform.position.x,
                z = r.transform.position.z
            });
        }
        UnityEngine.Debug.Log($"[GridGenerator] Found {robotList.Count} robots by layer");

        // Detect Delivery points by layer - get unique root objects
        var deliveryObjects = allObjects
            .Where(obj => obj.layer == deliveryLayerIndex)
            .Select(obj => obj.transform.root.gameObject)
            .Distinct()
            .ToList();

        List<DeliveryConfig> deliveryList = new List<DeliveryConfig>();
        foreach (var d in deliveryObjects)
        {
            deliveryList.Add(new DeliveryConfig
            {
                x = d.transform.position.x,
                z = d.transform.position.z
            });
        }
        UnityEngine.Debug.Log($"[GridGenerator] Found {deliveryList.Count} delivery points");

        if (deliveryList.Count == 0)
        {
            // Provide a default delivery config if none found
            deliveryList.Add(new DeliveryConfig { x = 0.0f, z = 0.0f });
            UnityEngine.Debug.LogWarning("[GridGenerator] No delivery points found, using default (0, 0)");
        }

        // Build Config Object
        SimulationConfig config = new SimulationConfig
        {
            shelves = shelfList,
            robots = robotList,
            delivery_points = deliveryList
        };

        string json = JsonUtility.ToJson(config);
        
        StringMsg msg = new StringMsg(json);
        ros.Publish(configTopic, msg);
        UnityEngine.Debug.Log($"[GridGenerator] Simulation Config published to {configTopic}: {json}");
    }

    public Vector3 GridToWorld(int gx, int gz)
    {
        float worldX = originWorld.x + (gx + 0.5f) * cellSize;
        float worldZ = originWorld.z + (gz + 0.5f) * cellSize;
        return new Vector3(worldX, originWorld.y, worldZ);
    }
}

// Data Structures for JSON Serialization
[Serializable]
public class SimulationConfig
{
    public List<ShelfConfig> shelves;
    public List<RobotConfig> robots;
    public List<DeliveryConfig> delivery_points;
}

[Serializable]
public class ShelfConfig
{
    public int id;
    public float x;
    public float z;
    public int count;
}

[Serializable]
public class RobotConfig
{
    public string name;
    public float x;
    public float z;
}

[Serializable]
public class DeliveryConfig
{
    public float x;
    public float z;
}
