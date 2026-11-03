using System.Collections;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
public class SkeletonAI : MonoBehaviour
{
    [Header("Objetivo")]
    // Objetivo
    public GameObject target;

    [Header("Distancias de Decisión")]
    // Distancia a la que queremos estar del jugador. Si estamos más lejos, vamos hacia él y si estamos más cerca, huimos
    public float safeDistance = 8f;
    public float tolerance = 0.5f;

    [Header("Parámetros de Movimiento")]
    // Velocidad y aceleración que aplicamos al NavMeshAgent
    public float maxSpeed = 4f;
    public float acceleration = 8f;
    public float angularSpeed = 360f;

    public float deceleration = 12f;
    public float turnAcceleration = 20f;
    public float maxTurnSpeed = 8f;
    public float maxAngleToMove = 60f;

    [Header("Comportamiento NavMesh")]
    // Cada cuánto recalculamos el destino en Seek/Flee para no saturar con llamadas de repath continuo
    public float repathInterval = 0.2f;
    // Distancia hacia la que proyectamos el destino de huida en el NavMesh
    public float fleeDistance = 6f;

    [Header("NavMesh Link Jump")]
    public float jumpHeight = 1.5f;
    public float jumpDuration = 0.8f;

    [Header("Animación")]
    // Referencia al Animator para sincronizar la velocidad de la animación
    public Animator animator;

    // Los tres estados posibles según la distancia al objetivo
    enum Mode { Idle, Seek, Flee }
    Mode mode = Mode.Idle;

    NavMeshAgent agent;
    float nextRepathTime;
    bool isTraversingLink = false;

    static readonly int SpeedMagnitudeHash = Animator.StringToHash("SpeedMagnitude");

    // Ángulos que evaluamos al buscar un punto de huida en el NavMesh (0 = lado contrario al objetivo)
    static readonly float[] fleeAngles = { 0f, 30f, -30f, 60f, -60f, 90f, -90f };

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        agent.speed = maxSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = angularSpeed;

        agent.autoTraverseOffMeshLink = false;
    }

    void Update()
    {
        if (target == null || !agent.isOnNavMesh) return;

        
        if (agent.isOnOffMeshLink && !isTraversingLink)
        {
            StartCoroutine(TraverseOffMeshLink());
            return;
        }

        if (isTraversingLink) return;

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        // Determinamos el modo (Seek, Flee o Idle) en función de las zonas
        Mode newMode;
        if (distance > safeDistance + tolerance) newMode = Mode.Seek;
        else if (distance < safeDistance - tolerance) newMode = Mode.Flee;
        else newMode = Mode.Idle;

        bool modeChanged = newMode != mode;
        mode = newMode;

        if (mode == Mode.Idle)
        {
            // Zona neutra: detenemos el camino y nos encaramos al jugador
            if (modeChanged) agent.ResetPath();
            FaceTarget(toTarget);
        }
        else if (modeChanged || Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + repathInterval;

            if (mode == Mode.Seek) Seek();
            else Flee();
        }

        SetAnimation();
    }

    // Seek: el destino es la posición del jugador en el NavMesh
    void Seek()
    {
        if (NavMesh.SamplePosition(target.transform.position, out NavMeshHit hit, 2f, agent.areaMask))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(target.transform.position);
        }
    }

    // Flee: calcula un punto de escape en el NavMesh en dirección opuesta al jugador
    void Flee()
    {
        Vector3 pos = transform.position;
        Vector3 targetPos = target.transform.position;

        Vector3 away = pos - targetPos;
        away.y = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : transform.forward;

        float bestDistance = FlatDistance(pos, targetPos);
        Vector3 bestPoint = pos;
        bool found = false;

        foreach (float angle in fleeAngles)
        {
            Vector3 candidate = pos + Quaternion.Euler(0f, angle, 0f) * away * fleeDistance;

            // NavMesh.Raycast comprueba si hay un borde o pared en el camino; si lo hay, devuelve el punto límite
            Vector3 reachable = NavMesh.Raycast(pos, candidate, out NavMeshHit hit, agent.areaMask)
                ? hit.position
                : candidate;

            float d = FlatDistance(reachable, targetPos);
            if (d > bestDistance)
            {
                bestDistance = d;
                bestPoint = reachable;
                found = true;
            }
        }

        if (found) agent.SetDestination(bestPoint);
        else agent.ResetPath(); // Si está acorralado sin salida
    }

    // Cruce del NavMeshLink
    IEnumerator TraverseOffMeshLink()
    {
        isTraversingLink = true;

        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;

        Vector3 jumpDir = endPos - startPos;
        jumpDir.y = 0f;
        if (jumpDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(jumpDir);
            float rotTimer = 0f;
            while (rotTimer < 0.15f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotTimer / 0.15f);
                rotTimer += Time.deltaTime;
                yield return null;
            }
            transform.rotation = targetRotation;
        }

        if (animator != null)
            animator.SetFloat(SpeedMagnitudeHash, maxSpeed);

      
        float timer = 0f;
        while (timer < jumpDuration)
        {
            float t = timer / jumpDuration;
            float yOffset = 4f * jumpHeight * t * (1f - t);

            transform.position = Vector3.Lerp(startPos, endPos, t) + Vector3.up * yOffset;

            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;

        agent.CompleteOffMeshLink();
        isTraversingLink = false;
    }

    // Gira al personaje hacia el objetivo cuando está en modo Idle
    void FaceTarget(Vector3 toTarget)
    {
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(toTarget);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, angularSpeed * Time.deltaTime);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // Envía la velocidad actual al Animator para las transiciones del Blend Tree
    void SetAnimation()
    {
        if (animator != null && agent != null)
            animator.SetFloat(SpeedMagnitudeHash, agent.velocity.magnitude);
    }

    // Dibuja las zonas de decisión en la Scene View al seleccionar el esqueleto
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, safeDistance + tolerance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, safeDistance - tolerance);
    }
}