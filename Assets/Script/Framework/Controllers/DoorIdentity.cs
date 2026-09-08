using UnityEngine;

public class DoorIdentity : MonoBehaviour
{
    [Header("Identificador del JSON")]
    [Tooltip("ID numérico que tiene esta puerta en el archivo JSON")]
    public int doorId;

    private void Start()
    {
        var visualizer = FindObjectOfType<UnityGameVisualizer>();
        if (visualizer != null)
        {
            visualizer.RegisterDoorObject(doorId, gameObject);
        }
        else
        {
            Debug.LogWarning($"[DoorIdentity] No se encontró UnityGameVisualizer en la escena para registrar la puerta con ID {doorId}.");
        }
    }
}