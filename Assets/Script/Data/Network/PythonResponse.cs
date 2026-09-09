using System.Collections.Generic;

[System.Serializable]
public class GameAction
{
    public int order;
    public string type;
    public List<int> from;
    public List<int> to;
    public List<List<int>> between;
    public List<int> position;
    public int poi_id;
    public int cost;
    public int remaining_ap;
}

[System.Serializable]
public class PythonToUnityData
{
    public int agent_id;
    public List<GameAction> actions;
}