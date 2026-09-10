using System.Collections.Generic;
using UnityEngine;

public class POIChoreManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] public GameStateManager stateManager; // Referencia al manager del estado
    public MonoBehaviour visualizerObject; // Objeto que implementa IGameVisualizer
    private IGameVisualizer visualizer => visualizerObject as IGameVisualizer;

    [Header("Rango del Grid")]
    [SerializeField] private int minX = 1;
    [SerializeField] private int maxX = 8;
    [SerializeField] private int minY = 1;
    [SerializeField] private int maxY = 6;
    private int FA_countdown = 5;
    private int Vic_countdown = 10;
    private bool automaticReveal = false;

    public void ProcesarPOI()
    {
        automaticReveal = false; // Reiniciar el flag de revelación automática al inicio de cada proceso
        GameState currentState = stateManager.CurrentState;

        if (currentState == null)
        {
            Debug.LogError("[POI Manager] CurrentState es nulo en StateManager.");
            return;
        }

        // 1. Verificar si siempre hay 3 registros de POI
        if (currentState.poi != null && currentState.poi.Count >= 300) /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            Debug.Log("[POI Manager] Ya existen 3 POIs en el GameState. Proceso finalizado.");
            return;
        }

        // 2. Elegir un número random para pos X & pos Y en los rangos determinados
        int randomX, randomY;
        bool posicionOcupada;

        // 2. Generar posiciones aleatorias hasta encontrar una celda sin POI existente
        do
        {
            randomX = Random.Range(minX, maxX + 1);
            randomY = Random.Range(minY, maxY + 1);

            // Verificar si ya existe un POI registrado en esas coordenadas
            posicionOcupada = currentState.poi != null && currentState.poi.Exists(p => 
                p.position != null && 
                p.position.Length >= 2 && 
                p.position[0] == randomX && 
                p.position[1] == randomY
            );

            if (posicionOcupada)
            {
                Debug.Log($"[POI Manager] La celda ({randomX}, {randomY}) ya contiene un POI. Reintentando...");
            }

        } while (posicionOcupada);

        Debug.Log($"[POI Manager] Celda libre seleccionada: ({randomX}, {randomY})");

        Debug.Log($"[POI Manager] Evaluando celda seleccionada: ({randomX}, {randomY})");

        // 3. Verificar si hay humo o fuego en esta celda y quitarlo
        VerificarYRemoverFuegoOHumo(currentState, randomX, randomY);

        // 4. Verificar si hay un bombero/agente en la celda
        Agent agenteEnCelda = currentState.agents?.Find(a => a.position != null && a.position[0] == randomX && a.position[1] == randomY);

        if (agenteEnCelda != null)
        {
            Debug.Log($"[POI Manager] Revelar POI en ({randomX}, {randomY}) al caer sobre Agente ID {agenteEnCelda.id}");

            string resultadoAleatorio = ResultadoRand();
            if (resultadoAleatorio == "ERROR") {return;}

            // Crear y añadir el POI conocido al GameState
            POI nuevoPoiRevelado = new POI
            {
                id = GenerarNuevoPoiId(currentState),
                position = new int[] { randomX, randomY },
                status = "known",
                result = resultadoAleatorio
            };

            visualizer?.SpawnPOIVisual(randomX, randomY, nuevoPoiRevelado.id);

            if (currentState.poi == null) currentState.poi = new List<POI>();
            currentState.poi.Add(nuevoPoiRevelado);

            // Modificar el estado del agente si fue víctima
            if (resultadoAleatorio == "victim" && agenteEnCelda.carrying_victim == false)
            {
                agenteEnCelda.carrying_victim = true;
                visualizer?.CarryPOI(nuevoPoiRevelado.id, agenteEnCelda.id);
                Debug.Log($"[POI Manager] Cargando víctima: Agente ID {agenteEnCelda.id} actualizado con carrying_victim = true.");
            }
            else
            {
                Debug.Log("[POI Manager] False alarm revelada. o victim sin poder cargar.");
                visualizer?.RemovePOIVisual(nuevoPoiRevelado.id);
                if (stateManager?.CurrentState?.poi != null)
                {
                    int eliminados = stateManager.CurrentState.poi.RemoveAll(p => p.id == nuevoPoiRevelado.id);
                    Debug.Log($"[STATE] POI ID {nuevoPoiRevelado.id} eliminado del GameState ({eliminados} registro(s) remido(s)).");
                }
            }

            return; // Terminar ejecución
        }

        // 5. Si no había agente, colocar el Prefab visual y guardar POI "unknown"
        string resultadoOculto = ResultadoRand();
        if (resultadoOculto == "ERROR") {return;}

        POI nuevoPoiOculto = new POI
        {
            id = GenerarNuevoPoiId(currentState),
            position = new int[] { randomX, randomY },
            status = "unknown",
            result = resultadoOculto
        };

        visualizer?.SpawnPOIVisual(randomX, randomY, nuevoPoiOculto.id);

        if (currentState.poi == null) currentState.poi = new List<POI>();
        currentState.poi.Add(nuevoPoiOculto);

        Debug.Log($"[POI Manager] Prefab de POI colocado e insertado a GameState como 'unknown' con ID {nuevoPoiOculto.id} y resultado secreto '{resultadoOculto}'.");
    }

    // --- MÉTODOS DE APOYO Y CONSULTA AL GAMESTATE ---

    private void VerificarYRemoverFuegoOHumo(GameState state, int x, int y)
    {
        //Limpieza en las listas específicas de coordenadas [x, y]
        if (state.fire != null)
        {
            int removedFire = state.fire.RemoveAll(f => f != null && f.Length >= 2 && f[0] == x && f[1] == y);
            visualizer?.RemoveFireVisual(x, y);

            if (removedFire > 0) Debug.Log($"[POI Manager] Removido de la lista 'fire' en ({x}, {y}).");
        }

        if (state.smoke != null)
        {
            int removedSmoke = state.smoke.RemoveAll(s => s != null && s.Length >= 2 && s[0] == x && s[1] == y);
            visualizer?.RemoveSmokeVisual(x, y);

            if (removedSmoke > 0) Debug.Log($"[POI Manager] Removido de la lista 'smoke' en ({x}, {y}).");
        }
    }

    private int GenerarNuevoPoiId(GameState state)
    {
        if (state.poi == null || state.poi.Count == 0) return 1;
        int maxId = 0;
        foreach (var p in state.poi)
        {
            if (p.id > maxId) maxId = p.id;
        }
        return maxId + 1;
    }

    public string ResultadoRand(){
        string resultadoAleatorio = Random.value > 0.5f ? "victim" : "false_alarm";

        if (FA_countdown <= 0 && Vic_countdown <= 0)
        {
            Debug.LogError("[POI Manager] Error: Ambos contadores (False Alarm y Victim) llegaron a 0. No hay POIs disponibles.");
            return "ERROR"; // Detiene la función por completo
        }

        // 2. Si cayó en false_alarm pero ya no quedan, forzar a victim
        if (resultadoAleatorio == "false_alarm" && FA_countdown <= 0)
        {
            resultadoAleatorio = "victim";
            Vic_countdown--;
        }
        // 3. De igual manera, si cayó en victim pero ya no quedan, forzar a false_alarm
        else if (resultadoAleatorio == "victim" && Vic_countdown <= 0)
        {
            resultadoAleatorio = "false_alarm";
            FA_countdown--;
        }
        return resultadoAleatorio;
    }
}