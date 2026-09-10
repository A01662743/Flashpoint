using UnityEngine;

public class PuertaPared : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenDoorFromFire(Vector3 fireWorldPos)
    {
        Debug.Log($"[PuertaPared] Abrir puerta desde fuego en posición: {fireWorldPos}");

        // 1. Buscar y animar la Puerta hija
        Puerta puertaHija = GetComponentInChildren<Puerta>();
        if (puertaHija != null)
        {
            puertaHija.OpenDoorFromFire(fireWorldPos);
        }
        else
        {
            Debug.LogWarning("[PuertaPared] No se encontró el componente Puerta en ningún hijo.");
        }

        // 2. Buscar y destruir las DOS paredes chicas hijas
        ParedChica[] paredesChicas = GetComponentsInChildren<ParedChica>();
        
        if (paredesChicas.Length > 0)
        {
            foreach (ParedChica pared in paredesChicas)
            {
                pared.Destruida();
            }
        }
        else
        {
            Debug.LogWarning("[PuertaPared] No se encontraron componentes ParedChica en los hijos.");
        }
    }
}
