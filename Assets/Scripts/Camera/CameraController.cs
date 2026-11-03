using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // El jugador a seguir (Knight)
    public Vector3 offset = new Vector3(0, 1.5f, 0); // Offset para mirar hacia la cabeza/centro del jugador
    
    [Header("Camera Settings")]
    public float distance = 5.0f;
    public float xSpeed = 15.0f; // Reducido para ajustarse a Mouse.delta del Input System
    public float ySpeed = 15.0f; 
    
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;

    [Header("Collision Settings (Anti-Clipping)")]
    public LayerMask collisionMask = ~0; // Por defecto colisiona con todo
    public float cameraRadius = 0.3f; // Radio imaginario de la cámara para que no traspase paredes
    public float minDistance = 0.5f; // Distancia mínima a la que se puede acercar la cámara al jugador

    private float x = 0.0f;
    private float y = 0.0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        // Ocultar y bloquear el cursor en el centro de la pantalla
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Obtener inputs del mouse con el nuevo Input System
            if (Mouse.current != null)
            {
                x += Mouse.current.delta.x.ReadValue() * xSpeed * Time.deltaTime;
                y -= Mouse.current.delta.y.ReadValue() * ySpeed * Time.deltaTime;
            }

            // Limitar la rotación vertical
            y = ClampAngle(y, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(y, x, 0);
            
            // Punto al que la cámara quiere mirar (cabeza del jugador)
            Vector3 targetPivot = target.position + offset;
            
            // Posición ideal de la cámara si no hubiera paredes
            Vector3 desiredCameraPos = rotation * new Vector3(0.0f, 0.0f, -distance) + targetPivot;

            // --- SISTEMA ANTI-CLIPPING ---
            Vector3 direction = desiredCameraPos - targetPivot;
            float maxDistance = direction.magnitude;
            
            // Lanzamos una esfera imaginaria desde el jugador hacia la cámara
            if (Physics.SphereCast(targetPivot, cameraRadius, direction.normalized, out RaycastHit hit, maxDistance, collisionMask))
            {
                // Si la esfera choca con una pared, acercamos la cámara al punto del choque
                float hitDistance = Mathf.Clamp(hit.distance, minDistance, maxDistance);
                transform.position = targetPivot + direction.normalized * hitDistance;
            }
            else
            {
                // Si no hay paredes, la cámara va a su posición ideal
                transform.position = desiredCameraPos;
            }

            transform.rotation = rotation;
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
