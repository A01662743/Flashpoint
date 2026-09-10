using System.Collections;
using UnityEngine;

public class Puerta : MonoBehaviour
{
    private Coroutine doorAnimCOR;

    /// <summary>
    /// Activa la animación pasando la posición en el mundo del fuego que la detonó.
    /// </summary>
    public void OpenDoorFromFire(Vector3 fireWorldPos)
    {
        if (doorAnimCOR == null)
        {
            doorAnimCOR = StartCoroutine(AnimateDoor(fireWorldPos));
        }
    }

    private IEnumerator AnimateDoor(Vector3 fireWorldPos)
    {
        // 1. Calcular dirección hacia el fuego
        Vector3 dirToFire = (fireWorldPos - transform.position).normalized;

        // 2. Determinar si está al frente o atrás de la puerta
        float dot = Vector3.Dot(transform.forward, dirToFire);
        float targetAngleX = (dot >= 0) ? -90f : 90f;

        // 3. Guardar estado inicial y objetivo
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + new Vector3(0f, 0.2f, 0f);

        Quaternion startRot = transform.rotation;
        Quaternion targetRot = startRot * Quaternion.Euler(targetAngleX, 0f, 0f);

        float duration = 0.4f; // Tiempo de animación en segundos
        float elapsed = 0f;

        // 4. Interpola posición y rotación progresivamente
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;
        doorAnimCOR = null;
    }
}