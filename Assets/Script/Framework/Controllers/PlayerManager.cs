using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public List<Bombero> bomberos = new List<Bombero>();
    public int turnoActual = 0;
    public TableroManager tableroManager;

    public event Action<Bombero> CambiodeTurno;
    private bool primerTurnoNotificado = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       NotificarTurno(); 
    }
        // Update is called once per frame
    void Update()
    {
        if(!primerTurnoNotificado){
            NotificarTurno();
            primerTurnoNotificado = true;
        }
        
    }

    public Bombero ObtenerBomberoenTurno(){
        return bomberos[turnoActual];
    }

    public void SiguienteTurno(){
        tableroManager.AvanzarFuego();

        turnoActual = (turnoActual + 1) % bomberos.Count;
        NotificarTurno();
    }

    void NotificarTurno(){
        CambiodeTurno?.Invoke(ObtenerBomberoenTurno());
    }

}
