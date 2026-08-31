using UnityEngine;
using System.Collections;

public class Bombero : MonoBehaviour
{
    public string nombreJugador;
    public Color colorjugador;
    public bool esMiTurno = false;
    public Camera miCamara;
    public int apDisponibles = 0;

    public KeyCode izq = KeyCode.LeftArrow;
    public KeyCode der = KeyCode.RightArrow;
    public KeyCode front = KeyCode.UpArrow;
    private Coroutine bomberMoving; // Referencia a la corrutina en ejecución
    private Coroutine bomberRot; // Referencia a la corrutina en ejecución
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!esMiTurno) return;

        if (Input.GetKeyDown(izq) && bomberRot == null)
        {
            bomberRot = StartCoroutine(Rotate(-1)); // Gira el objeto 90 grados en el eje Y hacia la izquierda
        }
        if (Input.GetKeyDown(der) && bomberRot == null)
        {
            bomberRot = StartCoroutine(Rotate(1)); // Gira el objeto 90 grados en el eje Y hacia la derecha
        }
        if (Input.GetKeyDown(front) && bomberMoving == null && bomberRot == null)
        {
            bomberMoving = StartCoroutine(MoveForward());
        }
        
    }

    private IEnumerator MoveForward()
    {
        float timeElapsed = 0f;
        float timeToMove = 1f; // Tiempo que tomará el movimiento
        Vector3 currentPos = transform.position;
        Vector3 targetPos = currentPos + transform.forward* 5; // Mueve el objeto hacia adelante en la dirección que está mirando

        while (timeElapsed < timeToMove)
        {
            transform.position = Vector3.Lerp(currentPos, targetPos, timeElapsed / timeToMove);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos; // Asegurarse de que la posición final sea exacta
        bomberMoving = null; // Reinicia la referencia a la corrutina

        GastarAP(ReglasJuego.COSTO_MOVER_SIN_FUEGO);
    }

    private IEnumerator Rotate(int n)
    {
        float timeElapsed = 0f;
        float timeToMove = 1f; // Tiempo que tomará el movimiento
        Quaternion currentPos = transform.rotation;
        Quaternion targetPos = currentPos * Quaternion.Euler(0f, 90f * n, 0f); // Gira el objeto 90 grados en el eje Y


        while (timeElapsed < timeToMove)
        {
            transform.rotation = Quaternion.Lerp(currentPos, targetPos, timeElapsed / timeToMove);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = targetPos; // Asegurarse de que la posición final sea exacta
        bomberRot = null; // Reinicia la referencia a la corrutina
    }

    void GastarAP(int costo){
        apDisponibles -= costo;
        
        if (apDisponibles <= 0){
            esMiTurno = false;
            FindObjectOfType<PlayerManager>().SiguienteTurno();
        }
    }
}
