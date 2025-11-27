using UnityEngine;
using UnityEngine.AI; // Necessario per muovere il robot
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std; // Usiamo stringhe semplici per i comandi

[RequireComponent(typeof(NavMeshAgent))]
public class PalletRobotSubscriber : MonoBehaviour
{
    [Header("Destinazioni (Trascina oggetti dalla scena)")]
    public Transform shelfA;
    public Transform shelfB;
    public Transform packingStation;
    public Transform homePosition;

    [Header("Configurazione ROS")]
    public string topicName = "/palletRobot/command";

    // Riferimenti interni
    private NavMeshAgent agent;
    private PalletRobotPublisher statusPublisher; // Riferimento all'altro script per inviare risposte

    void Start()
    {
        // 1. Ottieni i componenti
        agent = GetComponent<NavMeshAgent>();
        statusPublisher = GetComponent<PalletRobotPublisher>();

        // 2. Iscriviti al topic dei comandi
        ROSConnection.GetOrCreateInstance().Subscribe<StringMsg>(topicName, ExecuteCommand);
        
        Debug.Log("Robot pronto. In ascolto su: " + topicName);
    }

    // Callback: Eseguita quando ROS invia un comando
    void ExecuteCommand(StringMsg msg)
    {
        string command = msg.data;
        Debug.Log("Comando ricevuto: " + command);

        // Logica di navigazione diretta
        switch (command)
        {
            case "goto_shelf_a":
                MoveTo(shelfA.position, "MOVING_TO_SHELF_A");
                break;
            case "goto_shelf_b":
                MoveTo(shelfB.position, "MOVING_TO_SHELF_B");
                break;
            case "goto_packing":
                MoveTo(packingStation.position, "MOVING_TO_PACKING");
                break;
            case "goto_home":
                MoveTo(homePosition.position, "MOVING_HOME");
                break;
            case "pickup":
                PerformAction("PICKING_UP", 2.0f); // Simula 2 secondi di lavoro
                break;
            case "place":
                PerformAction("PLACING", 2.0f); // Simula 2 secondi di lavoro
                break;
            default:
                Debug.LogWarning("Comando sconosciuto: " + command);
                break;
        }
    }

    // Funzione per muovere il robot
    void MoveTo(Vector3 target, string statusMessage)
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            bool pathSet = agent.SetDestination(target);
            Debug.Log($"[MoveTo] Target: {target}, SetDestination result: {pathSet}, Agent Position: {transform.position}");
            
            if (!pathSet)
            {
                Debug.LogError($"[MoveTo] Failed to set destination to {target}. Check NavMesh connectivity or if target is reachable.");
            }

            // Usa l'altro script per dire a ROS che ci stiamo muovendo
            if(statusPublisher) statusPublisher.PublishStatus(statusMessage);
        }
        else
        {
            Debug.LogWarning("Cannot move: Agent is not valid, not active, or not on NavMesh.");
        }
    }

    // Funzione per simulare azioni (Pickup/Place)
    void PerformAction(string actionStatus, float duration)
    {
        // Ferma il robot se possibile
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
        
        // Comunica lo stato
        if(statusPublisher) statusPublisher.PublishStatus(actionStatus);

        // Simula il tempo di lavoro (invoca CompleteAction dopo 'duration' secondi)
        CancelInvoke("CompleteAction");
        Invoke("CompleteAction", duration);
    }

    // Chiamata quando un'azione o un movimento finisce
    void CompleteAction()
    {
        if(statusPublisher) statusPublisher.PublishStatus("TASK_COMPLETE");
    }

    void Update()
    {
        // Check if agent is valid, active, and on a NavMesh before accessing path properties
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        // Controlla se il robot è arrivato a destinazione
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                // Evita di spammare "Arrived" ogni frame, fallo solo una volta se necessario
                // Qui lasciamo la logica semplice.
            }
        }
    }
}