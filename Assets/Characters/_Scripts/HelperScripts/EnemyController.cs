using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public GameObject PlayerTarget { get; set; }
    [SerializeField]
    private GameObject m_playerReference;
    [SerializeField]
    private LayerMask m_playerLayerMask;
    [SerializeField]
    private float m_detectionRange = 10.0f;
    public Vector3 LastKnownPlayerPosition { get; set; }

    [Header("Seek Settings")]
    public float moveSpeed = 3.5f;
    public float turnSpeed = 5.0f;
    
    [Header("Flee Settings")]
    public float fleeDistance = 5.0f;
    
    private Animator m_animator;

    private void Start()
    {
        LastKnownPlayerPosition = transform.position;
        m_animator = GetComponentInChildren<Animator>();
    }
    
    private void Update()
    {
        RaycastHit hit;
        Vector3 direction = m_playerReference.transform.position - transform.position;
        Physics.Raycast(transform.position, direction, out hit, m_detectionRange, m_playerLayerMask);
        
        if (hit.collider != null && hit.collider.gameObject == m_playerReference)
        {
            PlayerTarget = hit.collider.gameObject;
            LastKnownPlayerPosition = PlayerTarget.transform.position;
        }
        else
        {
            PlayerTarget = null;
        }

        // Apply Seek/Flee Behavior
        if (PlayerTarget != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, PlayerTarget.transform.position);

            if (distanceToPlayer < fleeDistance)
            {
                Flee(PlayerTarget.transform.position);
            }
            else
            {
                Seek(PlayerTarget.transform.position);
            }
            
            if (m_animator != null)
            {
                m_animator.SetFloat("SpeedMagnitude", moveSpeed);
            }
        }
        else
        {
            if (m_animator != null)
            {
                m_animator.SetFloat("SpeedMagnitude", 0f);
            }
        }
    }

    private void Seek(Vector3 targetPos)
    {
        Vector3 direction = targetPos - transform.position;
        direction.y = 0f; // Keep movement on the floor
        
        if (direction.magnitude > 0.1f)
        {
            Vector3 movement = direction.normalized * moveSpeed;
            
            float angle = Mathf.Rad2Deg * Mathf.Atan2(movement.x, movement.z);
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * turnSpeed);
            transform.position += transform.forward.normalized * moveSpeed * Time.deltaTime;
        }
    }

    private void Flee(Vector3 targetPos)
    {
        // La dirección para huir es desde el objetivo hacia nosotros
        Vector3 direction = transform.position - targetPos;
        direction.y = 0f; // Keep movement on the floor
        
        if (direction.magnitude > 0.1f)
        {
            Vector3 movement = direction.normalized * moveSpeed;
            
            float angle = Mathf.Rad2Deg * Mathf.Atan2(movement.x, movement.z);
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * turnSpeed);
            transform.position += transform.forward.normalized * moveSpeed * Time.deltaTime;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (Application.isPlaying)
        {
            if (PlayerTarget != null)
                Gizmos.color = Color.green;
            if (m_playerReference != null)
            {
                Vector3 direction = m_playerReference.transform.position - transform.position;
                Gizmos.DrawLine(transform.position, m_playerReference.transform.position);
            }
        }
        Gizmos.DrawWireSphere(transform.position, m_detectionRange);

        // Visualizar el rango de huida en amarillo
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fleeDistance);
    }
}
