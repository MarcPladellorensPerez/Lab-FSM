using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(BehaviorGraphAgent))]
public class RobberFSM : MonoBehaviour
{
    public enum State
    {
        Wander,
        Robbery,
        Hiding,
        Hidden
    }

    [Header("Estado Actual (Debug)")]
    public State currentState = State.Wander;

    [Header("Configuración de Wander y Refugios")]
    public float wanderRadius = 8f;
    public float wanderChangeInterval = 3f;
    public float arrivalThreshold = 0.6f;
    public float retryCooldown = 2.0f; // Espera de 2 segundos tras interrupción o fallo
    public List<Transform> refugePoints = new List<Transform>();

    private NavMeshAgent navAgent;
    private BehaviorGraphAgent behaviorAgent;

    private float wanderTimer;
    private float cooldownTimer;

    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        behaviorAgent = GetComponent<BehaviorGraphAgent>();
    }

    void Start()
    {
        // Aseguramos que el BehaviorGraphAgent no arranque solo hasta entrar en Robbery
        behaviorAgent.End();
        EnterState(State.Wander);
    }

    void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        switch (currentState)
        {
            case State.Wander:
                UpdateWander();
                break;
            case State.Robbery:
                UpdateRobbery();
                break;
            case State.Hiding:
                UpdateHiding();
                break;
            case State.Hidden:
                UpdateHidden();
                break;
        }
    }

    private void EnterState(State newState)
    {
        currentState = newState;

        switch (newState)
        {
            case State.Wander:
                navAgent.speed = GetBlackboardFloat("WanderSpeed", 2.0f);
                navAgent.isStopped = false;
                wanderTimer = 0f;
                break;

            case State.Robbery:
                navAgent.speed = GetBlackboardFloat("RobberySpeed", 4.5f);
                navAgent.isStopped = false;
                SyncResourceAvailability();
                behaviorAgent.Restart(); // Inicia el grafo de Robbery
                break;

            case State.Hiding:
                navAgent.speed = GetBlackboardFloat("RobberySpeed", 4.5f);
                navAgent.isStopped = false;
                Transform closestRefuge = GetClosestRefuge();
                if (closestRefuge != null)
                {
                    SetBlackboardVariable("CurrentRefuge", closestRefuge);
                    navAgent.SetDestination(closestRefuge.position);
                }
                break;

            case State.Hidden:
                navAgent.ResetPath();
                navAgent.isStopped = true;
                navAgent.velocity = Vector3.zero;
                break;
        }
    }

    // --- LÓGICA DE ESTADOS ---

    private void UpdateWander()
    {
        // Deambular lentamente por el escenario
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f || (!navAgent.pathPending && navAgent.remainingDistance <= arrivalThreshold))
        {
            Vector3 randomDest = GetRandomNavMeshPoint(transform.position, wanderRadius);
            navAgent.SetDestination(randomDest);
            wanderTimer = wanderChangeInterval;
        }

        // Transición Wander -> Robbery: Policia lejos y tiempo de espera (2s) cumplido
        if (cooldownTimer <= 0f && IsCopFar())
        {
            EnterState(State.Robbery);
        }
    }

    private void UpdateRobbery()
    {
        // 1. Transición Robbery -> Hiding: Tesoro recogido
        if (GetBlackboardBool("HasTreasure"))
        {
            behaviorAgent.End();
            EnterState(State.Hiding);
            return;
        }

        // 2. Transición Robbery -> Wander: El policía vuelve antes de completar el robo
        if (!IsCopFar())
        {
            InterruptRobbery();
            return;
        }

        // 3. Transición Robbery -> Wander: El plan en el grafo ha fallado (ej. sin recursos)
        if (behaviorAgent.Graph != null && !behaviorAgent.Graph.IsRunning)
        {
            InterruptRobbery();
        }
    }

    private void UpdateHiding()
    {
        // Transición Hiding -> Hidden: Llega al refugio más cercano
        if (!navAgent.pathPending && navAgent.remainingDistance <= arrivalThreshold)
        {
            EnterState(State.Hidden);
        }
    }

    private void UpdateHidden()
    {
        // Se queda detenido en el refugio
    }

    // --- GESTIÓN DE INTERRUPCIONES Y BLACKBOARD ---

    private void InterruptRobbery()
    {
        // Detenemos el grafo y cancelamos el desplazamiento actual
        // No reseteamos HasKey, HasTool ni DoorOpen para conservar el inventario y estado de la puerta
        behaviorAgent.End();
        navAgent.ResetPath();

        cooldownTimer = retryCooldown; // Esperar 2 segundos antes de volver a intentarlo
        EnterState(State.Wander);
    }

    private void SyncResourceAvailability()
    {
        // En el siguiente intento comprueba de nuevo las condiciones:
        // Una vía está disponible si ya tenemos el objeto en inventario o si sigue activo en la escena
        GameObject keyObj = GetBlackboardGameObject("KeyObject");
        bool hasKey = GetBlackboardBool("HasKey");
        bool keyAvailable = hasKey || (keyObj != null && keyObj.activeInHierarchy);
        SetBlackboardVariable("KeyAvailable", keyAvailable);

        GameObject toolObj = GetBlackboardGameObject("ToolObject");
        bool hasTool = GetBlackboardBool("HasTool");
        bool toolAvailable = hasTool || (toolObj != null && toolObj.activeInHierarchy);
        SetBlackboardVariable("ToolAvailable", toolAvailable);
    }

    private bool IsCopFar()
    {
        GameObject cop = GetBlackboardGameObject("Cop");
        GameObject treasure = GetBlackboardGameObject("Treasure");
        float safeDist = GetBlackboardFloat("CopSafeDistance", 8.0f);

        if (cop == null) return true;

        Transform referenceTarget = (treasure != null) ? treasure.transform : transform;
        float distance = Vector3.Distance(cop.transform.position, referenceTarget.position);
        return distance >= safeDist;
    }

    private Transform GetClosestRefuge()
    {
        Transform closest = null;
        float minDist = Mathf.Infinity;

        foreach (Transform refuge in refugePoints)
        {
            if (refuge == null) continue;
            float dist = Vector3.Distance(transform.position, refuge.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = refuge;
            }
        }
        return closest;
    }

    private Vector3 GetRandomNavMeshPoint(Vector3 origin, float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius + origin;
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return origin;
    }

    // --- HELPERS DEL BLACKBOARD ---

    private bool GetBlackboardBool(string varName)
    {
        if (behaviorAgent.GetVariable(varName, out BlackboardVariable<bool> variable))
            return variable.Value;
        return false;
    }

    private float GetBlackboardFloat(string varName, float defaultValue)
    {
        if (behaviorAgent.GetVariable(varName, out BlackboardVariable<float> variable))
            return variable.Value;
        return defaultValue;
    }

    private GameObject GetBlackboardGameObject(string varName)
    {
        if (behaviorAgent.GetVariable(varName, out BlackboardVariable<GameObject> variable))
            return variable.Value;
        return null;
    }

    private void SetBlackboardVariable<T>(string varName, T value)
    {
        behaviorAgent.SetVariableValue(varName, value);
    }
}