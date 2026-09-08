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

    /// <summary>
    /// Lee el archivo JSON base desde la carpeta Resources
    /// </summary>
    public void LoadInitialState()
    {
        // Carga el archivo desde Assets/Resources/initial_state.json
        TextAsset jsonFile = Resources.Load<TextAsset>(initialJsonFileName);

        if (jsonFile != null)
        {
            LoadGameState(jsonFile.text);
            Debug.Log($"[GameStateManager] JSON inicial '{initialJsonFileName}' cargado con éxito.");
        }
        else
        {
            Debug.LogError($"[GameStateManager] No se pudo encontrar el archivo '{initialJsonFileName}.json' en Assets/Resources/");
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
    /// Verifica si hay fuego en una coordenada específica (x, y)
    /// </summary>
    public bool HasFireAt(int x, int y)
    {
        if (CurrentState == null) return false;
        
        foreach (var coord in CurrentState.fire)
        {
            if (coord[0] == x && coord[1] == y)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Convierte el string de bits del tablero a una máscara o booleano
    /// </summary>
    public string GetCellWalls(int row, int col)
    {
        if (CurrentState == null || CurrentState.grid == null) return null;
        if (row >= 0 && row < CurrentState.grid.Count && col >= 0 && col < CurrentState.grid[row].Count)
        {
            return CurrentState.grid[row][col];
        }
        return null;
    }
}