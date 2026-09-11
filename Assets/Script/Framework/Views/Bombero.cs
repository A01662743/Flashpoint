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

    [SerializeField] private float duracion = 2.0f; // Tiempo en segundos
    [SerializeField] private float alturaCurva = 10.0f; // Qué tan alto sube la curva en 'Y'

    // Función principal para iniciar el movimiento
    public void MoverA(float objetivoX, float objetivoZ)
    {
        // Usa la 'Y' del transform local del objeto/prefab
        float objetivoY = transform.position.y;

        Vector3 posicionFinal = new Vector3(objetivoX, objetivoY, objetivoZ);
        
        StopAllCoroutines(); // Opcional: detiene corrutinas previas para evitar conflictos
        StartCoroutine(MoverEnCurvaCo(posicionFinal, duracion));
    }

    // Corrutina de interpolación y arco
    private IEnumerator MoverEnCurvaCo(Vector3 destino, float tiempoTotal)
    {
        Vector3 inicio = transform.position;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < tiempoTotal)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = Mathf.Clamp01(tiempoTranscurrido / tiempoTotal);

            // Interpolación lineal entre punto A y punto B
            Vector3 posicionBase = Vector3.Lerp(inicio, destino, t);

            // Arco parabólico: sube y baja usando Mathf.Sin (máximo en t = 0.5)
            float desfaseY = Mathf.Sin(t * Mathf.PI) * alturaCurva;

            // Aplica la posición combinada
            transform.position = new Vector3(posicionBase.x, posicionBase.y + desfaseY, posicionBase.z);

            yield return null; // Espera al siguiente frame
        }

        // Asegura que quede exactamente en el destino final al terminar
        transform.position = destino;
    }

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