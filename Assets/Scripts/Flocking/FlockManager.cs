using UnityEngine;

public class FlockManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El prefab de tu araña (tiene que llevar el componente SpiderFlock)")]
    public GameObject spiderPrefab;
    [Tooltip("Cantidad de arañas a generar")]
    public int numSpiders = 20;
    [Tooltip("Área de generación inicial (mitad del ancho en X y en Z)")]
    public Vector3 spawnLimits = new Vector3(5, 0, 5);

    [Header("Ajustes del Enjambre (Flock)")]
    [Range(0.0f, 10.0f)] public float minSpeed = 2.0f;
    [Range(0.0f, 10.0f)] public float maxSpeed = 5.0f;
    [Tooltip("Radio en el que una araña considera a otra su vecina (cohesión y alineación)")]
    [Range(1.0f, 10.0f)] public float neighbourDistance = 3.0f;
    [Tooltip("Radio, más pequeño que el anterior, en el que se separan unas de otras")]
    [Range(0.1f, 5.0f)] public float separationDistance = 1.5f;
    [Range(0.0f, 10.0f)] public float rotationSpeed = 4.0f;

    [Header("Peso de cada regla")]
    public float cohesionWeight = 1f;
    public float alignWeight = 1f;
    public float separationWeight = 1.5f;

    [Header("Líderes")]
    [Tooltip("Cuántas arañas son líderes. Se puede cambiar con el juego en marcha")]
    [Min(0)] public int numLeaders = 2;
    [Tooltip("Velocidad a la que se mueven los líderes")]
    [Range(0.0f, 10.0f)] public float leaderSpeed = 3.0f;
    [Tooltip("Cuánto tira el líder más cercano de cada seguidor")]
    public float followLeaderWeight = 2f;
    [Tooltip("Distancia a la que un líder da por alcanzado su punto y elige otro")]
    public float leaderArriveDistance = 2f;
    [Tooltip("Cuánto más grandes se ven los líderes, para distinguirlos")]
    public float leaderScale = 1.5f;

    [Header("Extras")]
    [Tooltip("Cada cuántos segundos recalcula cada araña las reglas. 0 = cada frame, como en las diapos")]
    [Range(0f, 1f)] public float rulesInterval = 0.2f;
    [Tooltip("Pequeña componente aleatoria para que el movimiento sea menos mecánico")]
    [Range(0f, 1f)] public float randomness = 0.2f;
    [Tooltip("Si está activo, las arañas que se salen de roamLimits vuelven al centro")]
    public bool bounded = true;
    [Tooltip("Mitad del ancho en X y en Z de la zona por la que pueden moverse")]
    public Vector3 roamLimits = new Vector3(15, 0, 15);
    [Tooltip("Opcional: un punto al que el grupo tiende a ir (por ejemplo, el jugador). Si hay, los líderes van hacia él")]
    public Transform goal;
    public float goalWeight = 0.5f;

    [HideInInspector]
    public SpiderFlock[] allSpiders;

    void Start()
    {
        if (spiderPrefab == null || spiderPrefab.GetComponent<SpiderFlock>() == null)
        {
            Debug.LogError("FlockManager: falta el prefab o no tiene el componente SpiderFlock.", this);
            return;
        }

        allSpiders = new SpiderFlock[numSpiders];
        for (int i = 0; i < numSpiders; i++)
        {
            Vector3 pos = this.transform.position + new Vector3(
                Random.Range(-spawnLimits.x, spawnLimits.x),
                0, 
                Random.Range(-spawnLimits.z, spawnLimits.z));

            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject go = Instantiate(spiderPrefab, pos, rot);

            allSpiders[i] = go.GetComponent<SpiderFlock>();
            allSpiders[i].myManager = this;
        }

        AssignLeaders();
    }

   
    public void AssignLeaders()
    {
        if (allSpiders == null) return;

        int n = Mathf.Clamp(numLeaders, 0, allSpiders.Length);

        for (int i = 0; i < allSpiders.Length; i++)
        {
            if (allSpiders[i] != null)
                allSpiders[i].SetLeader(i < n);
        }
    }

    void OnValidate()
    {
        if (maxSpeed < minSpeed) maxSpeed = minSpeed;

        if (Application.isPlaying) AssignLeaders();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnLimits.x * 2f, 0.1f, spawnLimits.z * 2f));

        if (bounded)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(roamLimits.x * 2f, 0.1f, roamLimits.z * 2f));
        }
    }
}