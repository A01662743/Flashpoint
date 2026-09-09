using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class UnityGameVisualizer : MonoBehaviour, IGameVisualizer
{
    [Header("Prefabs Visuales")]
    public GameObject smokePrefab;
    public GameObject firePrefab;
    public GameObject POIPrefab;

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

    public GameStateManager stateManager; // Objeto game state manager

    // ========================================================================
    // DICCIONARIOS DE RASTREO
    // Convención única del proyecto: coordenadas siempre en (x, y), Base-1.
    // ========================================================================

    // Mapea (x, y) -> GameObject de Humo
    private Dictionary<Vector2Int, GameObject> smokeObjects = new Dictionary<Vector2Int, GameObject>();

    // Mapea (x, y) -> GameObject de Fuego
    private Dictionary<Vector2Int, GameObject> fireObjects = new Dictionary<Vector2Int, GameObject>();

    // Mapea (x, y) -> GameObject de POI's
    private Dictionary<int, GameObject> POIObjects = new Dictionary<int, GameObject>();

    // Mapea (x, y) -> GameObject de POI físico en escena (por posición, antes de conocer su ID real del JSON)
    private Dictionary<Vector2Int, GameObject> POIObjectsByPos = new Dictionary<Vector2Int, GameObject>();

    // Mapea ID de Puerta -> GameObject de Puerta en Escena
    private Dictionary<int, GameObject> doorObjects = new Dictionary<int, GameObject>();

    // Mapea "x1,y1-x2,y2" -> GameObject de Pared Física
    private Dictionary<string, GameObject> wallsByCoords = new Dictionary<string, GameObject>();

    // Mapea ID de Pared (asignado por JSON al dañarse) -> GameObject de Pared
    private Dictionary<int, GameObject> wallsById = new Dictionary<int, GameObject>();

    // Mapea ID de Agente -> GameObject de Agente en Escena
    private Dictionary<int, GameObject> agentObjects = new Dictionary<int, GameObject>();


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
        RegistrarPOIsDeEscenaFisica(); // Llena POIObjectsByPos con los POI físicos de la escena

        // Intentar registro inicial al arrancar
        TryRegisterInitialEntities();
    }

    public void TryRegisterInitialEntities()
    {
        Debug.Log($"[DEBUG] TryRegisterInitialEntities llamado. stateManager null? {stateManager == null} | CurrentState null? {stateManager?.CurrentState == null}");

        if (stateManager != null && stateManager.CurrentState != null)
        {
            RegistrarAgentesIniciales(stateManager.CurrentState);
            RegistrarPOIsIniciales(stateManager.CurrentState);
        }
    }

    public void RegistrarAgentesIniciales(GameState state)
    {
        if (state?.agents == null) return;

        // Busca los GameObjects en la escena (puedes filtrar por Tag "Agent" o por su Componente)
        GameObject[] agentesEnEscena = GameObject.FindGameObjectsWithTag("Agent");

        foreach (GameObject agenteGO in agentesEnEscena)
        {
            // Convertimos la posición 3D/2D del objeto a su casilla (x, y) en la matriz
            Vector2Int gridPos = WorldToGridPosition(agenteGO.transform.position);

            // Buscamos en el GameState cuál agente tiene estas mismas coordenadas
            Agent matchData = state.agents.Find(a => a.position != null && 
                                                    a.position.Length >= 2 && 
                                                    a.position[0] == gridPos.x && 
                                                    a.position[1] == gridPos.y);

            if (matchData != null && !agentObjects.ContainsKey(matchData.id))
            {
                agentObjects.Add(matchData.id, agenteGO);
                agenteGO.name = $"Agent_{matchData.id}";
                Debug.Log($"[VISUAL] Agente vinculado: Pos ({gridPos.x},{gridPos.y}) -> ID {matchData.id}");
            }
        }
    }

    public void RegistrarPOIsDeEscenaFisica()
    {
        GameObject[] poisEnEscena = GameObject.FindGameObjectsWithTag("POI");
        Debug.Log($"[DEBUG] RegistrarPOIsDeEscenaFisica encontró {poisEnEscena.Length} objetos con tag POI.");

        foreach (GameObject poiGO in poisEnEscena)
        {
            Vector2Int gridPos = WorldToGridPosition(poiGO.transform.position);

            // Si el estado aún no tiene los IDs asignados, asignamos temporalmente el InstanceID único de Unity
            int idTemp = poiGO.GetHashCode();
            if (idTemp < 0) idTemp = -idTemp; // Garantizar valor positivo

            if (!POIObjects.ContainsKey(idTemp))
            {
                POIObjects.Add(idTemp, poiGO);
            }

            if (!POIObjectsByPos.ContainsKey(gridPos))
            {
                POIObjectsByPos.Add(gridPos, poiGO);
            }
            
            Debug.Log($"[VISUAL] POI Físico registrado en Escena -> Casilla ({gridPos.x},{gridPos.y}) | WorldPos: {poiGO.transform.position} | Nombre: {poiGO.name}");
        }
    }

    public void RegistrarPOIsIniciales(GameState state)
    {
        if (state?.poi == null)
        {
            Debug.LogWarning("[DEBUG] RegistrarPOIsIniciales: state.poi es null, no hay nada que vincular.");
            return;
        }

        Debug.Log($"[DEBUG] RegistrarPOIsIniciales: intentando vincular {state.poi.Count} POIs del JSON. POIObjectsByPos tiene {POIObjectsByPos.Count} entradas.");

        // Vincular los IDs reales del JSON con los GameObjects registrados por posición
        foreach (POI poiData in state.poi)
        {
            if (poiData.position != null && poiData.position.Length >= 2)
            {
                Vector2Int pos = new Vector2Int(poiData.position[0], poiData.position[1]);

                if (POIObjectsByPos.TryGetValue(pos, out GameObject poiGO))
                {
                    // Actualizar la clave del diccionario con el ID real del JSON
                    if (!POIObjects.ContainsKey(poiData.id))
                    {
                        POIObjects.Add(poiData.id, poiGO);
                        poiGO.name = $"POI_{poiData.id}";
                        Debug.Log($"[VISUAL] POI en ({pos.x},{pos.y}) vinculado exitosamente con ID JSON: {poiData.id}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[DEBUG] No se encontró GameObject físico en POIObjectsByPos para la posición JSON ({pos.x},{pos.y}) del POI ID {poiData.id}. Claves disponibles: {string.Join(", ", POIObjectsByPos.Keys)}");
                }
            }
        }
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
        worldPos.y = smokePrefab.transform.position.y;
        GameObject instance = Instantiate(smokePrefab, worldPos, Quaternion.identity, transform);
        instance.name = $"Smoke_[{x},{y}]";
        smokeObjects.Add(pos, instance);
    }

    public void SpawnFireVisual(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);
        if (fireObjects.ContainsKey(pos)) return;

        Vector3 worldPos = GridToWorldPosition(x, y);
        worldPos.y = firePrefab.transform.position.y;
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
            // Intentamos obtener el script en la raíz o en un objeto hijo
            if (wallGO.TryGetComponent<WallIdentity>(out var pared) || 
                wallGO.GetComponentInChildren<WallIdentity>() is var paredHija && (pared = paredHija) != null)
            {
                pared.CambiarEstado(WallIdentity.Estado.Danado);
                Debug.Log($"[VISUAL] Pared en ({coordA[0]},{coordA[1]}) - ({coordB[0]},{coordB[1]}) (ID {wallId}) marcada como dañada.");
            }
            else
            {
                Debug.LogWarning($"[VISUAL] El GameObject de la pared ID {wallId} no tiene el componente WallIdentity.");
            }
        }
    }

    public void DestroyWallVisual(int wallId, int[] coordA, int[] coordB)
    {
        GameObject wallGO = FindWallGameObject(wallId, coordA, coordB);

        if (wallGO != null)
        {
            if (wallGO.TryGetComponent<WallIdentity>(out var pared) || 
                wallGO.GetComponentInChildren<WallIdentity>() is var paredHija && (pared = paredHija) != null)
            {
                pared.CambiarEstado(WallIdentity.Estado.Destruido);
                Debug.Log($"[VISUAL] Pared ID {wallId} destruida de la escena.");
            }
            else
            {
                Debug.LogWarning($"[VISUAL] El GameObject de la pared ID {wallId} no tiene el componente WallIdentity.");
            }
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
    public void RemovePOIVisual(int poiId)
    {
        Debug.Log($"[VISUAL] RemovePOIVisual llamado para ID {poiId}. ¿Está en diccionario? {POIObjects.ContainsKey(poiId)}");
        // Si el diccionario aún está vacío o faltaba este POI, intentar registrar entidades
        if (!POIObjects.ContainsKey(poiId))
        {
            Debug.LogWarning($"[VISUAL] POI se intento eliminar pero no se encontró, intentando registrar entidades iniciales.");
            TryRegisterInitialEntities();
        }

        if (POIObjects.TryGetValue(poiId, out GameObject poiGO))
        {
            POIObjects.Remove(poiId);

            if (poiGO != null)
            {
                if (poiGO.TryGetComponent<POIScript>(out var poiScript))
                {
                    poiScript.enabled = false; // deja de rotar/oscilar
                }
                StartCoroutine(AnimateAndDestroyPOI(poiGO));
            }
            else
            {
                Debug.LogWarning($"[VISUAL] POI ID {poiId} estaba en el diccionario pero su GameObject ya era null/destruido.");
            }
        }
        else
        {
            // RESPALDO DE EMERGENCIA: Buscar por coincidencia de nombre o etiqueta si el diccionario falló
            GameObject fallbackPOI = GameObject.Find($"POI_{poiId}");
            if (fallbackPOI != null)
            {
                StartCoroutine(AnimateAndDestroyPOI(fallbackPOI));
                Debug.Log($"[VISUAL] POI ID {poiId} encontrado mediante búsqueda de respaldo.");
            }
            else
            {
                Debug.LogWarning($"[VISUAL] No se encontró el POI con ID {poiId} para remover.");
            }
        }
    }

    private System.Collections.IEnumerator AnimateAndDestroyPOI(GameObject poiGO)
    {
        float targetY = 40f;
        float speed = 25f; // Velocidad del ascenso (ajustable)

        while (poiGO != null && poiGO.transform.position.y < targetY)
        {
            // Mueve la posición progresivamente hacia el Y objetivo
            Vector3 currentPos = poiGO.transform.position;
            currentPos.y = Mathf.MoveTowards(currentPos.y, targetY, speed * Time.deltaTime);
            poiGO.transform.position = currentPos;

            yield return null;
        }

        if (poiGO != null)
        {
            Destroy(poiGO);
            Debug.Log($"[VISUAL] POI elevado a Y={targetY} y destruido de la escena.");
        }
    }
    public void EliminateAgentVisual(int agentId) { }

    public void SpawnPOIVisual(int x, int y, int id)
    {
        if (POIObjects.ContainsKey(id)) return;

        Vector3 worldPos = GridToWorldPosition(x, y);
        worldPos.y = POIPrefab.transform.position.y;
        GameObject instance = Instantiate(POIPrefab, worldPos, Quaternion.identity, transform);
        instance.name = $"POI_{id}";
        
        // Se registra la instancia en el diccionario para permitir su eliminación por ID
        POIObjects.Add(id, instance);
    }

    public void RemoveSmokeVisual(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);

        if (smokeObjects.TryGetValue(pos, out GameObject smokeGO))
        {
            Destroy(smokeGO);
            smokeObjects.Remove(pos);
            Debug.Log($"[VISUAL] Humo removido en la casilla ({x}, {y}).");
        }
    }

    public void RemoveFireVisual(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);

        if (fireObjects.TryGetValue(pos, out GameObject fireGO))
        {
            Destroy(fireGO);
            fireObjects.Remove(pos);
            Debug.Log($"[VISUAL] Fuego removido en la casilla ({x}, {y}).");
        }
    }

    public void MoveAgentVisual(int agentId, int newX, int newY)
    {
        if (agentObjects.TryGetValue(agentId, out GameObject agentGO))
        {
            Vector3 targetWorldPos = GridToWorldPosition(newX, newY);
            Debug.Log($"[VISUAL] Agente ID {agentId} debe moverse a ({newX}, {newY}) con su corrutina");
        }
        else
        {
            Debug.LogWarning($"[VISUAL] No se encontró el Agente ID {agentId} en agentObjects.");
        }
    }

    public GameObject GetAgentGameObject(int agentId)
    {
        agentObjects.TryGetValue(agentId, out GameObject agentGO);
        return agentGO;
    }

    /// Mueve un agente a la posición exterior (fuera del tablero) correspondiente a la entrada más cercana.
    public void RespawnAgent(int agentId)
    {
        // 1. Obtener el GameObject del agente por su ID
        GameObject agentGO = GetAgentGameObject(agentId);
        if (agentGO == null)
        {
            Debug.LogWarning($"[VISUAL] No se pudo hacer Respawn del agente ID {agentId} porque no se encontró en escena.");
            return;
        }

        // 2. Obtener la casilla actual (x, y) del agente en la cuadrícula
        Vector2Int currentGridPos = WorldToGridPosition(agentGO.transform.position);

        // 3. Definir las entradas disponibles [x, y]
        List<Vector2Int> entrances = new List<Vector2Int>
        {
            new Vector2Int(1, 3),
            new Vector2Int(6, 1),
            new Vector2Int(8, 4),
            new Vector2Int(3, 6)
        };

        // 4. Encontrar la entrada físicamente más cercana mediante distancia euclidiana al cuadrado (heurística rápida)
        Vector2Int closestEntrance = entrances[0];
        float minDistance = float.MaxValue;

        foreach (Vector2Int entrance in entrances)
        {
            // Vector2Int.SqrMagnitude evita calcular la raíz cuadrada manteniendo la precisión de cercanía
            float dist = (currentGridPos - entrance).sqrMagnitude;
            if (dist < minDistance)
            {
                minDistance = dist;
                closestEntrance = entrance;
            }
        }

        // 5. Determinar la nueva posición fuera del tablero según la entrada más cercana
        Vector2Int newPos = currentGridPos; // Valor por defecto en caso de no coincidir

        if (closestEntrance == new Vector2Int(1, 3))
        {
            newPos = new Vector2Int(0, 3);
        }
        else if (closestEntrance == new Vector2Int(6, 1))
        {
            newPos = new Vector2Int(6, 0);
        }
        else if (closestEntrance == new Vector2Int(8, 4))
        {
            newPos = new Vector2Int(9, 4);
        }
        else if (closestEntrance == new Vector2Int(3, 6))
        {
            newPos = new Vector2Int(3, 7);
        }

        // 6. Convertir la casilla newPos a coordenadas de mundo
        Vector3 targetWorldPos = GridToWorldPosition(newPos.x, newPos.y);

        // Preservar la altura (Y si es 3D, Z si es 2D) original del Prefab del Agente
        if (!is2D)
        {
            targetWorldPos.y = agentGO.transform.position.y;
        }

        // 7. Mover físicamente el agente en la escena
        agentGO.transform.position = targetWorldPos;

        Debug.Log($"[VISUAL] Respawn Agente ID {agentId}: Posición anterior en Grid ({currentGridPos.x},{currentGridPos.y}) -> Entrada cercana ({closestEntrance.x},{closestEntrance.y}) -> Nueva pos fuera del tablero ({newPos.x},{newPos.y})");
    }
}