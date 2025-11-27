using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std; // Usiamo StringMsg per lo stato

public class PalletRobotPublisher : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "/palletRobot/command";

    void Start()
    {
        // Avvia la connessione
        ros = ROSConnection.GetOrCreateInstance();
        
        // Registra il publisher
        ros.RegisterPublisher<StringMsg>(topicName);
    }

    // Questa funzione ora è pubblica e viene chiamata dal Subscriber
    // per dire a ROS cosa sta succedendo (es. "TASK_COMPLETE")
    public void PublishStatus(string statusMessage)
    {
        StringMsg msg = new StringMsg(statusMessage);
        
        // Invia il messaggio a ROS
        ros.Publish(topicName, msg);
        
        Debug.Log($"[ROS INVIO] Stato inviato: {statusMessage}");
    }

    // Abbiamo rimosso l'Update con il timer casuale perché 
    // ora pubblichiamo solo quando succede qualcosa di reale.
}