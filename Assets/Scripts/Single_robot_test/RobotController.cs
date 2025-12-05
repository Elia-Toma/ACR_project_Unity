using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

public class RobotController : MonoBehaviour
{
    [SerializeField] private string robotName = "robot1";
    [SerializeField] private float positionSmoothTime = 0.1f; // Smoothing per transizioni fluide
    [SerializeField] private bool debugLogs = true;

    private ROSConnection rosConnection;
    private string poseTopicName;
    private Vector3 targetPosition;
    private Vector3 positionVelocity = Vector3.zero;
    private bool hasReceivedFirstPose = false;

    private void Start()
    {
        // Inizializza la connessione ROS
        rosConnection = ROSConnection.GetOrCreateInstance();
        poseTopicName = $"/{robotName}/pose";

        // Sottoscrivi al topic della posizione
        rosConnection.Subscribe<PoseStampedMsg>(poseTopicName, OnPoseReceived);

        if (debugLogs)
            Debug.Log($"[{robotName}] RobotController inizializzato. In ascolto su {poseTopicName}");

        // Inizializza la posizione target con la posizione attuale
        targetPosition = transform.position;
    }

    private void OnPoseReceived(PoseStampedMsg poseMsg)
    {
        if (poseMsg == null || poseMsg.pose == null)
        {
            Debug.LogWarning($"[{robotName}] PoseMsg ricevuto ma null");
            return;
        }

        // Estrai la posizione dal messaggio ROS
        // ROS usa X, Z (Y=0), Unity usa X, Y, Z
        // Mappiamo: ROS.X -> Unity.X, ROS.Z -> Unity.Z, mantenendo Y invariato
        float rosX = (float)poseMsg.pose.position.x;
        float rosZ = (float)poseMsg.pose.position.z;
        float unityY = transform.position.y; // Mantieni Y invariato

        targetPosition = new Vector3(rosX, unityY, rosZ);

        if (!hasReceivedFirstPose)
        {
            hasReceivedFirstPose = true;
            // Primo messaggio: posiziona immediatamente senza smoothing
            transform.position = targetPosition;
            positionVelocity = Vector3.zero;

            if (debugLogs)
                Debug.Log($"[{robotName}] Prima posizione ricevuta: ({rosX:F2}, {unityY:F2}, {rosZ:F2})");
        }
        else if (debugLogs)
        {
            Debug.Log($"[{robotName}] Posizione aggiornata: ({rosX:F2}, {unityY:F2}, {rosZ:F2})");
        }
    }

    private void Update()
    {
        if (!hasReceivedFirstPose)
            return;

        // Usa SmoothDamp per transizioni fluide verso la posizione target
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref positionVelocity,
            positionSmoothTime
        );
    }

    /// <summary>
    /// Cambia il nome del robot e riottiene la connessione al nuovo topic
    /// </summary>
    public void SetRobotName(string newRobotName)
    {
        if (rosConnection != null)
        {
            rosConnection.Unsubscribe(poseTopicName);
        }

        robotName = newRobotName;
        poseTopicName = $"/{robotName}/pose";
        hasReceivedFirstPose = false;
        positionVelocity = Vector3.zero;

        if (rosConnection != null)
        {
            rosConnection.Subscribe<PoseStampedMsg>(poseTopicName, OnPoseReceived);

            if (debugLogs)
                Debug.Log($"[{robotName}] Topic aggiornato a {poseTopicName}");
        }
    }
}
