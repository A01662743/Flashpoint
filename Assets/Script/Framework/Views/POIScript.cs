using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POIScript : MonoBehaviour
{
    public Vector3 axisRotation = Vector3.up; // (0, 1, 0) = Eje Y por defecto
    public float rotationSpeed = 30f;          // Grados por segundo
    public float amplitude = 0.35f; // Distancia del movimiento (arriba/abajo)
    public float frequency = 1.0f;   // Velocidad de la oscilación
    private Vector3 startPos;
    public int num = 0;
    [Header("Modelos de Víctima")]
    [Tooltip("Lista de GameObjects hijos que representan las distintas apariencias o modelos de víctima")]
    public List<GameObject> victimModels = new List<GameObject>();
    private Coroutine carriedCOR = null;

    /// Inicia la secuencia de animación y anexa el POI al agente correspondiente.

    /// <param name="agentId">ID del agente que va a cargar este POI</param>
    /// <param name="agentTransform">Transform del agente encontrado en el visualizador</param>
    public void StartToBeCarriedPOI(int agentId, Transform agentTransform)
    {
        carriedCOR = StartCoroutine(ToBeCarriedRoutine(agentTransform));
    }

    private IEnumerator ToBeCarriedRoutine(Transform agentTransform)
    {
        // Guardar la posición inicial para el offset final
        Vector3 startPos = transform.position;
        float targetY = 15f;
        float speed = 10f;

        // 1. Elevar el POI hasta Y = 10
        while (transform.position.y < targetY)
        {
            Vector3 currentPos = transform.position;
            currentPos.y = Mathf.MoveTowards(currentPos.y, targetY, speed * Time.deltaTime);
            transform.position = currentPos;
            yield return null;
        }

        // 2. Activar un modelo de víctima aleatorio si existen en la lista
        if (victimModels != null && victimModels.Count > 0)
        {
            Debug.Log($"[POI] Activando un modelo de víctima aleatorio de {victimModels.Count} disponibles.");
            // Desactivar todos los modelos por seguridad
            foreach (GameObject model in victimModels)
            {
                if (model != null) model.SetActive(false);
            }

            // Seleccionar y activar uno al azar
            int randomIndex = Random.Range(0, victimModels.Count);
            if (victimModels[randomIndex] != null)
            {
                victimModels[randomIndex].SetActive(true);
            }
        }

        yield return new WaitForSecondsRealtime(2); // Esperar 2 segundos antes de continuar
        Vector3 targetScale = transform.localScale * 0.6f;

        while (transform.position.y > startPos.y + 1.3f)
        {
            Vector3 currentPos = transform.position;
            currentPos.y = Mathf.MoveTowards(currentPos.y, startPos.y + 1f, speed * Time.deltaTime);
            currentPos.x = Mathf.MoveTowards(currentPos.x, startPos.x + 1f, speed * Time.deltaTime);
            currentPos.z = Mathf.MoveTowards(currentPos.z, startPos.z + 1f, speed * Time.deltaTime);
            transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, speed * Time.deltaTime);
            transform.position = currentPos;
            yield return null;
        }


        // 3. Reducir proporciones al 40% (Vector3.one * 0.4f)
        transform.localScale = targetScale;

        // 4. Posicionar en la ubicación inicial con el offset (+1 en Y, +1.5 en X, +1.5 en Z)
        Vector3 targetPos = new Vector3(startPos.x + 1f, startPos.y + 1f, startPos.z + 1f);
        transform.position = targetPos;

        // 5. Emparentar el POI al Transform del agente (se moverá junto con él)
        if (agentTransform != null)
        {
            transform.SetParent(agentTransform, true); // true preserva las coordenadas del mundo al emparentar
            Debug.Log($"[POI] POI emparentado exitosamente al Agente '{agentTransform.name}'.");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

        if(carriedCOR == null)
        {
            float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

    }
}
