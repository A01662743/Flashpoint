using System.Collections;
using UnityEngine;

public class Humo : MonoBehaviour
{
    private Coroutine corrutinaAnimacion;

    [Header("Configuración de Flotación y Rotación")]
    [Tooltip("Velocidad de rotación constante en los tres ejes")]
    public Vector3 velocidadRotacion = new Vector3(15f, 25f, 10f);

    [Tooltip("Velocidad de la oscilación vertical")]
    public float velocidadFlotacion = 2f;

    [Tooltip("Amplitud del movimiento (sube y baja en un rango de 0.5 unidades)")]
    public float amplitudFlotacion = 0.5f;

    private Vector3 posicionInicial;

    private void Start()
    {
        posicionInicial = transform.position;
        Aparecer(0.3f);
    }

    private void Update()
    {
        // 1. Rotación lenta en todos los ángulos
        transform.Rotate(velocidadRotacion * Time.deltaTime);

        // 2. Movimiento vertical usando función Seno (sube y baja 0.5 en total: +0.25 a -0.25)
        float nuevoY = posicionInicial.y + Mathf.Sin(Time.time * velocidadFlotacion) * (amplitudFlotacion / 2f);
        transform.position = new Vector3(transform.position.x, nuevoY, transform.position.z);
    }

    // ========================================================================
    // CORRUTINAS DE ESCALA
    // ========================================================================

    public void Aparecer(float duracion = 0.3f)
    {
        if (corrutinaAnimacion != null)
        {
            StopCoroutine(corrutinaAnimacion);
        }

        corrutinaAnimacion = StartCoroutine(RutinaAparecer(duracion));
    }

    public void DesvanecerYDestruir(float duracion = 0.3f)
    {
        if (corrutinaAnimacion != null)
        {
            StopCoroutine(corrutinaAnimacion);
        }

        corrutinaAnimacion = StartCoroutine(RutinaDesvanecer(duracion));
    }

    private IEnumerator RutinaAparecer(float duracion)
    {
        Vector3 escalaObjetivo = new Vector3(2f, 2f, 2f);
        Vector3 escalaInicial = transform.localScale; // Por si inicia desde cero o un valor previo
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float porcentaje = Mathf.Clamp01(tiempoTranscurrido / duracion);

            transform.localScale = Vector3.Lerp(escalaInicial, escalaObjetivo, porcentaje);
            yield return null;
        }

        transform.localScale = escalaObjetivo;
    }

    private IEnumerator RutinaDesvanecer(float duracion)
    {
        Vector3 escalaInicial = transform.localScale;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float porcentaje = Mathf.Clamp01(tiempoTranscurrido / duracion);
            
            transform.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, porcentaje);
            yield return null;
        }

        transform.localScale = Vector3.zero;
        Destroy(gameObject);
    }
}