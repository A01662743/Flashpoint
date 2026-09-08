using System.Collections.Generic;
using UnityEngine;

public class SmokeSpawnManager : MonoBehaviour
{
    [Header("Referencias")]
    public GameStateManager stateManager;
    public MonoBehaviour visualizerObject; // Objeto que implementa IGameVisualizer
    private IGameVisualizer visualizer => visualizerObject as IGameVisualizer;

    // Direcciones de propagación: Arriba, Abajo, Izquierda, Derecha
    private readonly int[][] directions = new int[][]
    {
        new int[] { -1, 0 }, // Norte
        new int[] { 1, 0 },  // Sur
        new int[] { 0, -1 }, // Oeste
        new int[] { 0, 1 }   // Este
    };

    /// <summary>
    /// Punto de entrada principal para la lógica de Spawn Smoke
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
        // LÓGICA DE EXPLOSIÓN
        // ========================================================================

        private void TriggerExplosion(int originX, int originY)
        {
            // En cada dirección, la onda expansiva avanza independientemente
            for (int d = 0; d < directions.Length; d++)
            {
                PropagateExplosionLine(originX, originY, directions[d][0], directions[d][1]);
            }
        }

        private void PropagateExplosionLine(int startX, int startY, int dirX, int dirY)
    {
        GameState state = stateManager.CurrentState;
        int currentX = startX;
        int currentY = startY;
        bool continueLine = true;

        while (continueLine)
        {
            int nextX = currentX + dirX;
            int nextY = currentY + dirY;

            // 3.a. Verificar si la siguiente casilla está dentro de los límites del tablero
            if (!IsWithinLimits(nextX, nextY))
            {
                continueLine = false;
                break;
            }

            // 1. Verificar Paredes usando el grid y la lista 'walls'
            // Obtener la dirección (0: Norte, 1: Este, 2: Sur, 3: Oeste) de la casilla actual hacia la siguiente
            int wallBitIndex = GetWallBitIndex(dirX, dirY);

            if (HasWallInGrid(currentX, currentY, wallBitIndex))
            {
                Wall damagedWall = GetWallInDamagedList(currentX, currentY, nextX, nextY);

                if (damagedWall != null)
                {
                    // 1.a.i. Si ya estaba en 'walls' -> Estaba dañada. Se destruye totalmente.
                    // a. Actualizar grid poniendo en '0' la pared en ambas casillas adyacentes
                    RemoveWallFromGrid(currentX, currentY, wallBitIndex);
                    RemoveWallFromGrid(nextX, nextY, GetOppositeBitIndex(wallBitIndex));

                    // b. Eliminar de la lista de paredes dañadas
                    state.walls.Remove(damagedWall);

                    // c. Sumar puntos de daño global y notificar al visualizador
                    state.game.damage++;
                    visualizer?.DestroyWallVisual(damagedWall.id, damagedWall.between[0], damagedWall.between[1]);

                    // d. Detener la línea de fuego
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

                    // Detener la línea de fuego
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
                    visualizer?.DestroyDoorVisual(door.id);
                    continueLine = false;
                    break;
                }
                // Si está "open" o "destroyed", la onda de fuego continúa
            }

            // 3. Evaluar fuego/humo en la casilla objetivo
            if (IsFireAt(nextX, nextY))
            {
                visualizer?.TriggerHeatUpAnimation(nextX, nextY);
                currentX = nextX;
                currentY = nextY;
            }
            else
            {
                if (IsSmokeAt(nextX, nextY))
                {
                    RemoveSmokeAt(nextX, nextY);
                }

                AddFire(nextX, nextY);
                continueLine = false;
            }
        }
    }

    // ========================================================================
    // FUNCIONES AUXILIARES DE BÚSQUEDA Y VALIDACIÓN
    // ========================================================================

    private bool IsWithinLimits(int x, int y)
    {
        GameState state = stateManager.CurrentState;
        if (state == null || state.grid == null) return false;
        return x >= 0 && x < state.grid.Count && y >= 0 && y < state.grid[0].Count;
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

    private Wall GetWallBetween(int x1, int y1, int x2, int y2)
    {
        return stateManager.CurrentState.walls.Find(w =>
            (MatchesCoord(w.between[0], x1, y1) && MatchesCoord(w.between[1], x2, y2)) ||
            (MatchesCoord(w.between[0], x2, y2) && MatchesCoord(w.between[1], x1, y1)));
    }

    private Door GetDoorBetween(int x1, int y1, int x2, int y2)
    {
        return stateManager.CurrentState.doors.Find(d =>
            (MatchesCoord(d.between[0], x1, y1) && MatchesCoord(d.between[1], x2, y2)) ||
            (MatchesCoord(d.between[0], x2, y2) && MatchesCoord(d.between[1], x1, y1)));
    }

    private void RemoveWall(Wall wall)
    {
        stateManager.CurrentState.walls.Remove(wall);
    }

    private bool MatchesCoord(int[] coord, int x, int y)
    {
        return coord[0] == x && coord[1] == y;
    }

    // Mapeo de dirección a índice del string de 4 bits: "NESW" (0: Norte, 1: Este, 2: Sur, 3: Oeste)
    private int GetWallBitIndex(int dirX, int dirY)
    {
        if (dirX == -1 && dirY == 0) return 0; // Norte
        if (dirX == 0 && dirY == 1)  return 1; // Este
        if (dirX == 1 && dirY == 0)  return 2; // Sur
        if (dirX == 0 && dirY == -1) return 3; // Oeste
        return -1;
    }

    private int GetOppositeBitIndex(int bitIndex)
    {
        return (bitIndex + 2) % 4;
    }

    private bool HasWallInGrid(int row, int col, int bitIndex)
    {
        string cellBits = stateManager.CurrentState.grid[row][col];
        return cellBits[bitIndex] == '1';
    }

    private void RemoveWallFromGrid(int row, int col, int bitIndex)
    {
        char[] bits = stateManager.CurrentState.grid[row][col].ToCharArray();
        bits[bitIndex] = '0';
        stateManager.CurrentState.grid[row][col] = new string(bits);
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