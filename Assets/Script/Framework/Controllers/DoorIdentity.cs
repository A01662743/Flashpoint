using UnityEngine;

public class DoorIdentity : MonoBehaviour
{
    [Header("Identificador del JSON")]
    [Tooltip("ID numérico que tiene esta puerta en el archivo JSON")]
    public int doorId;

    [Header("Coordenada Calculada (Solo Lectura)")]
    public Vector2Int gridPosition;

    private void Start()
    {
        var visualizer = FindObjectOfType<UnityGameVisualizer>();
        if (visualizer != null)
        {
            // Convertir la posición física 3D/2D a coordenada del tablero
            gridPosition = visualizer.WorldToGridPosition(transform.position);

            // Registrar la puerta con su ID del JSON
            visualizer.RegisterDoorObject(doorId, gameObject);
        }
        else
        {
            Debug.LogWarning($"[DoorIdentity] No se encontró UnityGameVisualizer en la escena.");
        }
    }
}