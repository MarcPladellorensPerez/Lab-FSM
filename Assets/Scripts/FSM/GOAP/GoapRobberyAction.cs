using System;
using System.Collections.Generic;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

/// <summary>
/// Parte 3 (GOAP). Sustituye las secuencias fijas del grafo Robber: al empezar lee el estado del mundo
/// del Blackboard y de la escena, genera con GoapPlanner el plan de menor coste para HasTreasure = true
/// y lo ejecuta acción por acción (desplazamiento con NavMeshAgent + efecto al llegar).
/// La FSM (RobberFSM) no cambia: Success con HasTreasure -> Hiding; Failure o interrupción -> Wander.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "GOAP Robbery",
    story: "[Agent] plans and executes a GOAP plan to get [HasTreasure]",
    category: "Action/GOAP",
    id: "5b8e2d4f9a1c47e6b3d0f2a8c6e4b1d9")]
public partial class GoapRobberyAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<bool> HasTreasure;

    [SerializeReference] public BlackboardVariable<GameObject> KeyObject;
    [SerializeReference] public BlackboardVariable<GameObject> ToolObject;
    [SerializeReference] public BlackboardVariable<GameObject> DoorPoint;
    [SerializeReference] public BlackboardVariable<GameObject> DoorBlocker;
    [SerializeReference] public BlackboardVariable<GameObject> Treasure;

    [SerializeReference] public BlackboardVariable<bool> HasKey;
    [SerializeReference] public BlackboardVariable<bool> HasTool;
    [SerializeReference] public BlackboardVariable<bool> DoorOpen;

    [SerializeReference] public BlackboardVariable<float> Speed = new(4.5f);
    [SerializeReference] public BlackboardVariable<float> ArrivalDistance = new(0.8f);

    [SerializeReference] public BlackboardVariable<float> PickCost = new(1f);
    [SerializeReference] public BlackboardVariable<float> OpenWithKeyCost = new(1f);
    [SerializeReference] public BlackboardVariable<float> ForceWithToolCost = new(3f);
    [SerializeReference] public BlackboardVariable<float> TakeTreasureCost = new(1f);

    [SerializeReference] public BlackboardVariable<string> CurrentPlan;

    // Hechos del estado del mundo
    private const string KEY_IN_SCENE = "KeyInScene";
    private const string TOOL_IN_SCENE = "ToolInScene";
    private const string HAS_KEY = "HasKey";
    private const string HAS_TOOL = "HasTool";
    private const string DOOR_OPEN = "DoorOpen";
    private const string HAS_TREASURE = "HasTreasure";

    // Reintentos cuando el camino sale parcial (p. ej. el NavMesh aún no se ha actualizado tras abrir la puerta)
    private const int MaxRepathRetries = 4;
    private const float RepathDelay = 0.25f;

    private List<GoapAction> m_Plan;
    private int m_Step;
    private NavMeshAgent m_Nav;
    private float m_OriginalSpeed = -1f;
    private int m_Retries;
    private float m_RetryTimer;

    protected override Status OnStart()
    {
        if (Agent == null || Agent.Value == null) return Fail("falta la referencia Agent");
        m_Nav = Agent.Value.GetComponentInChildren<NavMeshAgent>();
        if (m_Nav == null) return Fail("el agente no tiene NavMeshAgent");

        var goal = new Dictionary<string, bool> { { HAS_TREASURE, true } };
        m_Plan = GoapPlanner.Plan(ReadWorldState(), goal, BuildActions(), out float cost);

        if (m_Plan == null)
        {
            SetPlanText("(sin plan)");
            return Fail("no existe ningún plan que consiga HasTreasure = true");
        }

        string text = $"{GoapPlanner.Describe(m_Plan)} (coste {cost:0.##})";
        SetPlanText(text);
        Debug.Log($"[GOAP] Plan: {text}", Agent.Value);

        if (m_Plan.Count == 0) return Status.Success;

        m_OriginalSpeed = m_Nav.speed;
        m_Nav.speed = Speed.Value;
        m_Nav.isStopped = false;
        m_Step = 0;
        return BeginStep();
    }

    protected override Status OnUpdate()
    {
        if (m_Plan == null || m_Nav == null) return Status.Failure;
        GoapAction action = m_Plan[m_Step];

        if (m_Nav.pathPending) return Status.Running;
        if (m_Nav.remainingDistance > ArrivalDistance.Value) return Status.Running;

        if (m_Nav.pathStatus != NavMeshPathStatus.PathComplete)
        {
            // Ha llegado al final de un camino parcial: reintenta unas pocas veces antes de fallar.
            m_RetryTimer += Time.deltaTime;
            if (m_RetryTimer < RepathDelay) return Status.Running;
            m_RetryTimer = 0f;
            if (++m_Retries > MaxRepathRetries) return Fail($"{action.Name}: destino inalcanzable");
            m_Nav.SetDestination(Destination(action.Target));
            return Status.Running;
        }

        if (!action.Perform()) return Fail($"{action.Name}: la acción ha fallado (el mundo ha cambiado)");
        Debug.Log($"[GOAP] {action.Name} completada", Agent.Value);

        m_Step++;
        if (m_Step >= m_Plan.Count) return Status.Success;
        return BeginStep();
    }

    protected override void OnEnd()
    {
        // Se llama también cuando la FSM interrumpe el robo con behaviorAgent.End(): cancela el desplazamiento.
        if (m_Nav != null)
        {
            if (m_Nav.isOnNavMesh) m_Nav.ResetPath();
            if (m_OriginalSpeed >= 0f) m_Nav.speed = m_OriginalSpeed;
        }
        m_Plan = null;
        m_Nav = null;
        m_OriginalSpeed = -1f;
    }

    // ---------- Planificación ----------

    private Dictionary<string, bool> ReadWorldState()
    {
        return new Dictionary<string, bool>
        {
            { KEY_IN_SCENE, IsActive(KeyObject) },
            { TOOL_IN_SCENE, IsActive(ToolObject) },
            { HAS_KEY, Get(HasKey) },
            { HAS_TOOL, Get(HasTool) },
            { DOOR_OPEN, Get(DoorOpen) },
            { HAS_TREASURE, Get(HasTreasure) },
        };
    }

    private List<GoapAction> BuildActions()
    {
        return new List<GoapAction>
        {
            new GoapAction("RecogerLlave", PickCost.Value, KeyObject?.Value, () => Pick(KeyObject, HasKey))
                .Pre(KEY_IN_SCENE, true).Pre(HAS_KEY, false)
                .Eff(HAS_KEY, true).Eff(KEY_IN_SCENE, false),

            new GoapAction("RecogerHerramienta", PickCost.Value, ToolObject?.Value, () => Pick(ToolObject, HasTool))
                .Pre(TOOL_IN_SCENE, true).Pre(HAS_TOOL, false)
                .Eff(HAS_TOOL, true).Eff(TOOL_IN_SCENE, false),

            new GoapAction("AbrirConLlave", OpenWithKeyCost.Value, DoorPoint?.Value, OpenDoor)
                .Pre(HAS_KEY, true).Pre(DOOR_OPEN, false)
                .Eff(DOOR_OPEN, true),

            new GoapAction("ForzarConHerramienta", ForceWithToolCost.Value, DoorPoint?.Value, OpenDoor)
                .Pre(HAS_TOOL, true).Pre(DOOR_OPEN, false)
                .Eff(DOOR_OPEN, true),

            new GoapAction("CogerTesoro", TakeTreasureCost.Value, Treasure?.Value, TakeTreasure)
                .Pre(DOOR_OPEN, true).Pre(HAS_TREASURE, false)
                .Eff(HAS_TREASURE, true),
        };
    }

    // ---------- Ejecución ----------

    private Status BeginStep()
    {
        GoapAction action = m_Plan[m_Step];
        if (action.Target == null) return Fail($"{action.Name}: falta la referencia del objetivo");
        m_Retries = 0;
        m_RetryTimer = 0f;
        m_Nav.SetDestination(Destination(action.Target));
        return Status.Running;
    }

    private bool Pick(BlackboardVariable<GameObject> item, BlackboardVariable<bool> flag)
    {
        if (!IsActive(item)) return false;
        Set(flag, true);
        item.Value.SetActive(false);
        return true;
    }

    // Misma regla que el nodo OpenDoor (pista 3).
    private bool OpenDoor()
    {
        if (Get(DoorOpen)) return true;
        if (DoorBlocker == null || DoorBlocker.Value == null || !(Get(HasKey) || Get(HasTool))) return false;
        DoorBlocker.Value.SetActive(false);
        Set(DoorOpen, true);
        return true;
    }

    private bool TakeTreasure()
    {
        if (!IsActive(Treasure)) return false;
        Treasure.Value.SetActive(false);
        Set(HasTreasure, true);
        return true;
    }

    // ---------- Utilidades ----------

    private static Vector3 Destination(GameObject target)
    {
        Vector3 p = target.transform.position;
        return NavMesh.SamplePosition(p, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : p;
    }

    private static bool IsActive(BlackboardVariable<GameObject> v) => v != null && v.Value != null && v.Value.activeInHierarchy;
    private static bool Get(BlackboardVariable<bool> v) => v != null && v.Value;
    private static void Set(BlackboardVariable<bool> v, bool value) { if (v != null) v.Value = value; }

    private void SetPlanText(string text) { if (CurrentPlan != null) CurrentPlan.Value = text; }

    private Status Fail(string reason)
    {
        Debug.LogWarning($"[GOAP] {reason}", Agent != null ? Agent.Value : null);
        return Status.Failure;
    }
}
