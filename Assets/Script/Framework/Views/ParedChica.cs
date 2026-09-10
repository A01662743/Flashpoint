using UnityEngine;

public class ParedChica : MonoBehaviour
{
    [Header("Modelos 3D")]
    [Tooltip("GameObject del modelo en estado sano/intacto")]
    public GameObject modeloSano;

    [Tooltip("GameObject del modelo en estado dañado/destruido")]
    public GameObject modeloDanado;

    public void Destruida()
    {
        Debug.Log($"[ParedChica] Pared destruida en {gameObject.name}");

        // Desactivar modelo sano
        if (modeloSano != null)
        {
            modeloSano.SetActive(false);
        }

        // Activar modelo dañado
        if (modeloDanado != null)
        {
            modeloDanado.SetActive(true);
        }
    }
}