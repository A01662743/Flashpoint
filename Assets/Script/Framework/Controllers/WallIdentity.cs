using UnityEngine;

public class WallIdentity : MonoBehaviour
{
    public enum WallOrientationAxis { Forward, Right, Up }

    [Header("Configuración de Orientación")]
    [Tooltip("Elige qué eje local atraviesa perpendicularmente la pared hacia ambas casillas")]
    public WallOrientationAxis perpendicularAxis = WallOrientationAxis.Forward;

    [Header("Coordenadas Calculadas del Tablero (Base-1)")]
    public int cellA_Row;
    public int cellA_Col;
    public int cellB_Row;
    public int cellB_Col;
    
    public enum Estado { Intacto, Danado, Destruido }
    
    [Header("Modelos de cada estado")]
    [SerializeField] private GameObject modeloIntacto;
    [SerializeField] private GameObject modeloDanado;
    [SerializeField] private GameObject modeloDestruido;

    public void CambiarEstado(Estado nuevoEstado)
    {
        // Desactivamos todos primero
        modeloIntacto.SetActive(false);
        modeloDanado.SetActive(false);
        modeloDestruido.SetActive(false);

        // Activamos solo el correspondiente
        switch (nuevoEstado)
        {
            case Estado.Intacto:
                modeloIntacto.SetActive(true);
                break;
            case Estado.Danado:
                modeloDanado.SetActive(true);
                break;
            case Estado.Destruido:
                modeloDestruido.SetActive(true);
                break;
        }
    }
    private void Start()
    {
        CalculateGridCoordinates();
        CambiarEstado(Estado.Intacto);
    }

    public void CalculateGridCoordinates()
    {
        var visualizer = FindObjectOfType<UnityGameVisualizer>();
        if (visualizer == null)
        {
            Debug.LogWarning("[WallIdentity] No se encontró UnityGameVisualizer en la escena.");
            return;
        }

        Vector3 wallPos = transform.position;
        
        // Determinar qué eje perpendicular atraviesa la pared
        Vector3 direction = transform.forward;
        switch (perpendicularAxis)
        {
            case WallOrientationAxis.Right: direction = transform.right; break;
            case WallOrientationAxis.Up: direction = transform.up; break;
            case WallOrientationAxis.Forward: direction = transform.forward; break;
        }

        // Offset para caer dentro de las casillas adyacentes
        float offset = visualizer.cellSize * 0.35f;

        Vector3 sideAPos = wallPos + (direction * offset);
        Vector3 sideBPos = wallPos - (direction * offset);

        // Convertir posiciones de mundo a Vector2Int(row, col)
        Vector2Int gridA = visualizer.WorldToGridPosition(sideAPos);
        Vector2Int gridB = visualizer.WorldToGridPosition(sideBPos);

        // ASIGNACIÓN CLARA: Vector2Int.x es la FILA y Vector2Int.y es la COLUMNA
        cellA_Row = gridA.x;
        cellA_Col = gridA.y;
        
        cellB_Row = gridB.x;
        cellB_Col = gridB.y;

        // Registrar en el visualizador pasando (rowA, colA, rowB, colB) en orden
        visualizer.RegisterWallByCoordinates(cellA_Row, cellA_Col, cellB_Row, cellB_Col, gameObject);

        Debug.Log($"[WallIdentity] Pared '{name}' registrada entre [{cellA_Row},{cellA_Col}] y [{cellB_Row},{cellB_Col}]");
    }
}