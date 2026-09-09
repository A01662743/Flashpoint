using System.Collections.Generic;
using UnityEngine;

public class SmokeSpawnManager : MonoBehaviour
{
    [Header("Referencias")]
    public GameStateManager stateManager;
    public MonoBehaviour visualizerObject; // Objeto que implementa IGameVisualizer
    private IGameVisualizer visualizer => visualizerObject as IGameVisualizer;

    // Convención única del proyecto: coordenadas siempre en (x, y), Base-1. x avanza en horizontal, y avanza en vertical.
    // Direcciones de propagación como (dx, dy): Norte, Sur, Oeste, Este
    private readonly int[][] directions = new int[][]
    {
        new int[] { 0, -1 }, // Norte (y disminuye)
        new int[] { 0, 1 },  // Sur   (y aumenta)
        new int[] { -1, 0 }, // Oeste (x disminuye)
        new int[] { 1, 0 }   // Este  (x aumenta)
    };

    /// <summary>
    /// Punto de entrada principal para la lógica de Spawn Smoke (Recibe coordenadas Base-1: x, y)
    /// </summary>
    public void ProcessSmokeSpawn(int targetX, int targetY)
    {
        GameState state = stateManager.CurrentState;
        if (state == null) return;

        // 1. Verificar si ya hay otro fuego/humo en la casilla
        if (IsFireAt(targetX, targetY))
        {
            // 1.c. Si hay fuego -> Explosión
            TriggerExplosion(targetX, targetY);
        }
        else if (IsSmokeAt(targetX, targetY))
        {
            // 1.b. Si hay humo -> Convertir a Fuego
            RemoveSmokeAt(targetX, targetY);
            AddFire(targetX, targetY);
            visualizer?.PromoteSmokeToFireVisual(targetX, targetY);
        }
        else
        {
            // 1.a. Si no hay nada -> Añadir humo
            state.smoke.Add(new int[] { targetX, targetY });
            visualizer?.SpawnSmokeVisual(targetX, targetY);
        }
    }

    // ========================================================================
    // AÑADIR FUEGO Y ELIMINACIÓN
    // ========================================================================

    private void AddFire(int x, int y)
    {
        GameState state = stateManager.CurrentState;
        state.fire.Add(new int[] { x, y });
        visualizer?.SpawnFireVisual(x, y);

        // Verificar si hay POI o Agentes en la casilla para eliminarlos
        CheckAndEliminateEntities(x, y);
    }

    private void CheckAndEliminateEntities(int x, int y)
    {
        GameState state = stateManager.CurrentState;

        // Eliminar POIs
        for (int i = state.poi.Count - 1; i >= 0; i--)
        {
            if (state.poi[i].position[0] == x && state.poi[i].position[1] == y)
            {
                POI removedPoi = state.poi[i];
                state.poi.RemoveAt(i);

                if (removedPoi.result == "victim")
                {
                    state.game.lost++;
                }

                visualizer?.RemovePOIVisual(removedPoi.id, removedPoi.result);
            }
        }

        // Eliminar/Reposicionar Agentes
        for (int i = state.agents.Count - 1; i >= 0; i--)
        {
            if (state.agents[i].position[0] == x && state.agents[i].position[1] == y)
            {
                Agent agent = state.agents[i];
                agent.status = "knocked_out";
                visualizer?.EliminateAgentVisual(agent.id);
            }
        }
    }

    // ========================================================================
    // LÓGICA DE EXPLOSIÓN INDEPENDIENTE POR DIRECCIÓN
    // ========================================================================

    private void TriggerExplosion(int originX, int originY)
    {
        // 1. Transformar el origen en fuego si no lo era ya
        if (!IsFireAt(originX, originY))
        {
            if (IsSmokeAt(originX, originY)) RemoveSmokeAt(originX, originY);
            AddFire(originX, originY);
        }

        // 2. Ejecutar cada dirección de manera totalmente independiente.
        // Al llamarse en un bucle simple, si la primera dirección (ej. Norte)
        // se detiene por pared, las llamadas a Este, Sur y Oeste corren con su propio 'continueLine'.
        for (int d = 0; d < directions.Length; d++)
        {
            PropagateExplosionLine(originX, originY, directions[d][0], directions[d][1]);
        }
    }

    private void PropagateExplosionLine(int startX, int startY, int dx, int dy)
    {
        GameState state = stateManager.CurrentState;
        int currentX = startX;
        int currentY = startY;

        // Estado de continuidad local y exclusivo para esta dirección
        bool continueLine = true;

        while (continueLine)
        {
            int nextX = currentX + dx;
            int nextY = currentY + dy;

            // 3.a. Verificar si la siguiente casilla está dentro de los límites del tablero (Base-1)
            if (!IsWithinLimits(nextX, nextY))
            {
                continueLine = false;
                break;
            }

            // 1. Verificar Paredes usando el grid y la lista 'walls' (bit en la dirección del movimiento)
            int wallBitIndex = GetWallBitIndex(dx, dy);

            if (HasWallInGrid(currentX, currentY, wallBitIndex))
            {
                Wall damagedWall = GetWallInDamagedList(currentX, currentY, nextX, nextY);

                if (damagedWall != null)
                {
                    // 1.a.i. Si ya estaba en 'walls' -> Estaba dañada. Se destruye totalmente.
                    RemoveWallFromGrid(currentX, currentY, wallBitIndex);
                    RemoveWallFromGrid(nextX, nextY, GetOppositeBitIndex(wallBitIndex));

                    state.walls.Remove(damagedWall);
                    state.game.damage++;
                    visualizer?.DestroyWallVisual(damagedWall.id, damagedWall.between[0], damagedWall.between[1]);

                    continueLine = false;
                    break;
                }
                else
                {
                    // 1.a.ii. Si NO estaba en 'walls' -> Estaba sana. Pasa a estar dañada.
                    Wall newDamagedWall = new Wall
                    {
                        id = GetNextWallId(),
                        between = new List<int[]>
                        {
                            new int[] { currentX, currentY },
                            new int[] { nextX, nextY }
                        }
                    };

                    state.walls.Add(newDamagedWall);
                    state.game.damage++;
                    visualizer?.DamageWallVisual(newDamagedWall.id, newDamagedWall.between[0], newDamagedWall.between[1]);

                    continueLine = false;
                    break;
                }
            }

            // 2. Verificar Puertas
            Door door = GetDoorBetween(currentX, currentY, nextX, nextY);
            if (door != null)
            {
                if (door.status == "closed")
                {
                    door.status = "destroyed";
                    state.game.damage++;
                    state.game.damage++;
                    Debug.Log($"[SmokeSpawn] Puerta ID {door.id} cerrada entre ({currentX},{currentY}) y ({nextX},{nextY}) -> destruida por la explosión.");
                    visualizer?.DestroyDoorVisual(door.id);
                    continueLine = false;
                    break;
                }
                else
                {
                    door.status = "destroyed";
                    state.game.damage++;
                    state.game.damage++;
                    Debug.Log($"[SmokeSpawn] Puerta ID {door.id} entre ({currentX},{currentY}) y ({nextX},{nextY}) está '{door.status}', se destruye y la línea de fuego continúa.");
                    visualizer?.DestroyDoorVisual(door.id);
                }
            }

            // 3. Evaluar fuego/humo en la casilla objetivo
            if (IsFireAt(nextX, nextY))
            {
                // Si ya hay fuego, la onda de calor atraviesa la celda y sigue propagándose sin frenar
                visualizer?.TriggerHeatUpAnimation(nextX, nextY);
                currentX = nextX;
                currentY = nextY;
            }
            else
            {
                bool hadSmoke = IsSmokeAt(nextX, nextY);

                // Si hay humo, lo remueve del estado antes de encender el fuego
                if (hadSmoke)
                {
                    RemoveSmokeAt(nextX, nextY);
                }

                // Enciende fuego en la nueva celda (vacía o donde había humo)
                AddFire(nextX, nextY);

                if (hadSmoke)
                {
                    // Notifica al visualizer para que destruya el GameObject de humo
                    // y deje el de fuego en su lugar (evita humo "fantasma" en escena)
                    visualizer?.PromoteSmokeToFireVisual(nextX, nextY);
                }

                // Al depositar fuego en la primera casilla sin fuego, la onda expansiva se consume en esta línea
                continueLine = false;
            }
        }
    }

    // ========================================================================
    // FUNCIONES AUXILIARES DE BÚSQUEDA Y VALIDACIÓN
    // Convención única: todo recibe (x, y) Base-1.
    // La única conversión a los índices internos de la matriz ocurre dentro
    // de IsWithinLimits, HasWallInGrid y RemoveWallFromGrid, de forma idéntica.
    // ========================================================================

    /// <summary>
    /// Evalúa si las coordenadas (x, y) Base-1 están dentro de la matriz
    /// </summary>
    private bool IsWithinLimits(int x, int y)
    {
        GameState state = stateManager.CurrentState;
        if (state == null || state.grid == null) return false;

        int yIndex = y - 1;
        int xIndex = x - 1;

        if (yIndex < 0 || yIndex >= state.grid.Count) return false;
        if (xIndex < 0 || xIndex >= state.grid[yIndex].Count) return false;

        return true;
    }

    private bool IsFireAt(int x, int y)
    {
        return stateManager.CurrentState.fire.Exists(f => f[0] == x && f[1] == y);
    }

    private bool IsSmokeAt(int x, int y)
    {
        return stateManager.CurrentState.smoke.Exists(s => s[0] == x && s[1] == y);
    }

    private void RemoveSmokeAt(int x, int y)
    {
        stateManager.CurrentState.smoke.RemoveAll(s => s[0] == x && s[1] == y);
    }

    private Door GetDoorBetween(int x1, int y1, int x2, int y2)
    {
        return stateManager.CurrentState.doors.Find(d =>
            (MatchesCoord(d.between[0], x1, y1) && MatchesCoord(d.between[1], x2, y2)) ||
            (MatchesCoord(d.between[0], x2, y2) && MatchesCoord(d.between[1], x1, y1)));
    }

    private bool MatchesCoord(int[] coord, int x, int y)
    {
        return coord[0] == x && coord[1] == y;
    }

    // Mapeo de dirección a índice del string de 4 bits: orden (arriba, izquierda, abajo, derecha)
    // es decir (0: Norte, 1: Oeste, 2: Sur, 3: Este)
    private int GetWallBitIndex(int dx, int dy)
    {
        if (dx == 0 && dy == -1) return 0; // Norte  (arriba)
        if (dx == -1 && dy == 0) return 1; // Oeste  (izquierda)
        if (dx == 0 && dy == 1)  return 2; // Sur    (abajo)
        if (dx == 1 && dy == 0)  return 3; // Este   (derecha)
        return -1;
    }

    private int GetOppositeBitIndex(int bitIndex)
    {
        return (bitIndex + 2) % 4;
    }

    /// <summary>
    /// Recibe (x, y) Base-1 y los convierte a los índices internos Base-0 de 'grid'
    /// para leer el bit de pared. Usa la misma conversión (yIndex = y-1, xIndex = x-1)
    /// que RemoveWallFromGrid.
    /// </summary>
    private bool HasWallInGrid(int x, int y, int bitIndex)
    {
        if (!IsWithinLimits(x, y)) return false;

        int yIndex = y - 1;
        int xIndex = x - 1;

        string cellBits = stateManager.CurrentState.grid[yIndex][xIndex];
        return cellBits[bitIndex] == '1';
    }

    /// <summary>
    /// Recibe (x, y) Base-1 y los convierte a los índices internos Base-0 de 'grid'
    /// para modificar el bit de pared. Usa la misma conversión (yIndex = y-1, xIndex = x-1)
    /// que HasWallInGrid.
    /// </summary>
    private void RemoveWallFromGrid(int x, int y, int bitIndex)
    {
        if (!IsWithinLimits(x, y)) return;

        int yIndex = y - 1;
        int xIndex = x - 1;

        char[] bits = stateManager.CurrentState.grid[yIndex][xIndex].ToCharArray();
        bits[bitIndex] = '0';
        stateManager.CurrentState.grid[yIndex][xIndex] = new string(bits);
    }

    private Wall GetWallInDamagedList(int x1, int y1, int x2, int y2)
    {
        return stateManager.CurrentState.walls.Find(w =>
            (MatchesCoord(w.between[0], x1, y1) && MatchesCoord(w.between[1], x2, y2)) ||
            (MatchesCoord(w.between[0], x2, y2) && MatchesCoord(w.between[1], x1, y1)));
    }

    private int GetNextWallId()
    {
        int maxId = 0;
        foreach (var w in stateManager.CurrentState.walls)
        {
            if (w.id > maxId) maxId = w.id;
        }
        return maxId + 1;
    }
}