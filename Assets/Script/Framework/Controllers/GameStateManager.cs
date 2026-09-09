using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }

    [Header("Configuración de Archivos")]
    [Tooltip("Nombre del archivo JSON dentro de Assets/Resources (sin la extensión .json)")]
    public string initialJsonFileName = "Initial_State";

    [Header("Referencias de Sistemas")]
    public SmokeSpawnManager smokeSpawnManager;
    public POIChoreManager POIChoreManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Carga el estado inicial al arrancar la escena
        LoadInitialState();
    }

    private void Update()
    {
        // Al presionar la tecla X en el teclado trigger smoke spawn
        if (Input.GetKeyDown(KeyCode.X))
        {
            TriggerSmokeProcess();
        }
        // Al presionar la tecla C en el teclado trigger poi chore
        if (Input.GetKeyDown(KeyCode.C))
        {
            TriggerPOIProcess();
        }
    }

    /// <summary>
    /// Dispara el proceso de generación/propagación de humo y fuego.
    /// Convención única del proyecto: coordenadas siempre en (x, y), Base-1. x avanza en horizontal, y avanza en vertical.
    /// </summary>
    public void TriggerSmokeProcess()
    {
        if (smokeSpawnManager != null)
        {
            Debug.Log("[GameManager] Presionada tecla X: Ejecutando ProcessSpawnSmoke()...");

            // Tablero: x va de 1 a 8, y va de 1 a 6
            // Random.Range para int es exclusivo en el máximo (1 a 9 -> 1..8) y (1 a 7 -> 1..6)
            int x = Random.Range(1, 9); // x: 1 a 8
            int y = Random.Range(1, 7); // y: 1 a 6

            Debug.Log($"[GameManager] Coordenadas generadas: X {x}, Y {y}");
            smokeSpawnManager.ProcessSmokeSpawn(x, y);
        }
        else
        {
            Debug.LogWarning("[GameManager] No se ha asignado la referencia a SmokeSpawnManager en el Inspector.");
        }
        Debug.LogWarning("Finalizado el ciclo smoke spawn");
    }

    public void TriggerPOIProcess()
    {
        if (POIChoreManager != null)
        {
            Debug.Log("[GameManager] Presionada tecla C: Ejecutando ProcessPOI()...");
            POIChoreManager.ProcesarPOI();
        }
        else
        {
            Debug.LogWarning("[GameManager] No se ha asignado la referencia a POIChoreManager en el Inspector.");
        }
        Debug.LogWarning("Finalizado el ciclo POI Chore");
    }

    /// <summary>
    /// Lee el archivo JSON base desde la carpeta Resources
    /// </summary>
    public void LoadInitialState()
    {
        // Carga el archivo desde Assets/Resources/Initial_State.json
        TextAsset jsonFile = Resources.Load<TextAsset>(initialJsonFileName);

        if (jsonFile != null)
        {
            LoadGameState(jsonFile.text);
            Debug.Log($"[GameStateManager] JSON inicial '{initialJsonFileName}' cargado con éxito.");

            // Asignar resultado aleatorio respetando contadores para cada POI del JSON
            if (CurrentState != null && CurrentState.poi != null)
            {
                foreach (POI poi in CurrentState.poi)
                {
                    string res = POIChoreManager.ResultadoRand();

                    if (res == "ERROR")
                    {
                        Debug.LogError("[GameStateManager] Se interrumpió la asignación de resultados en los POIs iniciales.");
                        break;
                    }

                    poi.result = res;
                }
            }
        }
        else
        {
            Debug.LogError($"[GameStateManager] No se pudo encontrar el archivo '{initialJsonFileName}' en Assets/Resources/");
        }
    }

    /// <summary>
    /// Deserializa una cadena de texto JSON y actualiza el estado interno
    /// </summary>
    public void LoadGameState(string jsonString)
    {
        try
        {
            CurrentState = Newtonsoft.Json.JsonConvert.DeserializeObject<GameState>(jsonString);

            // Opcional: Generar la representación visual inicial en el mapa
            // BuildInitialMapVisuals();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parseando el JSON: {e.Message}");
        }
    }

    private void OnGameStateUpdated()
    {
        // Aquí puedes mandar llamar funciones de actualización de la escena
        // Por ejemplo: RenderMap(), UpdateAgentsPositions(), etc.
    }

    // ========================================================================
    // MÉTODOS DE ACCESO / HELPER (Para usar desde otros scripts externamente)
    // Convención única: siempre (x, y) Base-1.
    // ========================================================================

    /// <summary>
    /// Retorna los datos de un agente por su ID
    /// </summary>
    public Agent GetAgentById(int agentId)
    {
        if (CurrentState == null) return null;
        return CurrentState.agents.Find(a => a.id == agentId);
    }

    /// <summary>
    /// Verifica si hay fuego en una coordenada específica (x, y) Base-1
    /// </summary>
    public bool HasFireAt(int x, int y)
    {
        if (CurrentState == null || CurrentState.fire == null) return false;

        foreach (var coord in CurrentState.fire)
        {
            if (coord[0] == x && coord[1] == y)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Convierte coordenadas (x, y) Base-1 a los dos índices internos Base-0
    /// que usa la matriz 'grid' (primer nivel = y-1, segundo nivel = x-1).
    /// Único lugar del proyecto donde se hace esta conversión.
    /// </summary>
    private bool TryGetGridIndices(int x, int y, out int yIndex, out int xIndex)
    {
        yIndex = y - 1;
        xIndex = x - 1;

        if (CurrentState == null || CurrentState.grid == null) return false;
        if (yIndex < 0 || yIndex >= CurrentState.grid.Count) return false;
        if (xIndex < 0 || xIndex >= CurrentState.grid[yIndex].Count) return false;

        return true;
    }

    /// <summary>
    /// Retorna la cadena de bits del tablero usando coordenadas (x, y) Base-1.
    /// </summary>
    public string GetCellWalls(int x, int y)
    {
        if (TryGetGridIndices(x, y, out int yIndex, out int xIndex))
        {
            return CurrentState.grid[yIndex][xIndex];
        }
        return null;
    }

    /// <summary>
    /// Actualiza la cadena de bits de una celda usando coordenadas (x, y) Base-1.
    /// </summary>
    public void SetCellWalls(int x, int y, string newBits)
    {
        if (TryGetGridIndices(x, y, out int yIndex, out int xIndex))
        {
            CurrentState.grid[yIndex][xIndex] = newBits;
        }
    }
}