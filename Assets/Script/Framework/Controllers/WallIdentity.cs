using UnityEngine;

public class WallIdentity : MonoBehaviour
{
    [Header("Posición de la Pared (Entre qué dos casillas está)")]
    [Tooltip("Coordenada [fila, columna] de la primera casilla adyacente")]
    public Vector2Int cellA;

    [Tooltip("Coordenada [fila, columna] de la segunda casilla adyacente")]
    public Vector2Int cellB;

    private void Start()
    {
        // Al iniciar la escena, la pared se registra sola en el visualizador por sus coordenadas
        var visualizer = FindObjectOfType<UnityGameVisualizer>();
        if (visualizer != null)
        {
            visualizer.RegisterWallByCoordinates(cellA, cellB, gameObject);
        }
        else
        {
            Debug.LogWarning($"[WallIdentity] No se encontró UnityGameVisualizer en la escena para registrar la pared en ({cellA.x},{cellA.y}) - ({cellB.x},{cellB.y}).");
        }
    }
}