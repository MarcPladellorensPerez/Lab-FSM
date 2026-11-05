using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Acción GOAP: precondiciones, efectos y coste sobre un estado del mundo booleano.
/// Target indica adónde debe ir el agente antes de ejecutar Perform (que devuelve false si la acción falla).
/// </summary>
public class GoapAction
{
    public readonly string Name;
    public readonly float Cost;
    public readonly GameObject Target;
    public readonly Func<bool> Perform;
    public readonly Dictionary<string, bool> Preconditions = new Dictionary<string, bool>();
    public readonly Dictionary<string, bool> Effects = new Dictionary<string, bool>();

    public GoapAction(string name, float cost, GameObject target, Func<bool> perform)
    {
        Name = name;
        Cost = cost;
        Target = target;
        Perform = perform;
    }

    public GoapAction Pre(string fact, bool value) { Preconditions[fact] = value; return this; }
    public GoapAction Eff(string fact, bool value) { Effects[fact] = value; return this; }

    public bool IsApplicable(Dictionary<string, bool> state) => GoapPlanner.Satisfies(state, Preconditions);

    public Dictionary<string, bool> Apply(Dictionary<string, bool> state)
    {
        var next = new Dictionary<string, bool>(state);
        foreach (var effect in Effects) next[effect.Key] = effect.Value;
        return next;
    }
}

/// <summary>
/// Planificador base: búsqueda de coste uniforme (A* sin heurística) desde el estado actual
/// hasta un estado que cumpla el objetivo. Devuelve la secuencia de acciones de menor coste, o null.
/// </summary>
public static class GoapPlanner
{
    private class SearchNode
    {
        public Dictionary<string, bool> State;
        public List<GoapAction> Plan;
        public float Cost;
    }

    public static List<GoapAction> Plan(Dictionary<string, bool> start, Dictionary<string, bool> goal,
                                        List<GoapAction> actions, out float totalCost, int maxExpansions = 1000)
    {
        totalCost = 0f;
        var open = new List<SearchNode> { new SearchNode { State = start, Plan = new List<GoapAction>(), Cost = 0f } };
        var closed = new HashSet<string>();
        int expansions = 0;

        while (open.Count > 0 && expansions < maxExpansions)
        {
            // Extrae el nodo de menor coste acumulado.
            int best = 0;
            for (int i = 1; i < open.Count; i++)
                if (open[i].Cost < open[best].Cost) best = i;
            SearchNode current = open[best];
            open.RemoveAt(best);

            if (Satisfies(current.State, goal))
            {
                totalCost = current.Cost;
                return current.Plan;
            }

            if (!closed.Add(Key(current.State))) continue;
            expansions++;

            foreach (GoapAction action in actions)
            {
                if (!action.IsApplicable(current.State)) continue;
                Dictionary<string, bool> next = action.Apply(current.State);
                if (closed.Contains(Key(next))) continue;

                var plan = new List<GoapAction>(current.Plan) { action };
                open.Add(new SearchNode { State = next, Plan = plan, Cost = current.Cost + action.Cost });
            }
        }
        return null;
    }

    public static bool Satisfies(Dictionary<string, bool> state, Dictionary<string, bool> conditions)
    {
        foreach (var condition in conditions)
            if (!state.TryGetValue(condition.Key, out bool value) || value != condition.Value) return false;
        return true;
    }

    public static string Describe(List<GoapAction> plan)
    {
        if (plan == null) return "(sin plan)";
        if (plan.Count == 0) return "(objetivo ya cumplido)";
        return string.Join(" -> ", plan.Select(a => a.Name));
    }

    private static string Key(Dictionary<string, bool> state)
    {
        var sb = new StringBuilder();
        foreach (var fact in state.OrderBy(f => f.Key, StringComparer.Ordinal))
            sb.Append(fact.Key).Append(fact.Value ? '1' : '0').Append(';');
        return sb.ToString();
    }
}
