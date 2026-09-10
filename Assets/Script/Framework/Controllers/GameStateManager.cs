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

    public void TriggerSmokeProcess()
    {
        if (smokeSpawnManager != null)
        {
            Debug.Log("[GameManager] Presionada tecla X: Ejecutando ProcessSpawnSmoke()...");
            int x = Random.Range(1, 9);
            int y = Random.Range(1, 7);
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

    public void LoadGameState(string jsonString)
    {
        try
        {
            CurrentState = Newtonsoft.Json.JsonConvert.DeserializeObject<GameState>(jsonString);

            Bombero[] bomberos = FindObjectsOfType<Bombero>();
            foreach (Bombero b in bomberos)
            {
                Agent agenteData = CurrentState.agents.Find(a => a.id == b.agentId);
                if (agenteData != null)
                {
                    b.SincronizarPosicionInicial(agenteData.position[0], agenteData.position[1]);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parseando el JSON: {e.Message}");
        }
    }

    private void OnGameStateUpdated()
    {
        // Aquí puedes mandar llamar funciones de actualización de la escena
    }

    public Agent GetAgentById(int agentId)
    {
        if (CurrentState == null) return null;
        return CurrentState.agents.Find(a => a.id == agentId);
    }

    public void SetCurrentAgent(int agentId)
    {
        if(CurrentState != null)
        {
            CurrentState.current_agent = agentId;
        }
    }

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

    private bool TryGetGridIndices(int x, int y, out int yIndex, out int xIndex)
    {
        yIndex = y - 1;
        xIndex = x - 1;

        if (CurrentState == null || CurrentState.grid == null) return false;
        if (yIndex < 0 || yIndex >= CurrentState.grid.Count) return false;
        if (xIndex < 0 || xIndex >= CurrentState.grid[yIndex].Count) return false;

        return true;
    }

    public string GetCellWalls(int x, int y)
    {
        if (TryGetGridIndices(x, y, out int yIndex, out int xIndex))
        {
            return CurrentState.grid[yIndex][xIndex];
        }
        return null;
    }

    public void SetCellWalls(int x, int y, string newBits)
    {
        if (TryGetGridIndices(x, y, out int yIndex, out int xIndex))
        {
            CurrentState.grid[yIndex][xIndex] = newBits;
        }
    }

    // ========================================================================
    // NUEVO: Aplicar acciones recibidas de Python
    // ========================================================================

    public void AplicarAccion(int agentId, GameAction accion)
    {
        if (CurrentState == null) return;

        Agent agente = GetAgentById(agentId);
        if (agente == null)
        {
            Debug.LogWarning($"[GameStateManager] No se encontró el agente {agentId} para aplicar la acción.");
            return;
        }

        switch (accion.type)
        {
            case "move":
            case "move_with_victim":
                agente.position = new int[] { accion.to[0], accion.to[1] };
                agente.ap = accion.remaining_ap;
                break;

            case "open_door":
                AbrirPuertaEnEstado(accion.between);
                agente.ap = accion.remaining_ap;
                break;

            case "extinguish_fire":
                CurrentState.fire.RemoveAll(f => f[0] == accion.position[0] && f[1] == accion.position[1]);
                FindObjectOfType<UnityGameVisualizer>()?.RemoveFireVisual(accion.position[0], accion.position[1]);
                if(!CurrentState.smoke.Exists(s => s[0] == accion.position[0] && s[1] == accion.position[1]))
                {
                    CurrentState.smoke.Add(new int[] { accion.position[0], accion.position[1] });
                    FindObjectOfType<UnityGameVisualizer>()?.SpawnSmokeVisual(accion.position[0], accion.position[1]);
                }
                agente.ap = accion.remaining_ap;
                break;

            case "clear_smoke":
                CurrentState.smoke.RemoveAll(s => s[0] == accion.position[0] && s[1] == accion.position[1]);
                agente.ap = accion.remaining_ap;
                break;

            case "damage_Wall":
                DañarOPactualizarPared(accion.between);
                agente.ap = accion.remaining_ap;
                break;

            case "pickup_victim":
                CurrentState.poi.RemoveAll(p => p.id == accion.poi_id);
                agente.carrying_victim = true;
                break;

            case "reveal_poi":
                POI poi = CurrentState.poi.Find(p => p.id == accion.poi_id);
                if (poi != null) poi.status = "known";
                break;

            case "rescue_victim":
                agente.carrying_victim = false;
                CurrentState.game.rescued++;
                break;

            default:
                Debug.LogWarning($"[GameStateManager] Acción no reconocida: {accion.type}");
                break;
        }
    }

    private void AbrirPuertaEnEstado(List<List<int>> between)
    {
        int x1 = between[0][0], y1 = between[0][1];
        int x2 = between[1][0], y2 = between[1][1];

        Door puerta = CurrentState.doors.Find(d =>
            (d.between[0][0] == x1 && d.between[0][1] == y1 && d.between[1][0] == x2 && d.between[1][1] == y2) ||
            (d.between[0][0] == x2 && d.between[0][1] == y2 && d.between[1][0] == x1 && d.between[1][1] == y1));

        if (puerta != null)
        {
            puerta.status = "open";
            FindObjectOfType<UnityGameVisualizer>()?.OpenDoorVisual(puerta.id);
        }
    }

    private void DañarOPactualizarPared(List<List<int>> between)
    {
        int x1 = between[0][0], y1 = between[0][1];
        int x2 = between[1][0], y2 = between[1][1];

        Wall existente = CurrentState.walls.Find(w =>
            (w.between[0][0] == x1 && w.between[0][1] == y1 && w.between[1][0] == x2 && w.between[1][1] == y2) ||
            (w.between[0][0] == x2 && w.between[0][1] == y2 && w.between[1][0] == x1 && w.between[1][1] == y1));

        if (existente != null)
        {
            CurrentState.walls.Remove(existente);
            RemoverBitDePared(x1, y1, x2, y2);
            CurrentState.game.damage++;
        }
        else
        {
            CurrentState.walls.Add(new Wall
            {
                id = CurrentState.walls.Count + 1,
                between = new List<int[]> { new int[] { x1, y1 }, new int[] { x2, y2 } }
            });
            CurrentState.game.damage++;
        }
    }

    private void RemoverBitDePared(int x1, int y1, int x2, int y2)
    {
        string cell1 = GetCellWalls(x1, y1);
        string cell2 = GetCellWalls(x2, y2);
        if (cell1 == null || cell2 == null) return;

        char[] bits1 = cell1.ToCharArray();
        char[] bits2 = cell2.ToCharArray();

        if (y2 == y1 + 1) { bits1[2] = '0'; bits2[0] = '0'; }
        else if (x2 == x1 - 1) { bits1[1] = '0'; bits2[3] = '0'; }
        else if (y2 == y1 - 1) { bits1[0] = '0'; bits2[2] = '0'; }
        else if (x2 == x1 + 1) { bits1[3] = '0'; bits2[1] = '0'; }

        SetCellWalls(x1, y1, new string(bits1));
        SetCellWalls(x2, y2, new string(bits2));
    }
}