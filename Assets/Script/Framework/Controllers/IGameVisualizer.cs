using UnityEngine;

public interface IGameVisualizer
{
    void SpawnSmokeVisual(int row, int col);
    void PromoteSmokeToFireVisual(int row, int col);
    void SpawnFireVisual(int row, int col);
    void TriggerHeatUpAnimation(int row, int col);
    
    void DamageWallVisual(int wallId, int[] coordA, int[] coordB);
    void DestroyWallVisual(int wallId, int[] coordA, int[] coordB);
    void DestroyDoorVisual(int doorId);
    
    void RemovePOIVisual(int poiId, string result);
    void EliminateAgentVisual(int agentId);

    Vector3 GridToWorldPosition(int row, int col);
    Vector2Int WorldToGridPosition(Vector3 worldPos);
}