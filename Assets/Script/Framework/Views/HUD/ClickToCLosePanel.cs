using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Requiere que el objeto UI tenga "Raycast Target" activado en la Imagen
public class ClickToClosePanel : MonoBehaviour, IPointerClickHandler
{
    [Header("Configuración del Panel")]
    [Tooltip("Asigna el Panel a apagar. Si está vacío, buscará el Panel padre contenedor.")]
    [SerializeField] private GameObject panelToClose;

    private void Awake()
    {
        // Si no se asignó manualmente en el Inspector
        if (panelToClose == null)
        {
            // Busca hacia arriba en los padres el primer objeto con componente Image (Panel)
            // ignorando este mismo objeto
            Image[] parentImages = GetComponentsInParent<Image>(true);

            foreach (Image img in parentImages)
            {
                // Si la imagen encontrada pertenece a un objeto padre (el panel contenedor) y NO es un Canvas
                if (img.gameObject != this.gameObject && img.GetComponent<Canvas>() == null)
                {
                    panelToClose = img.gameObject;
                    break;
                }
            }

            // Respaldos opcionales si no encontró una imagen contenedora: usar el objeto padre directo
            if (panelToClose == null && transform.parent != null)
            {
                panelToClose = transform.parent.gameObject;
            }
        }
    }

    // Evento que detecta el clic sobre la UI
    public void OnPointerClick(PointerEventData eventData)
    {
        ClosePanel();
    }

    public void ClosePanel()
    {
        if (panelToClose != null)
        {
            panelToClose.SetActive(false);
        }
        else
        {
            Debug.LogWarning("No se encontró un Panel contenedor para apagar.", this);
        }
    }
}