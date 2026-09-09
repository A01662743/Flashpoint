using UnityEngine;

public interface IGameVisualizer
{
    void SpawnSmokeVisual(int x, int y);
    void PromoteSmokeToFireVisual(int x, int coyl);
    void SpawnFireVisual(int x, int y);
    void TriggerHeatUpAnimation(int x, int y);
    
    void DamageWallVisual(int wallId, int[] coordA, int[] coordB);
    void DestroyWallVisual(int wallId, int[] coordA, int[] coordB);
    void DestroyDoorVisual(int doorId);
    
    void RemovePOIVisual(int poiId, string result);
    void EliminateAgentVisual(int agentId);

    Vector3 GridToWorldPosition(int x, int y);
    Vector2Int WorldToGridPosition(Vector3 worldPos);

    void SpawnPOIVisual(int x, int y, int id);
    void RemoveSmokeVisual(int x, int y);
    void RemoveFireVisual(int x, int y);
    void RemovePOIVisual(int id);
}