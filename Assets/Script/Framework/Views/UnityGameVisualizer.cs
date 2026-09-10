using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

// Datos de cada animación en cola
public struct HeatUpRequest
{
    public int x;
    public int y;
    public float intensity;
}

public enum VisualEventType
{
    SpawnSmoke,
    RemoveSmoke,
    SpawnFire,
    HeatUp,
    DamageWall,
    DestroyWall,
    DestroyDoor,
    RemovePOI,
    RespawnAgent
}

public struct VisualCommand
{
    public VisualEventType type;
    public int x;
    public int y;
    public int id;          // Para paredes, puertas, POIs o agentes
    public int[] target;    // Coordenadas secundarias (ej. paredes entre x1,y1 y x2,y2)
    public float intensity; // Para HeatUp
}

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

    private Queue<HeatUpRequest> heatUpQueue = new Queue<HeatUpRequest>();
    private bool isProcessingHeatUpQueue = false;

    [Header("Tiempos de Animación HeatUp")]
    [Tooltip("Duración total de la animación de Heat Up")]
    public float heatUpDuration = 1.0f;

    [Tooltip("Porcentaje de la animación actual que debe transcurrir antes de lanzar la siguiente (0.5 = 50%)")]
    [Range(0.1f, 1.0f)]
    public float overlapThreshold = 0.5f;

    private Queue<VisualCommand> visualQueue = new Queue<VisualCommand>();
    private bool isProcessingQueue = false;

    [Header("Tiempos de Animación Sequencial")]
    [Tooltip("Tiempo de espera entre la ejecución de cada evento visual")]
    public float delayBetweenEvents = 0.25f;

    // Métodos de la Interfaz: Ahora solo agregan a la cola
    public void SpawnSmokeVisual(int x, int y) 
        => EnqueueCommand(new VisualCommand { type = VisualEventType.SpawnSmoke, x = x, y = y });

    public void RemoveSmokeVisual(int x, int y) 
        => EnqueueCommand(new VisualCommand { type = VisualEventType.RemoveSmoke, x = x, y = y });

    public void SpawnFireVisual(int x, int y) 
        => EnqueueCommand(new VisualCommand { type = VisualEventType.SpawnFire, x = x, y = y });

    public void TriggerHeatUpAnimation(int x, int y, float intensity = 1.0f) 
        => EnqueueCommand(new VisualCommand { type = VisualEventType.HeatUp, x = x, y = y, intensity = intensity });

    public void DamageWallVisual(int id, int[] pos1, int[] pos2) 
        => EnqueueCommand(new VisualCommand { type = VisualEventType.DamageWall, id = id, target = new int[] { pos2[0], pos2[1] }, x = pos1[0], y = pos1[1] });

    public void DestroyWallVisual(int id, int[] pos1, int[] pos2) 
        => EnqueueCommand(new VisualCommand { type = VisualEventType.DestroyWall, id = id, target = new int[] { pos2[0], pos2[1] }, x = pos1[0], y = pos1[1] });

    public void DestroyDoorVisual(int id, int x, int y) 
        => EnqueueCommand(new VisualCommand { type = VisualEventType.DestroyDoor, id = id, x = x, y = y });

    public void RemovePOIVisual(int id) 
        => EnqueueCommand(new VisualCommand { type = VisualEventType.RemovePOI, id = id });

    public void RespawnAgent(int id) 
        => EnqueueCommand(new VisualCommand { type = VisualEventType.RespawnAgent, id = id });

    private void EnqueueCommand(VisualCommand cmd)
    {
        visualQueue.Enqueue(cmd);
        if (!isProcessingQueue)
        {
            StartCoroutine(ProcessVisualQueueRoutine());
        }
    }

    private IEnumerator ProcessVisualQueueRoutine()
    {
        isProcessingQueue = true;

        while (visualQueue.Count > 0)
        {
            VisualCommand cmd = visualQueue.Dequeue();

            // Ejecutar la acción correspondiente en Unity
            ExecuteVisualCommand(cmd);

            // Tiempo de pausa entre eventos para que sea totalmente legible
            yield return new WaitForSeconds(delayBetweenEvents);
        }

        isProcessingQueue = false;
    }

    private void ExecuteVisualCommand(VisualCommand cmd)
    {
        switch (cmd.type)
        {
            case VisualEventType.SpawnSmoke:
                ExecuteSpawnSmokeVisual(cmd.x, cmd.y);
                break;

            case VisualEventType.RemoveSmoke:
                ExecuteRemoveSmokeVisual(cmd.x, cmd.y);
                break;

            case VisualEventType.SpawnFire:
                ExecuteSpawnFireVisual(cmd.x, cmd.y);
                break;

            case VisualEventType.HeatUp:
                ExecuteTriggerHeatUpAnimation(cmd.x, cmd.y, cmd.intensity);
                break;

            case VisualEventType.DamageWall:
                ExecuteDamageWallVisual(cmd.id, new int[] { cmd.x, cmd.y }, cmd.target);
                break;

            case VisualEventType.DestroyWall:
                ExecuteDestroyWallVisual(cmd.id, new int[] { cmd.x, cmd.y }, cmd.target);
                break;

            case VisualEventType.DestroyDoor:
                ExecuteDestroyDoorVisual(cmd.id, cmd.x, cmd.y);
                break;

            case VisualEventType.RemovePOI:
                ExecuteRemovePOIVisual(cmd.id);
                break;

            case VisualEventType.RespawnAgent:
                ExecuteRespawnAgent(cmd.id);
                break;
        }
    }

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

    private void Awake()
    {
        // Awake() de TODOS los objetos de la escena corre antes que CUALQUIER
        // Start() (incluyendo el de GameStateManager, que es quien carga el
        // JSON). Suscribirse aquí, y no en Start(), es lo que elimina la
        // condición de carrera: ya no importa en qué orden Unity decida
        // ejecutar los Start() de los distintos scripts.
        if (stateManager != null)
        {
            stateManager.SubscribeToInitialState(TryRegisterInitialEntities);
        }
        else
        {
            Debug.LogError("[VISUAL] No se asignó la referencia a GameStateManager (stateManager) en el Inspector. El registro inicial de agentes/POIs/puertas nunca ocurrirá.");
        }
    }

    private void OnDestroy()
    {
        if (stateManager != null)
        {
            stateManager.UnsubscribeFromInitialState(TryRegisterInitialEntities);
        }
    }

    private void Start()
    {
        // Estos dos SÍ pueden ir en Start(): dependen únicamente de objetos
        // físicos ya presentes en la escena, no del JSON.
        RegistrarFuegosIniciales();
        RegistrarPOIsDeEscenaFisica(); // Llena POIObjectsByPos con los POI físicos de la escena
    }

    public void TryRegisterInitialEntities()
    {
        Debug.Log($"[DEBUG] TryRegisterInitialEntities llamado. stateManager null? {stateManager == null} | CurrentState null? {stateManager?.CurrentState == null}");

        if (stateManager != null && stateManager.CurrentState != null)
        {
            RegistrarAgentesIniciales(stateManager.CurrentState);
            RegistrarPOIsIniciales(stateManager.CurrentState);
            RegistrarPuertasIniciales(stateManager.CurrentState);
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
            else if (matchData == null)
            {
                Debug.LogWarning($"[VISUAL] GameObject '{agenteGO.name}' en Pos ({gridPos.x},{gridPos.y}) NO coincide con ningún agente del JSON. IDs disponibles en state.agents: {string.Join(", ", state.agents.ConvertAll(a => $"{a.id}@({a.position?[0]},{a.position?[1]})"))}");
            }
        }

        Debug.Log($"[DEBUG] RegistrarAgentesIniciales terminó. {agentObjects.Count} agentes registrados de {agentesEnEscena.Length} encontrados en escena y {state.agents.Count} en el JSON.");
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
        if (stateManager == null || stateManager.CurrentState == null || stateManager.CurrentState.fire == null)
        {
            Debug.LogWarning("[UnityGameVisualizer] No se pudo generar fuego inicial: stateManager o CurrentState.fire es null.");
            return;
        }

        foreach (int[] firePos in stateManager.CurrentState.fire)
        {
            SpawnFireVisual(firePos[0], firePos[1]);
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

    /// <summary>
    /// Busca en la escena todos los GameObjects que tengan el script PuertaPared
    /// (el contenedor "puerta con pared", que incluye las dos paredes laterales) y
    /// los vincula con su ID real del JSON. El emparejamiento se hace comparando la
    /// posición física del objeto contra el punto medio entre las dos casillas que
    /// separa la puerta (campo "between" del JSON), ya que una puerta no vive en una
    /// sola casilla sino entre dos, igual que una pared.
    /// NOTA: ningún objeto se autoregistra; este método es el único responsable de
    /// llenar doorObjects para las puertas iniciales de la partida.
    /// </summary>
    public void RegistrarPuertasIniciales(GameState state)
    {
        if (state?.doors == null)
        {
            Debug.LogWarning("[DEBUG] RegistrarPuertasIniciales: state.doors es null, no hay nada que vincular.");
            return;
        }

        PuertaPared[] puertasEnEscena = FindObjectsByType<PuertaPared>(FindObjectsSortMode.None);
        Debug.Log($"[DEBUG] RegistrarPuertasIniciales encontró {puertasEnEscena.Length} objetos con script PuertaPared en escena. JSON reporta {state.doors.Count} puertas.");

        foreach (PuertaPared puertaParedGO in puertasEnEscena)
        {
            Vector3 doorWorldPos = puertaParedGO.transform.position;

            Door matchData = null;
            float minDistSqr = float.MaxValue;

            // Busca la puerta del JSON cuyo punto medio esté más cerca de la posición del GameObject
            foreach (Door doorData in state.doors)
            {
                if (doorData.between == null || doorData.between.Count < 2) continue;

                Vector3 posA = GridToWorldPosition(doorData.between[0][0], doorData.between[0][1]);
                Vector3 posB = GridToWorldPosition(doorData.between[1][0], doorData.between[1][1]);
                Vector3 midPoint = (posA + posB) / 2f;

                float distSqr = (doorWorldPos - midPoint).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    matchData = doorData;
                }
            }

            // Se vincula directamente con la coincidencia más cercana sin validar límites de distancia
            if (matchData != null)
            {
                if (!doorObjects.ContainsKey(matchData.id))
                {
                    RegisterDoorObject(matchData.id, puertaParedGO.gameObject);
                    puertaParedGO.gameObject.name = $"Door_{matchData.id}";
                    Debug.Log($"[VISUAL] Puerta vinculada directamente: WorldPos {doorWorldPos} -> ID {matchData.id} " +
                            $"(between [{matchData.between[0][0]},{matchData.between[0][1]}]-[{matchData.between[1][0]},{matchData.between[1][1]}])");
                }
            }
        }

        Debug.Log($"[DEBUG] RegistrarPuertasIniciales terminó. {doorObjects.Count} puertas registradas.");
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

    public void ExecuteSpawnSmokeVisual(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);
        if (smokeObjects.ContainsKey(pos)) return;

        Vector3 worldPos = GridToWorldPosition(x, y);
        worldPos.y = smokePrefab.transform.position.y;
        GameObject instance = Instantiate(smokePrefab, worldPos, Quaternion.identity, transform);
        instance.name = $"Smoke_[{x},{y}]";
        smokeObjects.Add(pos, instance);
    }

    public void ExecuteSpawnFireVisual(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);
        if (fireObjects.ContainsKey(pos)) return;

        Vector3 worldPos = GridToWorldPosition(x, y);
        worldPos.y = firePrefab.transform.position.y;
        GameObject instance = Instantiate(firePrefab, worldPos, Quaternion.identity, transform);
        instance.name = $"Fire_[{x},{y}]";
        fireObjects.Add(pos, instance);
    }

    public void ExecuteTriggerHeatUpAnimation(int x, int y, float intensity = 1.0f)
    {
        // Encolar la petición de animación
        heatUpQueue.Enqueue(new HeatUpRequest { x = x, y = y, intensity = intensity });

        // Si el procesador no está activo, iniciarlo
        if (!isProcessingHeatUpQueue)
        {
            StartCoroutine(ProcessHeatUpQueueRoutine());
        }
    }
    private IEnumerator ProcessHeatUpQueueRoutine()
    {
        isProcessingHeatUpQueue = true;

        while (heatUpQueue.Count > 0)
        {
            HeatUpRequest request = heatUpQueue.Dequeue();

            // Disparar la animación visual en el GameObject correspondiente
            EjecutarEfectoVisualHeatUp(request.x, request.y, request.intensity);

            // Esperar exactamente hasta la mitad (o el tiempo configurado) antes de continuar
            float waitTime = heatUpDuration * overlapThreshold;
            yield return new WaitForSeconds(waitTime);
        }

        isProcessingHeatUpQueue = false;
    }

    private void EjecutarEfectoVisualHeatUp(int x, int y, float intensity)
    {
        // Reemplaza 'fireObjects' por el nombre de tu diccionario o lista de fuegos en el visualizador
        // Ejemplo si usas una clave Vector2Int o tu propio método de obtención:
        Vector2Int pos = new Vector2Int(x, y);

        if (fireObjects.TryGetValue(pos, out GameObject fuegoGO) && fuegoGO != null)
        {
            if (fuegoGO.TryGetComponent<Fuego>(out var fuegoScript))
            {
                fuegoScript.IniciarEfectoEscalado(intensity, heatUpDuration);
            }
        }
    }

    // ========================================================================
    // GESTIÓN DE PAREDES Y PUERTAS
    // ========================================================================

    public void ExecuteDamageWallVisual(int wallId, int[] coordA, int[] coordB)
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

    public void ExecuteDestroyWallVisual(int wallId, int[] coordA, int[] coordB)
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

        // Invalidar el caché por ID: si el ID llegara a reutilizarse en el
        // futuro (defensa adicional, el fix real está en GetNextWallId),
        // no debe apuntar a este GameObject ya destruido.
        wallsById.Remove(wallId);
    }

    public void ExecuteDestroyDoorVisual(int doorId, int x, int y)
    {
        // Si la puerta no está en el diccionario, reintentamos vincular las puertas de la escena con el JSON
        if (!doorObjects.ContainsKey(doorId))
        {
            Debug.LogWarning($"[VISUAL] Puerta ID {doorId} no encontrada en diccionario. Intentando re-registrar puertas...");
            TryRegisterInitialEntities();
        }

        if (doorObjects.TryGetValue(doorId, out GameObject doorGO) && doorGO != null)
        {
            PuertaPared puertaParedScript = doorGO.GetComponent<PuertaPared>();

            if (puertaParedScript != null)
            {
                Vector3 fireWorldPos = GridToWorldPosition(x, y);
                puertaParedScript.OpenDoorFromFire(fireWorldPos);
                Debug.Log($"[VISUAL] Reacción de Puerta ID {doorId} ante fuego iniciada desde casilla ({x}, {y}).");
            }
            else
            {
                Debug.LogWarning($"[VISUAL] Se encontró el GameObject de la Puerta ID {doorId}, pero no tiene el script PuertaPared.");
            }
        }
        else
        {
            // RESPALDO DE EMERGENCIA: Búsqueda directa por el nombre asignado o GameObject en escena
            GameObject fallbackDoor = GameObject.Find($"Door_{doorId}");
            if (fallbackDoor != null && fallbackDoor.TryGetComponent<PuertaPared>(out var scriptRespaldo))
            {
                Vector3 fireWorldPos = GridToWorldPosition(x, y);
                scriptRespaldo.OpenDoorFromFire(fireWorldPos);
                Debug.Log($"[VISUAL] Puerta ID {doorId} activada mediante búsqueda de respaldo por nombre.");
            }
            else
            {
                Debug.LogError($"[VISUAL] No se pudo encontrar ni activar la Puerta ID {doorId} en la escena.");
            }
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
    public void ExecuteRemovePOIVisual(int poiId)
    {
        Debug.Log($"[VISUAL] ExecuteRemovePOIVisual llamado para ID {poiId}. ¿Está en diccionario? {POIObjects.ContainsKey(poiId)}");
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

    public void ExecuteRemoveSmokeVisual(int x, int y)
    public void OpenDoorVisual(int doorId)
    {
        if (!doorObjects.ContainsKey(doorId))
        {
            TryRegisterInitialEntities();
        }

        if (doorObjects.TryGetValue(doorId, out GameObject doorGO) && doorGO != null)
        {
            Puerta puertaScript = doorGO.GetComponentInChildren<Puerta>();
            if (puertaScript != null)
            {
                doorGO.transform.Rotate(0f, 90f, 0f);
                Debug.Log($"[VISUAL] Puerta ID {doorId} abierta visualmente (acción normal, sin explosión).");
            }
            else
            {
                Debug.LogWarning($"[VISUAL] Puerta ID {doorId} encontrada pero sin componente Puerta.");
            }
        }
        else
        {
            Debug.LogWarning($"[VISUAL] No se pudo encontrar la Puerta ID {doorId} para abrir.");
        }
    }

    public void RemoveSmokeVisual(int x, int y)
    {
        Debug.Log($"[VISUAL] ExecuteRemoveSmokeVisual llamado para casilla ({x}, {y}).");
        Vector2Int pos = new Vector2Int(x, y);

        if (smokeObjects.TryGetValue(pos, out GameObject humoGO) && humoGO != null)
        {
            smokeObjects.Remove(pos);

            // Si tiene el script, ejecutamos la reducción de escala y autodestrucción
            if (humoGO.TryGetComponent<Humo>(out var humoScript))
            {
                humoScript.DesvanecerYDestruir(0.25f); // Ajusta la duración deseada aquí
            }
            else
            {
                Destroy(humoGO);
            }
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
    public void ExecuteRespawnAgent(int agentId)
    {
        // 1. Obtener el GameObject del agente por su ID
        GameObject agentGO = GetAgentGameObject(agentId);

        // Si no está registrado, reintentar el registro initial antes de rendirse
        if (agentGO == null)
        {
            Debug.LogWarning($"[VISUAL] Agente ID {agentId} no está en agentObjects. Reintentando registro...");
            TryRegisterInitialEntities();
            agentGO = GetAgentGameObject(agentId);
        }

        if (agentGO == null)
        {
            Debug.LogWarning($"[VISUAL] No se pudo hacer Respawn del agente ID {agentId} porque no se encontró en escena tras reintentar.");
            return;
        }

        Debug.Log($"[Propagación Fuego] Respawneando Agente");

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

        // 6. VERIFICAR Y REMOVER FUEGO EN LA NUEVA POSICIÓN
        GameState state = stateManager != null ? stateManager.CurrentState : null;
        if (state != null)
        {
            // Actualizar la posición del agente en el GameState
            Agent agentData = state.agents?.Find(a => a.id == agentId);
            if (agentData != null)
            {
                agentData.position = new int[] { newPos.x, newPos.y };
            }

            // Si hay fuego en la casilla destino, eliminarlo del estado y de la escena
            if (state.fire != null)
            {
                int removedCount = state.fire.RemoveAll(f => f != null && f.Length >= 2 && f[0] == newPos.x && f[1] == newPos.y);
                if (removedCount > 0)
                {
                    RemoveFireVisual(newPos.x, newPos.y);
                    Debug.Log($"[VISUAL] Fuego removido del estado y la escena en la casilla de respawn ({newPos.x}, {newPos.y}).");
                }
            }
        }

        // 7. Convertir la casilla newPos a coordenadas de mundo
        Vector3 targetWorldPos = GridToWorldPosition(newPos.x, newPos.y);

        // Preservar la altura (Y si es 3D, Z si es 2D) original del Prefab del Agente
        if (!is2D)
        {
            targetWorldPos.y = agentGO.transform.position.y;
        }

        // 8. Mover físicamente el agente en la escena
        agentGO.transform.position = targetWorldPos;

        Debug.Log($"[VISUAL] Respawn Agente ID {agentId}: Posición anterior en Grid ({currentGridPos.x},{currentGridPos.y}) -> Entrada cercana ({closestEntrance.x},{closestEntrance.y}) -> Nueva pos fuera del tablero ({newPos.x},{newPos.y})");
    }

    public void CarryPOI(int poiId, int agentId)
    {
        // 1. Buscar el Agente en el diccionario
        GameObject agentGO = GetAgentGameObject(agentId); // Usa agentObjects.TryGetValue internamente[cite: 10]
        
        if (agentGO == null)
        {
            Debug.LogWarning($"[VISUAL] No se encontró el Agente ID {agentId} para cargar el POI ID {poiId}.");
            return;
        }

        // 2. Buscar el POI en el diccionario
        if (POIObjects.TryGetValue(poiId, out GameObject poiGO) && poiGO != null) //[cite: 10]
        {
            if (poiGO.TryGetComponent<POIScript>(out var poiScript))
            {
                Debug.Log($"[POI Manager] Ejecutando carrying animation para el POI ID {poiId}.");
                poiScript.StartToBeCarriedPOI(agentId, agentGO.transform);
            }
            else
            {
                Debug.LogWarning($"[VISUAL] El GameObject del POI ID {poiId} no contiene el componente POIScript.");
            }
        }
        else
        {
            Debug.LogWarning($"[VISUAL] No se encontró el POI ID {poiId} en POIObjects para ser cargado por el Agente ID {agentId}.");
        }
    }
}