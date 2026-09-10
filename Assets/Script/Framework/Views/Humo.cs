using System.Collections;
using UnityEngine;

public class Humo : MonoBehaviour
{
    private Coroutine corrutinaDesvanecer;

    public void DesvanecerYDestruir(float duracion = 0.3f)
    {
        if (corrutinaDesvanecer != null)
        {
            StopCoroutine(corrutinaDesvanecer);
        }

        corrutinaDesvanecer = StartCoroutine(RutinaDesvanecer(duracion));
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