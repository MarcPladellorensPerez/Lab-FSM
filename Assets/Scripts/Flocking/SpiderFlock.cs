using UnityEngine;

public class SpiderFlock : MonoBehaviour
{
    [HideInInspector]
    public FlockManager myManager;

    [HideInInspector]
    public float speed;     // Velocidad actual

    // Lo decide el manager con SetLeader. Los líderes no siguen a nadie, los seguidores siguen al líder más cercano
    [HideInInspector]
    public bool isLeader;

    private Vector3 currentDirection;
    private float nextRulesTime;

    private Vector3 leaderTarget;
    private bool hasLeaderTarget;
    private Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Start()
    {
        if (myManager == null)
        {
            Debug.LogWarning(name + ": no tiene FlockManager asignado, se desactiva.", this);
            enabled = false;
            return;
        }

        //Velocidad inicial aleatoria basada en los límites del manager
        speed = Random.Range(myManager.minSpeed, myManager.maxSpeed);

        currentDirection = transform.forward;
        currentDirection.y = 0f;
        currentDirection.Normalize();

        nextRulesTime = Time.time + Random.Range(0f, myManager.rulesInterval);
    }

    // El manager llama a esto para convertir a la araña en líder o devolverla a seguidora
    public void SetLeader(bool value)
    {
        isLeader = value;

        // Si cambia de rol, que elija un punto nuevo la próxima vez
        hasLeaderTarget = false;

        // Los líderes se ven más grandes para distinguirlos
        transform.localScale = value ? baseScale * myManager.leaderScale : baseScale;
    }

    void Update()
    {
        if (Time.time >= nextRulesTime)
        {
            nextRulesTime = Time.time + myManager.rulesInterval;
            ApplyFlockingRules();
        }

        if (currentDirection != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(currentDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, myManager.rotationSpeed * Time.deltaTime);
        }

        transform.Translate(0f, 0f, speed * Time.deltaTime);
    }

    void ApplyFlockingRules()
    {
        // Los líderes no aplican las reglas del grupo: van a lo suyo
        if (isLeader)
        {
            ApplyLeaderRules();
            return;
        }

        FlockManager m = myManager;

        Vector3 cohesion = Vector3.zero;
        Vector3 align = Vector3.zero;
        Vector3 separation = Vector3.zero;
        float neighbourSpeed = 0f;
        int numNeighbours = 0;

        // Para guardar cuál es el líder más cercano
        SpiderFlock nearestLeader = null;
        Vector3 offsetToLeader = Vector3.zero;
        float nearestLeaderSqr = float.MaxValue;

        float neighbourSqr = m.neighbourDistance * m.neighbourDistance;
        float separationSqr = m.separationDistance * m.separationDistance;

        foreach (SpiderFlock other in m.allSpiders)
        {
            if (other == null || other == this) continue;

            Vector3 offset = other.transform.position - transform.position;
            offset.y = 0f;
            float sqrDistance = offset.sqrMagnitude;

            if (other.isLeader && sqrDistance < nearestLeaderSqr)
            {
                nearestLeaderSqr = sqrDistance;
                nearestLeader = other;
                offsetToLeader = offset;
            }

            if (sqrDistance > neighbourSqr) continue;

            // Cohesión: sumamos los vectores hacia cada vecino. Su media apunta al centro del grupo
            cohesion += offset;

            // Alineación: sumamos hacia dónde mira realmente cada vecino
            align += other.transform.forward;
            neighbourSpeed += other.speed;

            numNeighbours++;

            // Separación: empujamos en dirección contraria al vecino (por eso el -=, porque offset va hacia él).
            if (sqrDistance < separationSqr)
                separation -= offset / Mathf.Max(sqrDistance, 0.01f);
        }

        Vector3 direction = currentDirection;

        if (numNeighbours > 0)
        {
            cohesion /= numNeighbours;
            align /= numNeighbours;

            speed = Mathf.Clamp(neighbourSpeed / numNeighbours, m.minSpeed, m.maxSpeed);

           
            direction += cohesion.normalized * m.cohesionWeight
                       + align.normalized * m.alignWeight
                       + Vector3.ClampMagnitude(separation, 1f) * m.separationWeight;
        }

        if (nearestLeader != null)
            direction += offsetToLeader.normalized * m.followLeaderWeight;

        if (m.goal != null)
        {
            Vector3 toGoal = m.goal.position - transform.position;
            toGoal.y = 0f;
            direction += toGoal.normalized * m.goalWeight;
        }

        if (m.randomness > 0f)
            direction += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * m.randomness;

        if (m.bounded)
        {
            Vector3 fromCenter = transform.position - m.transform.position;
            fromCenter.y = 0f;

            if (Mathf.Abs(fromCenter.x) > m.roamLimits.x || Mathf.Abs(fromCenter.z) > m.roamLimits.z)
                direction = -fromCenter;
        }

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            currentDirection = direction.normalized;
    }

    void ApplyLeaderRules()
    {
        FlockManager m = myManager;

        Vector3 target;

        if (m.goal != null)
        {
            target = m.goal.position;
        }
        else
        {
            Vector3 toPoint = leaderTarget - transform.position;
            toPoint.y = 0f;

            if (!hasLeaderTarget || toPoint.magnitude < m.leaderArriveDistance)
            {
                leaderTarget = m.transform.position + new Vector3(
                    Random.Range(-m.roamLimits.x, m.roamLimits.x),
                    0f,
                    Random.Range(-m.roamLimits.z, m.roamLimits.z));
                hasLeaderTarget = true;
            }

            target = leaderTarget;
        }

        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (m.goal != null && direction.magnitude < m.leaderArriveDistance)
            speed = 0f;
        else
            speed = m.leaderSpeed;

        if (direction.sqrMagnitude > 0.0001f)
            currentDirection = direction.normalized;
    }
}