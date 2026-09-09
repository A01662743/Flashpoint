using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class UnityGameVisualizer : MonoBehaviour, IGameVisualizer
{
    [Header("Prefabs Visuales")]
    public GameObject smokePrefab;
    public GameObject firePrefab;
    public GameObject damagedWallEffectPrefab;
    public GameObject destroyedDoorEffectPrefab;

    [Header("Configuración del Tablero")]
    [Tooltip("Tamaño de cada casilla (unidades de Unity)")]
    public float cellSize = 5.0f;

    [Tooltip("Cantidad de casillas del tablero en el eje Y (vertical)")]
    [FormerlySerializedAs("gridRows")]
    public int boardSizeY = 6;

    [Tooltip("Cantidad de casillas del tablero en el eje X (horizontal)")]
    [FormerlySerializedAs("gridCols")]
    public int boardSizeX = 8;

    [Tooltip("Alineación: True para 2D (XY), False para 3D (XZ)")]
    public bool is2D = false;

    // ========================================================================
    // DICCIONARIOS DE RASTREO
    // Convención única del proyecto: coordenadas siempre en (x, y), Base-1.
    // ========================================================================

    // Mapea (x, y) -> GameObject de Humo
    private Dictionary<Vector2Int, GameObject> smokeObjects = new Dictionary<Vector2Int, GameObject>();

    // Mapea (x, y) -> GameObject de Fuego
    private Dictionary<Vector2Int, GameObject> fireObjects = new Dictionary<Vector2Int, GameObject>();

    // Mapea ID de Puerta -> GameObject de Puerta en Escena
    private Dictionary<int, GameObject> doorObjects = new Dictionary<int, GameObject>();

    // Mapea "x1,y1-x2,y2" -> GameObject de Pared Física
    private Dictionary<string, GameObject> wallsByCoords = new Dictionary<string, GameObject>();

    // Mapea ID de Pared (asignado por JSON al dañarse) -> GameObject de Pared
    private Dictionary<int, GameObject> wallsById = new Dictionary<int, GameObject>();


    // ========================================================================
    // MÉTODOS DE REGISTRO INICIAL
    // ========================================================================

    /// <summary>
    /// Registra un GameObject de pared física usando el par de casillas que divide.
    /// IMPORTANTE: quien llame a este método debe pasar las coordenadas en el
    /// mismo orden (x1, y1, x2, y2) que usa el resto del proyecto.
    /// </summary>
    
    private void Start()
    {
        RegistrarFuegosIniciales();
    }

    private void RegistrarFuegosIniciales()
    {
        // Busca todos los objetos con el script Fuego que ya están en la escena
        Fuego[] fuegosEnEscena = FindObjectsByType<Fuego>(FindObjectsSortMode.None);

        foreach (Fuego fuego in fuegosEnEscena)
        {
            // Convertimos su posición de mundo a coordenadas de Grid
            Vector2Int gridPos = WorldToGridPosition(fuego.transform.position);

            if (!fireObjects.ContainsKey(gridPos))
            {
                fireObjects.Add(gridPos, fuego.gameObject);
                fuego.gameObject.name = $"Fire_[{gridPos.x},{gridPos.y}]";
            }
        }
    }
    public void RegisterWallByCoordinates(int x1, int y1, int x2, int y2, GameObject wallGO)
    {
        string key = GetWallKey(x1, y1, x2, y2);
        wallsByCoords[key] = wallGO;
    }

    /// <summary>
    /// Registra un GameObject de puerta vinculándolo con su ID inicial del JSON.
    /// </summary>
    public void RegisterDoorObject(int doorId, GameObject doorGO)
    {
        if (!doorObjects.ContainsKey(doorId))
        {
            doorObjects.Add(doorId, doorGO);
        }
    }


    // ========================================================================
    // CONVERSIÓN DE COORDENADAS (WORLD <-> TABLERO)
    // ========================================================================

    /// <summary>
    /// Convierte coordenadas del JSON (x, y) Base-1 a posición 3D/2D en Unity.
    /// (0,0,0) es el centro del tablero. Soporta índices negativos o bordes.
    /// </summary>
    public Vector3 GridToWorldPosition(int x, int y)
    {
        // El centro exacto entre x=4 y x=5 es 4.5; entre y=3 y y=4 es 3.5
        float xCenterOffset = (boardSizeX + 1) / 2.0f; // 8 casillas en X -> 4.5
        float yCenterOffset = (boardSizeY + 1) / 2.0f; // 6 casillas en Y -> 3.5

        float worldX = (x - xCenterOffset) * cellSize;
        float worldZ = (yCenterOffset - y) * cellSize;

        if (is2D)
        {
            return new Vector3(worldX, worldZ, 0f);
        }
        else
        {
            return new Vector3(worldX, 0f, worldZ);
        }
    }

    /// <summary>
    /// Convierte una posición en Unity (Vector3) a coordenadas discretas (x, y) en Base-1 (Vector2Int).
    /// </summary>
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        float xCenterOffset = (boardSizeX + 1) / 2.0f;
        float yCenterOffset = (boardSizeY + 1) / 2.0f;

        float posX = worldPos.x;
        float posZ = is2D ? worldPos.y : worldPos.z;

        int x = Mathf.RoundToInt((posX / cellSize) + xCenterOffset);
        int y = Mathf.RoundToInt(yCenterOffset - (posZ / cellSize));

        return new Vector2Int(x, y);
    }


    // ========================================================================
    // GESTIÓN DE HUMO Y FUEGO
    // ========================================================================

    public void SpawnSmokeVisual(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);
        if (smokeObjects.ContainsKey(pos)) return;

        Vector3 worldPos = GridToWorldPosition(x, y);
        GameObject instance = Instantiate(smokePrefab, worldPos, Quaternion.identity, transform);
        instance.name = $"Smoke_[{x},{y}]";
        smokeObjects.Add(pos, instance);
    }

    public void PromoteSmokeToFireVisual(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);

        // 1. Destruir y remover el humo existente en esa casilla
        if (smokeObjects.TryGetValue(pos, out GameObject smokeGO))
        {
            Destroy(smokeGO);
            smokeObjects.Remove(pos);
        }

        // 2. Crear fuego en la misma casilla
        SpawnFireVisual(x, y);
    }

    public void SpawnFireVisual(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);
        if (fireObjects.ContainsKey(pos)) return;

        Vector3 worldPos = GridToWorldPosition(x, y);
        GameObject instance = Instantiate(firePrefab, worldPos, Quaternion.identity, transform);
        instance.name = $"Fire_[{x},{y}]";
        fireObjects.Add(pos, instance);
    }

    public void TriggerHeatUpAnimation(int x, int y)
{
    Vector2Int pos = new Vector2Int(x, y);

    if (!fireObjects.TryGetValue(pos, out GameObject fireGO))
    {
        Debug.LogError($"[DIAGNOSTICO] No existe ({x}, {y}) en fireObjects.");
        
        // Imprime todas las claves guardadas para ver las coordenadas reales
        Debug.Log($"[DIAGNOSTICO] Claves registradas actualmente en fireObjects ({fireObjects.Count}): " 
            + string.Join(", ", fireObjects.Keys));
            
        return;
    }

    if (fireGO != null && fireGO.TryGetComponent<Fuego>(out var scriptFuego))
    {
        scriptFuego.IniciarEfectoEscalado();
    }
}


    // ========================================================================
    // GESTIÓN DE PAREDES Y PUERTAS
    // ========================================================================

    public void DamageWallVisual(int wallId, int[] coordA, int[] coordB)
    {
        GameObject wallGO = FindWallGameObject(wallId, coordA, coordB);

        if (wallGO != null)
        {
            Debug.Log($"[VISUAL] Pared en ({coordA[0]},{coordA[1]}) - ({coordB[0]},{coordB[1]}) (ID {wallId}) marcada como dañada.");
        }
    }

    public void DestroyWallVisual(int wallId, int[] coordA, int[] coordB)
    {
        GameObject wallGO = FindWallGameObject(wallId, coordA, coordB);

        if (wallGO != null)
        {
            Debug.Log($"[VISUAL] Pared ID {wallId} destruida de la escena.");
        }
    }

    public void DestroyDoorVisual(int doorId)
    {
        if (doorObjects.TryGetValue(doorId, out GameObject doorGO))
        {
            Debug.Log($"[VISUAL] Puerta ID {doorId} destruida de la escena.");
        }
    }


    // ========================================================================
    // MÉTODOS AUXILIARES DE BÚSQUEDA DE PAREDES
    // ========================================================================

    /// <summary>
    /// Busca el GameObject de la pared. Si no está asociada al ID del JSON,
    /// la busca por coordenadas y vincula el ID dinámicamente.
    /// </summary>
    private GameObject FindWallGameObject(int wallId, int[] coordA, int[] coordB)
    {
        // 1. Intentar buscar por ID del JSON
        if (wallsById.TryGetValue(wallId, out GameObject wallGO))
        {
            return wallGO;
        }

        // 2. Buscar por coordenadas espaciales (coordA/coordB llegan como [x, y])
        string key = GetWallKey(coordA[0], coordA[1], coordB[0], coordB[1]);
        if (wallsByCoords.TryGetValue(key, out wallGO))
        {
            // Asociar ID del JSON al GameObject para futuras operaciones
            wallsById[wallId] = wallGO;
            return wallGO;
        }

        Debug.LogWarning($"[VISUAL] No se encontró el GameObject de la pared entre ({coordA[0]},{coordA[1]}) y ({coordB[0]},{coordB[1]})");
        return null;
    }

    /// <summary>
    /// Genera una clave única en string independiente del orden de las dos casillas adyacentes.
    /// Recibe siempre (x1, y1, x2, y2).
    /// </summary>
    private string GetWallKey(int x1, int y1, int x2, int y2)
    {
        if (y1 < y2 || (y1 == y2 && x1 < x2))
        {
            return $"{x1},{y1}-{x2},{y2}";
        }
        return $"{x2},{y2}-{x1},{y1}";
    }

    // Placeholders para POI y Agentes
    public void RemovePOIVisual(int poiId, string result) { }
    public void EliminateAgentVisual(int agentId) { }
}