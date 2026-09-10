using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSpawnManager : MonoBehaviour
{
    [Header("Referencias")]
    public GameStateManager stateManager;
    public MonoBehaviour visualizerObject; // Objeto que implementa IGameVisualizer
    private IGameVisualizer visualizer => visualizerObject as IGameVisualizer;

    private readonly int[][] directions = new int[][]
    {
        new int[] { 0, -1 }, // Norte
        new int[] { 0, 1 },  // Sur
        new int[] { -1, 0 }, // Oeste
        new int[] { 1, 0 }   // Este
    };

    public void ProcessSmokeSpawn(int targetX, int targetY)
    {
        GameState state = stateManager.CurrentState;
        if (state == null) return;

        // 1. Si cae directamente en un fuego existente -> HeatUp reforzado y Explosión
        if (IsFireAt(targetX, targetY))
        {
            // Enviamos intensidad aumentada (ejemplo 2.0f) al fuego impactado directamente
            visualizer?.TriggerHeatUpAnimation(targetX, targetY, intensity: 2.0f);
            TriggerExplosion(targetX, targetY);
        }
        // 2. Si hay humo -> Transforma el humo en fuego
        else if (IsSmokeAt(targetX, targetY))
        {
            AddFire(targetX, targetY);
        }
        // 3. Casilla vacía -> Humo
        else
        {
            state.smoke.Add(new int[] { targetX, targetY });
            visualizer?.SpawnSmokeVisual(targetX, targetY);
        }

        //reacción en cadena
        ProcessSmokeIgnitionChain();
    }

    // ========================================================================
    // AÑADIR FUEGO Y ELIMINACIÓN DE ENTIDADES/HUMO
    // ========================================================================

    private void AddFire(int x, int y)
    {
        GameState state = stateManager.CurrentState;

        if (IsSmokeAt(x, y))
        {
            RemoveSmokeAt(x, y);
            visualizer?.RemoveSmokeVisual(x, y);
        }

        if (!IsFireAt(x, y))
        {
            state.fire.Add(new int[] { x, y });
            visualizer?.SpawnFireVisual(x, y);
            
            // Ejecutar HeatUp al spawnear fuego
            visualizer?.TriggerHeatUpAnimation(x, y);
        }

        CheckAndEliminateEntities(x, y);
    }

    private void CheckAndEliminateEntities(int x, int y)
    {
        GameState state = stateManager.CurrentState;
        if (state == null) return;

        // 1. Eliminar POIs en las coordenadas
        if (state.poi != null)
        {
            for (int i = state.poi.Count - 1; i >= 0; i--)
            {
                if (state.poi[i].position != null && 
                    state.poi[i].position.Length >= 2 &&
                    state.poi[i].position[0] == x && 
                    state.poi[i].position[1] == y)
                {
                    POI poiAfectado = state.poi[i];
                    Debug.Log($"[DEBUG] POI a eliminar: {poiAfectado.id} | visualizer null? {visualizer == null}");

                    // Notificar primero al visualizador antes de remover del estado
                    visualizer?.RemovePOIVisual(poiAfectado.id);

                    if (poiAfectado.result == "victim" && state.game != null)
                    {
                        state.game.lost++;
                        Debug.Log($"[Propagación Fuego] Víctima perdida en ({x}, {y}). Total perdidas: {state.game.lost}");
                    }

                    state.poi.RemoveAt(i);
                }
            }
        }

        // 2. Eliminar / Dejar fuera de combate a los Agentes
        if (state.agents != null)
        {
            for (int i = state.agents.Count - 1; i >= 0; i--)
            {
                if (state.agents[i].position != null && 
                    state.agents[i].position.Length >= 2 &&
                    state.agents[i].position[0] == x && 
                    state.agents[i].position[1] == y)
                {
                    Agent agent = state.agents[i];
                    visualizer?.RespawnAgent(agent.id);
                    Debug.Log($"[Propagación Fuego] Agente ID {agent.id} alcanzado por fuego en ({x}, {y}).");
                }
            }
        }
    }

    // ========================================================================
    // EXPLOSIÓN
    // ========================================================================

    private void TriggerExplosion(int originX, int originY)
    {
        if (!IsFireAt(originX, originY))
        {
            AddFire(originX, originY);
        }

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

        bool continueLine = true;

        while (continueLine)
        {
            int nextX = currentX + dx;
            int nextY = currentY + dy;

            if (!IsWithinLimits(currentX, currentY))
            {
                Debug.Log($"[LOGICA-PARED] (síncrono) Línea cortada: ({currentX},{currentY}) está fuera de límites del grid.");
                continueLine = false;
                break;
            }

            bool isNextOutside = !IsWithinLimits(nextX, nextY);

            if (isNextOutside && !IsOneStepOutsideGrid(nextX, nextY))
            {
                Debug.Log($"[LOGICA-PARED] (síncrono) Línea cortada: siguiente casilla ({nextX},{nextY}) está fuera de los límites permitidos (ni siquiera un paso afuera).");
                continueLine = false;
                break;
            }

            int wallBitIndex = GetWallBitIndex(dx, dy);

            if (IsWithinLimits(currentX, currentY) && HasWallInGrid(currentX, currentY, wallBitIndex))
            {
                Wall damagedWall = GetWallInDamagedList(currentX, currentY, nextX, nextY);

                if (damagedWall != null)
                {
                    Debug.Log($"[LOGICA-PARED] (síncrono) Pared YA dañada entre ({currentX},{currentY}) y ({nextX},{nextY}) -> se destruye ahora. ID {damagedWall.id}. Encolando visual.");

                    RemoveWallFromGrid(currentX, currentY, wallBitIndex);
                    if (IsWithinLimits(nextX, nextY))
                    {
                        RemoveWallFromGrid(nextX, nextY, GetOppositeBitIndex(wallBitIndex));
                    }

                    state.walls.Remove(damagedWall);
                    state.game.damage++;
                    visualizer?.DestroyWallVisual(damagedWall.id, damagedWall.between[0], damagedWall.between[1]);

                    continueLine = false;
                    break;
                }
                else
                {
                    Wall newDamagedWall = new Wall
                    {
                        id = GetNextWallId(),
                        between = new List<int[]>
                        {
                            new int[] { currentX, currentY },
                            new int[] { nextX, nextY }
                        }
                    };

                    Debug.Log($"[LOGICA-PARED] (síncrono) Pared detectada e intacta entre ({currentX},{currentY}) y ({nextX},{nextY}) -> se marca dañada ahora. ID nuevo {newDamagedWall.id}. Encolando visual.");

                    state.walls.Add(newDamagedWall);
                    state.game.damage++;
                    visualizer?.DamageWallVisual(newDamagedWall.id, newDamagedWall.between[0], newDamagedWall.between[1]);

                    continueLine = false;
                    break;
                }
            }

            Door door = GetDoorBetween(currentX, currentY, nextX, nextY);
            if (door != null)
            {
                if (door.status == "closed")
                {
                    door.status = "destroyed";
                    state.game.damage += 2;
                    Debug.Log($"[Propagación Fuego] Puerta ID {door.id} destruida entre ({currentX}, {currentY}) y ({nextX}, {nextY}).");
                    visualizer?.DestroyDoorVisual(door.id, currentX, currentY);
                    continueLine = false;
                    break;
                }
            }

            if (IsFireAt(nextX, nextY))
            {
                visualizer?.TriggerHeatUpAnimation(nextX, nextY);
                currentX = nextX;
                currentY = nextY;

                if (isNextOutside)
                {
                    continueLine = false;
                }
            }
            else
            {
                AddFire(nextX, nextY);
                continueLine = false;
            }
        }
    }

    private bool IsOneStepOutsideGrid(int x, int y)
    {
        bool validX = x >= 0 && x <= 9; 
        bool validY = y >= 0 && y <= 7;
        return validX && validY;
    }

    // ========================================================================
    // REACCIÓN EN CADENA DE IGNICIÓN DE HUMO
    // ========================================================================

    public void ProcessSmokeIgnitionChain()
    {
        GameState state = stateManager.CurrentState;
        if (state == null || state.smoke == null || state.smoke.Count == 0) return;

        Queue<int[]> newlyCreatedFires = new Queue<int[]>();

        // 1. Clonar la lista de humos actual para evaluar la condición inicial
        List<int[]> initialSmokes = new List<int[]>(state.smoke);

        foreach (int[] smokePos in initialSmokes)
        {
            int sx = smokePos[0];
            int sy = smokePos[1];

            // Validar si el humo aún existe (por si fue removido en una iteración previa)
            if (!IsSmokeAt(sx, sy)) continue;

            // Verificar si hay algún fuego adyacente que lo encienda
            if (HasAdjacentFire(sx, sy))
            {
                PromoteSmokeToFire(sx, sy);
                newlyCreatedFires.Enqueue(new int[] { sx, sy });
            }
        }

        // 2. Propagación en cadena para humos contiguos
        while (newlyCreatedFires.Count > 0)
        {
            int[] currentFire = newlyCreatedFires.Dequeue();
            int fx = currentFire[0];
            int fy = currentFire[1];

            foreach (int[] dir in directions)
            {
                int nx = fx + dir[0];
                int ny = fy + dir[1];

                if (IsSmokeAt(nx, ny) && CanFireReachSmoke(fx, fy, nx, ny))
                {
                    PromoteSmokeToFire(nx, ny);
                    newlyCreatedFires.Enqueue(new int[] { nx, ny });
                }
            }
        }
    }

    private bool HasAdjacentFire(int sx, int sy)
    {
        foreach (int[] dir in directions)
        {
            int fx = sx + dir[0];
            int fy = sy + dir[1];

            if (IsFireAt(fx, fy) && CanFireReachSmoke(fx, fy, sx, sy))
            {
                return true;
            }
        }
        return false;
    }

    private bool CanFireReachSmoke(int fireX, int fireY, int smokeX, int smokeY)
    {
        int dx = smokeX - fireX;
        int dy = smokeY - fireY;

        // Mapear la dirección Fuego -> Humo para obtener la pared del lado del Fuego
        int wallBitIndex = GetWallBitIndex(dx, dy);

        // Si la dirección es válida y hay pared desde la casilla del fuego
        if (wallBitIndex != -1 && HasWallInGrid(fireX, fireY, wallBitIndex))
        {
            return false;
        }

        // Mapear la dirección opuesta para validar la pared del lado del Humo
        if (wallBitIndex != -1 && HasWallInGrid(smokeX, smokeY, GetOppositeBitIndex(wallBitIndex)))
        {
            return false;
        }

        // Verificar si hay puerta cerrada entre ambos
        Door door = GetDoorBetween(fireX, fireY, smokeX, smokeY);
        if (door != null && door.status == "closed")
        {
            return false; 
        }

        return true;
    }

    // DEJARLO SIMPLIFICADO ASÍ:
    private void PromoteSmokeToFire(int x, int y)
    {
        AddFire(x, y);
        Debug.Log($"[Reacción en Cadena] Humo en ({x}, {y}) se convirtió en Fuego.");
    }

    // ========================================================================
    // MÉTODOS AUXILIARES Y BÚSQUEDAS
    // ========================================================================

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

    // Orden real de los bits en el grid: (arriba, izquierda, derecha, abajo)
    // -> índices (0, 1, 2, 3). OJO: no es un ciclo donde el opuesto esté a
    // "+2" de distancia (ver GetOppositeBitIndex).
    private int GetWallBitIndex(int dx, int dy)
    {
        if (dx == 0 && dy == -1) return 0; // Norte -> arriba
        if (dx == -1 && dy == 0) return 1; // Oeste -> izquierda
        if (dx == 0 && dy == 1)  return 2; // Sur   -> abajo
        if (dx == 1 && dy == 0)  return 3; // Este  -> derecha
        return -1;
    }

    private int GetOppositeBitIndex(int bitIndex)
    {
        switch (bitIndex)
        {
            case 0: return 2; // arriba <-> abajo
            case 1: return 3; // izquierda <-> derecha
            case 2: return 0; // abajo <-> arriba
            case 3: return 1; // derecha <-> izquierda
            default: return -1;
        }
    }

    private bool HasWallInGrid(int x, int y, int bitIndex)
    {
        if (!IsWithinLimits(x, y)) return false;

        int yIndex = y - 1;
        int xIndex = x - 1;

        string cellBits = stateManager.CurrentState.grid[yIndex][xIndex];
        return cellBits[bitIndex] == '1';
    }

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

    // Contador monotónico: NUNCA se reutilizan IDs, aunque una pared se
    // elimine de state.walls al ser destruida. Reutilizar IDs causaba que
    // el visualizador (que cachea GameObject por ID) asociara paredes nuevas
    // con GameObjects de paredes viejas en otras coordenadas.
    private int nextWallIdCounter = -1;

    private int GetNextWallId()
    {
        if (nextWallIdCounter < 0)
        {
            int maxId = 0;
            foreach (var w in stateManager.CurrentState.walls)
            {
                if (w.id > maxId) maxId = w.id;
            }
            nextWallIdCounter = maxId;
        }

        nextWallIdCounter++;
        return nextWallIdCounter;
    }
}