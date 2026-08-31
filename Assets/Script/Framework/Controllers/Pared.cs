using UnityEngine;

public class Pared : MonoBehaviour
{
    public Vector2Int celdaA;

    public int estado = EstadosCelda.SANA;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RecibirDanio(){
        if(estado == EstadosCelda.SANA) {
            estado = EstadosCelda.DANIADA;
        } else if (estado == EstadosCelda.DANIADA){
            estado = EstadosCelda.ROTA;
            Destroy(gameObject);
        }
    }
}
