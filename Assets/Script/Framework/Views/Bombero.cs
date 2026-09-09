using UnityEngine;
using System.Collections;

public class Bombero : MonoBehaviour
{
    public string nombreJugador;
    public Color colorjugador;
    public bool esMiTurno = false;
    public Camera miCamara;
    public int apDisponibles = 0;
    public int agentId; // <- NUEVO: debe coincidir con Agent.id en el GameState

    private UnityGameVisualizer visualizer;
    private Coroutine bomberMoving;

    void Start()
    {
        visualizer = FindObjectOfType<UnityGameVisualizer>();
    }

    // Punto de entrada: se llama por cada acción que Python decide para este agente
    public void EjecutarAccion(GameAction accion, System.Action onCompletada)
    {
        switch (accion.type)
        {
            case "move":
            case "move_with_victim":
                StartCoroutine(MoverAPosicion(accion.to, onCompletada));
                break;

            default:
                // el resto de acciones (open_door, extinguish_fire, pickup_victim, etc.)
                // no mueven al bombero visualmente, solo consumen tiempo si quieres animación futura
                onCompletada?.Invoke();
                break;
        }

        apDisponibles = accion.remaining_ap;
    }
    public void SincronizarPosicionInicial(int x, int y)
    {
        if(visualizer == null)
        
            visualizer = FindObjectOfType<UnityGameVisualizer>();
        transform.position = visualizer.GridToWorldPosition(x, y);
    }
    private IEnumerator MoverAPosicion(System.Collections.Generic.List<int> destino, System.Action onCompletada)
    {
        float timeElapsed = 0f;
        float timeToMove = 1f;
        Vector3 currentPos = transform.position;
        Vector3 targetPos = visualizer.GridToWorldPosition(destino[0], destino[1]);

        while (timeElapsed < timeToMove)
        {
            transform.position = Vector3.Lerp(currentPos, targetPos, timeElapsed / timeToMove);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        bomberMoving = null;
        onCompletada?.Invoke();
    }
}