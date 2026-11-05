using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CopPatrol : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float waitTimeAtPoint = 8f;
    public float arrivalThreshold = 0.5f;

    private NavMeshAgent agent;
    private Animator animator;
    private int currentPointIndex = 0;
    private float waitTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        waitTimer = waitTimeAtPoint;
        if (patrolPoints.Length > 0 && patrolPoints[0] != null)
            agent.SetDestination(patrolPoints[0].position);
    }

    void Update()
    {
        if (patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= arrivalThreshold)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPointIndex].position);
                waitTimer = waitTimeAtPoint;
            }
        }

        if (animator != null)
        {
            float speed = agent.velocity.magnitude;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Float)
                    animator.SetFloat(param.name, speed);
            }
        }
    }
}