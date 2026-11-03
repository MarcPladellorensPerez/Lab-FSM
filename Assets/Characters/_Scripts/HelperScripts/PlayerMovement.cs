using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpSpeed = 8f;
    public float gravity = 20f;
    public float turnSmoothTime = 0.1f;

    [Header("References")]
    public Transform cameraTransform;
    [SerializeField] private Animator m_animator;
    
    private float turnSmoothVelocity;
    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    
    // Variable para controlar si el jugador puede moverse
    private bool canMove = true;
    
    // Enemigos en la escena para verificar si debemos huir
    private EnemyController[] m_enemies;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (m_animator == null) m_animator = GetComponent<Animator>();
        
        // Find all enemies in the scene to check their targets
        m_enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
    }

    void Update()
    {
        // Si no podemos movernos, solo aplicamos gravedad y detenemos el resto
        if (!canMove)
        {
            moveDirection.x = 0f;
            moveDirection.z = 0f;
            moveDirection.y -= gravity * Time.deltaTime;
            controller.Move(moveDirection * Time.deltaTime);
            
            if (m_animator != null) m_animator.SetFloat("SpeedMagnitude", 0f);
            return;
        }

        // 1. Obtener los inputs del teclado (W, A, S, D) usando el Nuevo Sistema de Inputs
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) horizontal = -1f;
            if (Keyboard.current.dKey.isPressed) horizontal = 1f;
            if (Keyboard.current.sKey.isPressed) vertical = -1f;
            if (Keyboard.current.wKey.isPressed) vertical = 1f;
        }

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
        
        // --- Flee Logic ---
        Vector3 fleeDirection = Vector3.zero;
        int fleeingFromCount = 0;
        bool isFleeing = false;

        foreach (var enemy in m_enemies)
        {
            if (enemy != null && enemy.PlayerTarget == this.gameObject)
            {
                // Vector Flee = Posicion(Player) - Posicion(Enemy)
                Vector3 dirFromEnemy = transform.position - enemy.transform.position;
                dirFromEnemy.y = 0f;
                fleeDirection += dirFromEnemy.normalized;
                fleeingFromCount++;
            }
        }

        if (fleeingFromCount > 0)
        {
            isFleeing = true;
            fleeDirection = (fleeDirection / fleeingFromCount).normalized;
        }

        if (controller.isGrounded)
        {
            if (isFleeing)
            {
                // Automatic Flee Movement
                float targetAngle = Mathf.Atan2(fleeDirection.x, fleeDirection.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                moveDirection = moveDir.normalized * moveSpeed;
            }
            else if (inputDirection.magnitude >= 0.1f)
            {
                // Manual Movement
                float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
                if (cameraTransform != null)
                {
                    targetAngle += cameraTransform.eulerAngles.y;
                }
                
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                
                // Rotar el personaje
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                // Calcular dirección de movimiento
                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                moveDirection = moveDir.normalized * moveSpeed;
            }
            else
            {
                moveDirection = Vector3.zero;
            }

            // 2. Saltar (Espacio)
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                moveDirection.y = jumpSpeed;
            }
        }

        // Aplicar gravedad
        moveDirection.y -= gravity * Time.deltaTime;

        // Mover el controlador
        controller.Move(moveDirection * Time.deltaTime);
        
        // Actualizar Animador
        if (m_animator != null)
        {
            float targetSpeed = 0f;
            if (isFleeing || inputDirection.magnitude > 0.1f)
            {
                targetSpeed = moveSpeed;
            }
            
            float currentAnimSpeed = m_animator.GetFloat("SpeedMagnitude");
            float newAnimSpeed = Mathf.Lerp(currentAnimSpeed, targetSpeed, Time.deltaTime * 15f);
            
            m_animator.SetFloat("SpeedMagnitude", newAnimSpeed);
        }
    }

    // Método que llama el HitHandler cuando el personaje recibe daño
    public void StopMovement()
    {
        canMove = false;
        moveDirection = Vector3.zero;
    }
    
    // Opcional: si necesitas que se vuelva a mover después de un tiempo
    public void ResumeMovement()
    {
        canMove = true;
    }
}
