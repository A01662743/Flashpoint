using UnityEngine;

public class TableroManager : MonoBehaviour
{
    public int ancho = 8;
    public int alto = 6;

    private int[,] tablero;

    void Start()
    {
        tablero = new int[ancho, alto];
    }

    public void AvanzarFuego()
    {
        Vector2Int objetivo = TirarDadosParaCelda();
        int estadoActual = tablero[objetivo.x, objetivo.y];

        switch (estadoActual)
        {
            case EstadosCelda.VACIA:
                ColocarHumo(objetivo);
                break;
            case EstadosCelda.HUMO:
                ColocarFuego(objetivo);
                break;
            case EstadosCelda.FUEGO:
                Debug.Log("Explosion en " + objetivo + " (pendiente de implementar)");
                break;
        }
    }

    Vector2Int TirarDadosParaCelda()
    {
        int x = Random.Range(0, ancho);
        int y = Random.Range(0, alto);
        return new Vector2Int(x, y);
    }

    void ColocarHumo(Vector2Int pos)
    {
        tablero[pos.x, pos.y] = EstadosCelda.HUMO;
        Debug.Log("Humo colocado en " + pos);
    }

    void ColocarFuego(Vector2Int pos)
    {
        tablero[pos.x, pos.y] = EstadosCelda.FUEGO;
        Debug.Log("Fuego colocado en " + pos);
    }
}