using System.Collections.Generic;

[System.Serializable]
public class GameState
{
    public int turn;
    public string phase;
    public int current_agent;
    public string game_status;

    public List<List<string>> grid;
    public List<POI> poi;
    public List<int[]> fire;      // Lista de coordenadas [x, y]
    public List<int[]> smoke;     // Lista de coordenadas [x, y]
    public List<Door> doors;
    public List<Wall> walls;
    public List<int[]> entrances;
    public List<Agent> agents;
    public List<Reservation> reservations;
    public GameStats game;
}

[System.Serializable]
public class POI
{
    public int id;
    public int[] position;
    public string status; // Opcional ("unknown", "known", "carried", etc.)
    public string result; // "victim", "false_alarm"
}

[System.Serializable]
public class Door
{
    public int id;
    public List<int[]> between; // [[x1, y1], [x2, y2]]
    public string status;
}

[System.Serializable]
public class Wall
{
    public int id;
    public List<int[]> between; // [[x1, y1], [x2, y2]]
}

[System.Serializable]
public class Agent
{
    public int id;
    public int[] position; // [x, y]
    public int ap;
    public bool carrying_victim;
    public string status;
    public int? reserved_target; // 'int?' permite aceptar valor entero o null
}

[System.Serializable]
public class Reservation
{
    public string target_type;
    public int target_id;
    public int agent_id;
}

[System.Serializable]
public class GameStats
{
    public int rescued;
    public int lost;
    public int damage;
}