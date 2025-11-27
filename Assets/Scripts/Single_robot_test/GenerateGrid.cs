using RosMessageTypes.Nav;
using RosMessageTypes.Std;
using System;
using System.Diagnostics;
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

        GenerateGrid();
        PublishOccupancyGrid();
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

    public Vector3 GridToWorld(int gx, int gz)
    {
        float worldX = originWorld.x + (gx + 0.5f) * cellSize;
        float worldZ = originWorld.z + (gz + 0.5f) * cellSize;
        return new Vector3(worldX, originWorld.y, worldZ);
    }
}
