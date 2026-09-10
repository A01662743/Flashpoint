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

    [Header("Configuración Base")]
    public float multiplicadorEscalaBase = 1.5f;
    public float duracionCrecimiento = 0.2f;
    public float tiempoDeEspera = 0.1f;
    public float duracionEncojimiento = 0.2f;

    private Vector3 escalaOriginal;
    private Coroutine corrutinaEscalado;
    private void Awake()
    {
        escalaOriginal = transform.localScale;
    }

    // Método público para iniciar el proceso

    public void IniciarEfectoEscalado(float intensidad = 1.0f, float duracionTotal = -1f)
    {
        Debug.Log($"[FUEGO] IniciarEfectoEscalado() ejecutado en: '{gameObject.name}' | Intensidad: {intensidad}");

        // Guardamos la escala real actual del objeto solo si no estamos reinterrumpiendo una animación
        if (corrutinaEscalado == null)
        {
            escalaOriginal = transform.localScale;
        }

        if (corrutinaEscalado != null)
        {
            StopCoroutine(corrutinaEscalado);
        }

        corrutinaEscalado = StartCoroutine(RutinaAgrandarYEncoger(intensidad, duracionTotal));
    }

    private IEnumerator RutinaAgrandarYEncoger(float intensidad, float duracionTotal)
    {
        // 1. Calculamos la escala objetivo ajustada por la intensidad
        float multFinal = multiplicadorEscalaBase * intensidad;
        Vector3 escalaObjetivo = escalaOriginal * multFinal;

        // 2. Si se pasa una duración total, recalculamos proporcionalmente los tiempos
        float tCrecimiento = duracionCrecimiento;
        float tEspera = tiempoDeEspera;
        float tEncojimiento = duracionEncojimiento;

        if (duracionTotal > 0f)
        {
            float sumaTiemposBase = duracionCrecimiento + tiempoDeEspera + duracionEncojimiento;
            if (sumaTiemposBase > 0f)
            {
                float factorProporcional = duracionTotal / sumaTiemposBase;
                tCrecimiento *= factorProporcional;
                tEspera *= factorProporcional;
                tEncojimiento *= factorProporcional;
            }
        }

        // 3. Fase de crecimiento
        yield return StartCoroutine(CambiarEscala(escalaOriginal, escalaObjetivo, tCrecimiento));

        if (tEspera > 0f)
        {
            yield return new WaitForSeconds(tEspera);
        }

        // 4. Fase de retorno a la escala original
        yield return StartCoroutine(CambiarEscala(escalaObjetivo, escalaOriginal, tEncojimiento));

        corrutinaEscalado = null;
    }

    private IEnumerator CambiarEscala(Vector3 inicio, Vector3 destino, float duracion)
    {
        if (duracion <= 0f)
        {
            transform.localScale = destino;
            yield break;
        }

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float porcentaje = Mathf.Clamp01(tiempoTranscurrido / duracion);
            float porcentajeSuave = Mathf.SmoothStep(0f, 1f, porcentaje);

            transform.localScale = Vector3.Lerp(inicio, destino, porcentajeSuave);
            yield return null;
        }

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