using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

public class RobotController : MonoBehaviour
{
    [Header("ROS Configuration")]
    [SerializeField] private string robotName = "robot1";
    [SerializeField] private bool debugLogs = true;

    [Header("Movement")]
    [SerializeField] private float positionSmoothTime = 0.1f;

    [Header("Visual Feedback")]
    [Tooltip("Oggetto da mostrare quando il robot ha preso il pacco (es. cubo).")]
    [SerializeField] private GameObject packageVisual; 
    [Tooltip("Renderer per cambiare colore quando il robot lavora.")]
    [SerializeField] private Renderer statusRenderer;
    [SerializeField] private Color workingColor = Color.green;

    // ROS Vars
    private ROSConnection rosConnection;
    private string poseTopicName;
    private string stateTopicName;
    private string goalTopicName; // Nuovo topic da ascoltare

    // Movement Vars
    private Vector3 targetPosition;
    private Vector3 positionVelocity = Vector3.zero;
    private bool hasReceivedFirstPose = false;

    // Visual & Logic Vars
    private Color defaultColor;
    private int goalsReceivedCounter = 0; // Conta i viaggi durante un lavoro
    private bool isExecuting = false;

    private void Start()
    {
        // Setup Visuals
        if (packageVisual != null) packageVisual.SetActive(false);
        
        if (statusRenderer != null) 
        {
            defaultColor = statusRenderer.material.color;
        }
        else 
        {
            statusRenderer = GetComponentInChildren<Renderer>();
            if(statusRenderer != null) defaultColor = statusRenderer.material.color;
        }

        // Init ROS
        rosConnection = ROSConnection.GetOrCreateInstance();
        targetPosition = transform.position;
        
        SetupTopicsAndSubscribe();
    }

    private void SetupTopicsAndSubscribe()
    {
        poseTopicName = $"/{robotName}/pose";
        stateTopicName = $"/{robotName}/state";
        goalTopicName = $"/{robotName}/goal"; // Il robot pubblica qui dove vuole andare

        // Sottoscrizioni
        rosConnection.Subscribe<PoseStampedMsg>(poseTopicName, OnPoseReceived);
        rosConnection.Subscribe<StringMsg>(stateTopicName, OnStateReceived);
        rosConnection.Subscribe<PoseStampedMsg>(goalTopicName, OnGoalReceived); // Ascoltiamo i goal

        if (debugLogs)
            Debug.Log($"[{robotName}] In ascolto su Pose, State e Goal.");
    }

    // --- 1. LOGICA STATO ---
    private void OnStateReceived(StringMsg stateMsg)
    {
        if (stateMsg == null) return;
        string state = stateMsg.data;

        // Visual Reset for Idle or Charging
        if (state == "idle" || state == "charging")
        {
             // FINE LAVORO (Idle) o CHARGING: Resetta tutto
            goalsReceivedCounter = 0;
            if (packageVisual != null) packageVisual.SetActive(false);
            if (statusRenderer != null) statusRenderer.material.color = defaultColor;
            isExecuting = false;
        }
        else if (state == "retrieving" || state == "returning")
        {
            // Andando a prendere il pacco (retrieving) o tonando alla base (returning):
            // Colore attivo, MA niente pacco visibile
            isExecuting = true;
            if (packageVisual != null) packageVisual.SetActive(false);
            if (statusRenderer != null) statusRenderer.material.color = workingColor;
        }
        else if (state == "delivering")
        {
            // Trasportando il pacco: Colore attivo E pacco visibile
            isExecuting = true;
            if (packageVisual != null) packageVisual.SetActive(true);
            if (statusRenderer != null) statusRenderer.material.color = workingColor;
        }
        // "bidding" ignored or treated as no-op visual change
    }

    // --- 2. LOGICA GOAL (Il trucco sta qui) ---
    // --- 2. LOGICA GOAL (Deprecata/Semplificata) ---
    private void OnGoalReceived(PoseStampedMsg goalMsg)
    {
        // Non usiamo più i goal per indovinare lo stato del pacco.
        // Lo stato 'delivering' ci dice esplicitamente quando mostrarlo.
    }

    // --- 3. LOGICA MOVIMENTO (Invariata) ---
    private void OnPoseReceived(PoseStampedMsg poseMsg)
    {
        if (poseMsg == null || poseMsg.pose == null) return;

        float rosX = (float)poseMsg.pose.position.x;
        float rosZ = (float)poseMsg.pose.position.z;
        float unityY = transform.position.y;

        targetPosition = new Vector3(rosX, unityY, rosZ);

        if (!hasReceivedFirstPose)
        {
            hasReceivedFirstPose = true;
            transform.position = targetPosition;
        }
    }

    private void Update()
    {
        if (!hasReceivedFirstPose) return;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref positionVelocity,
            positionSmoothTime
        );
    }

    public void SetRobotName(string newRobotName)
    {
        if (rosConnection != null)
        {
            if (!string.IsNullOrEmpty(poseTopicName)) rosConnection.Unsubscribe(poseTopicName);
            if (!string.IsNullOrEmpty(stateTopicName)) rosConnection.Unsubscribe(stateTopicName);
            if (!string.IsNullOrEmpty(goalTopicName)) rosConnection.Unsubscribe(goalTopicName);
        }

        robotName = newRobotName;
        hasReceivedFirstPose = false;
        isExecuting = false;
        goalsReceivedCounter = 0;
        
        // Reset visuali immediato
        if (packageVisual != null) packageVisual.SetActive(false);
        if (statusRenderer != null) statusRenderer.material.color = defaultColor;

        if (rosConnection != null) SetupTopicsAndSubscribe();
    }
}