using System.Collections.Generic;
using UnityEngine;

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

    [Tooltip("Número total de filas de la matriz del JSON")]
    public int gridRows = 6;

    [Tooltip("Número total de columnas de la matriz del JSON")]
    public int gridCols = 8;

    [Tooltip("Alineación: True para 2D (XY), False para 3D (XZ)")]
    public bool is2D = false;

    // ========================================================================
    // DICCIONARIOS DE RASTREO
    // ========================================================================
    
    // Mapea [fila, columna] -> GameObject de Humo
    private Dictionary<Vector2Int, GameObject> smokeObjects = new Dictionary<Vector2Int, GameObject>();

    // Mapea [fila, columna] -> GameObject de Fuego
    private Dictionary<Vector2Int, GameObject> fireObjects = new Dictionary<Vector2Int, GameObject>();

    // Mapea ID de Puerta -> GameObject de Puerta en Escena
    private Dictionary<int, GameObject> doorObjects = new Dictionary<int, GameObject>();

    // Mapea "row1,col1-row2,col2" -> GameObject de Pared Física
    private Dictionary<string, GameObject> wallsByCoords = new Dictionary<string, GameObject>();

    // Mapea ID de Pared (asignado por JSON al dañarse) -> GameObject de Pared
    private Dictionary<int, GameObject> wallsById = new Dictionary<int, GameObject>();


    // ========================================================================
    // MÉTODOS DE REGISTRO INICIAL
    // ========================================================================

    /// <summary>
    /// Registra un GameObject de pared física usando el par de casillas que divide.
    /// </summary>
    public void RegisterWallByCoordinates(Vector2Int cellA, Vector2Int cellB, GameObject wallGO)
    {
        string key = GetWallKey(cellA.x, cellA.y, cellB.x, cellB.y);
        if (!wallsByCoords.ContainsKey(key))
        {
            wallsByCoords.Add(key, wallGO);
        }
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
    // CONVERSIÓN DE COORDENADAS (WORLD <-> GRID)
    // ========================================================================

    /// <summary>
    /// Convierte coordenadas del JSON [row, col] a posición 3D/2D en Unity.
    /// (0,0,0) es el centro del tablero. Soporta índices negativos o bordes.
    /// </summary>
    public Vector3 GridToWorldPosition(int row, int col)
    {
        float colCenterOffset = (gridCols - 1) / 2.0f;
        float rowCenterOffset = (gridRows - 1) / 2.0f;

        float worldX = (col - colCenterOffset) * cellSize;
        float worldZ = -(row - rowCenterOffset) * cellSize;

        if (is2D)
        {
            return new Vector3(worldX, -worldZ, 0f);
        }
        else
        {
            return new Vector3(worldX, 0f, worldZ);
        }
    }

    /// <summary>
    /// Convierte una posición en Unity (Vector3) a coordenadas discretas [row, col] (Vector2Int).
    /// Permite valores fuera de rango o bordes (sin clampear).
    /// </summary>
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        float colCenterOffset = (gridCols - 1) / 2.0f;
        float rowCenterOffset = (gridRows - 1) / 2.0f;

        float posX = is2D ? worldPos.x : worldPos.x;
        float posZ = is2D ? -worldPos.y : worldPos.z;

        int col = Mathf.RoundToInt((posX / cellSize) + colCenterOffset);
        int row = Mathf.RoundToInt((-posZ / cellSize) + rowCenterOffset);

        return new Vector2Int(row, col);
    }


    // ========================================================================
    // GESTIÓN DE HUMO Y FUEGO
    // ========================================================================

    public void SpawnSmokeVisual(int row, int col)
    {
        Vector2Int pos = new Vector2Int(row, col);
        if (smokeObjects.ContainsKey(pos)) return;

        Vector3 worldPos = GridToWorldPosition(row, col);
        GameObject instance = Instantiate(smokePrefab, worldPos, Quaternion.identity, transform);
        instance.name = $"Smoke_[{row},{col}]";
        smokeObjects.Add(pos, instance);
    }

    public void PromoteSmokeToFireVisual(int row, int col)
    {
        Vector2Int pos = new Vector2Int(row, col);

        // 1. Destruir y remover el humo existente en esa casilla
        if (smokeObjects.TryGetValue(pos, out GameObject smokeGO))
        {
            Destroy(smokeGO);
            smokeObjects.Remove(pos);
        }

        // 2. Crear fuego en la misma casilla
        SpawnFireVisual(row, col);
    }

    public void SpawnFireVisual(int row, int col)
    {
        Vector2Int pos = new Vector2Int(row, col);
        if (fireObjects.ContainsKey(pos)) return;

        Vector3 worldPos = GridToWorldPosition(row, col);
        GameObject instance = Instantiate(firePrefab, worldPos, Quaternion.identity, transform);
        instance.name = $"Fire_[{row},{col}]";
        fireObjects.Add(pos, instance);
    }

    public void TriggerHeatUpAnimation(int row, int col)
    {
        Vector2Int pos = new Vector2Int(row, col);

        if (fireObjects.TryGetValue(pos, out GameObject fireGO))
        {
            Debug.Log($"[VISUAL] HeatUp ejecutado en el objeto de fuego en [{row}, {col}]");
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
            Debug.Log($"[VISUAL] Pared en [{coordA[0]},{coordA[1]}] - [{coordB[0]},{coordB[1]}] (ID {wallId}) marcada como dañada.");
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

        // 2. Buscar por coordenadas espaciales
        string key = GetWallKey(coordA[0], coordA[1], coordB[0], coordB[1]);
        if (wallsByCoords.TryGetValue(key, out wallGO))
        {
            // Asociar ID del JSON al GameObject para futuras operaciones
            wallsById[wallId] = wallGO;
            return wallGO;
        }

        Debug.LogWarning($"[VISUAL] No se encontró el GameObject de la pared entre [{coordA[0]},{coordA[1]}] y [{coordB[0]},{coordB[1]}]");
        return null;
    }

    /// <summary>
    /// Genera una clave única en string independiente del orden de las dos casillas adyacentes.
    /// </summary>
    private string GetWallKey(int row1, int col1, int row2, int col2)
    {
        if (row1 < row2 || (row1 == row2 && col1 < col2))
        {
            return $"{row1},{col1}-{row2},{col2}";
        }
        return $"{row2},{col2}-{row1},{col1}";
    }

    // Placeholders para POI y Agentes
    public void RemovePOIVisual(int poiId, string result) { }
    public void EliminateAgentVisual(int agentId) { }
}