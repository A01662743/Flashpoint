using System.Collections;
using UnityEngine;
using System;

public class Fuego : MonoBehaviour
{
    public Vector3 axisRotation = Vector3.up; // (0, 1, 0) = Eje Y por defecto
    public float rotationSpeed = 60f;          // Grados por segundo
    public float amplitude = 0.35f; // Distancia del movimiento (arriba/abajo)
    public float frequency = 1.0f;   // Velocidad de la oscilación
    private Vector3 startPos;
    public int num = 0;

    [Header("Configuración de Escala")]
    [SerializeField] private float duracionCrecimiento = 0.2f;
    [SerializeField] private float duracionEncojimiento = 0.2f;
    [SerializeField] private float tiempoDeEspera = 0.2f;
    [Header("Configuración de Escala")]
    [SerializeField] private float multiplicadorEscala = 2.0f; // Crecerá al doble (200%)
    private void Awake()
    {
        escalaOriginal = transform.localScale;
    }

    // Método público para iniciar el proceso

    private Vector3 escalaOriginal;
    private Coroutine corrutinaEscalado;

    public void IniciarEfectoEscalado()
    {
        Debug.Log($"[FUEGO] IniciarEfectoEscalado() ejecutado en: '{gameObject.name}'");

        // Guardamos la escala real actual del objeto justo antes de animar
        escalaOriginal = transform.localScale;

        if (corrutinaEscalado != null)
        {
            StopCoroutine(corrutinaEscalado);
        }

        corrutinaEscalado = StartCoroutine(RutinaAgrandarYEncoger());
    }

    private IEnumerator RutinaAgrandarYEncoger()
    {
        Vector3 escalaObjetivo = escalaOriginal * multiplicadorEscala;

        Debug.Log($"[FUEGO] Escalando desde {escalaOriginal} hasta {escalaObjetivo}");

        // 1. Fase de crecimiento
        yield return StartCoroutine(CambiarEscala(escalaOriginal, escalaObjetivo, duracionCrecimiento));

        if (tiempoDeEspera > 0f)
        {
            yield return new WaitForSeconds(tiempoDeEspera);
        }

        // 2. Fase de retorno
        yield return StartCoroutine(CambiarEscala(escalaObjetivo, escalaOriginal, duracionEncojimiento));

        corrutinaEscalado = null;
    }

    private IEnumerator CambiarEscala(Vector3 inicio, Vector3 destino, float duracion)
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            
            // Calculamos el progreso estandarizado entre 0 y 1
            float porcentaje = Mathf.Clamp01(tiempoTranscurrido / duracion);
            
            // Usamos SmoothStep para suavizar el inicio y el final de la animación
            float porcentajeSuave = Mathf.SmoothStep(0f, 1f, porcentaje);

            transform.localScale = Vector3.Lerp(inicio, destino, porcentajeSuave);

            yield return null; // Espera al siguiente frame
        }

        // Aseguramos que quede exactamente en el valor final deseado
        transform.localScale = destino;
    }

    void Start()
    {
        // Asegurarse de que la velocidad de rotación sea positiva
        transform.Rotate(Vector3.forward * 20.0f);
        startPos = transform.position;
    }

    void Update()
    {
        // Rota continuamente en el eje elegido de forma suave e independiente de los FPS
        transform.Rotate(axisRotation * rotationSpeed * Time.deltaTime, Space.World);

        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

    }
}