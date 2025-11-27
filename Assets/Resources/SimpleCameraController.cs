using UnityEngine;

public class SimpleCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float movementSpeed = 10f;
    public float fastMovementSpeed = 50f;
    public float freeLookSensitivity = 3f;

    [Header("Controls")]
    public bool enableMovement = true;
    public bool enableRotation = true;

    private void Update()
    {
        if (!enableMovement) return;

        // --- Movimento (WASD + QE per Alto/Basso) ---
        var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var targetSpeed = fastMode ? fastMovementSpeed : movementSpeed;

        var position = transform.position;

        // Avanti/Indietro (W/S)
        if (Input.GetKey(KeyCode.W)) position += transform.forward * targetSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) position -= transform.forward * targetSpeed * Time.deltaTime;

        // Sinistra/Destra (A/D)
        if (Input.GetKey(KeyCode.A)) position -= transform.right * targetSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) position += transform.right * targetSpeed * Time.deltaTime;

        // Alto/Basso (Q/E)
        if (Input.GetKey(KeyCode.E)) position += transform.up * targetSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) position -= transform.up * targetSpeed * Time.deltaTime;

        transform.position = position;

        // --- Rotazione (Mouse con Tasto Destro premuto) ---
        if (enableRotation && Input.GetMouseButton(1))
        {
            // Nasconde il cursore mentre ruoti
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * freeLookSensitivity;
            float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * freeLookSensitivity;

            transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
        }
        else
        {
            // Mostra di nuovo il cursore quando lasci il tasto destro
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}