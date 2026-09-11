using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public List<Bombero> bomberos = new List<Bombero>();
    public int turnoActual = 0;
    public WebClient webClient;

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
            IniciarTurnoDeAgente();
        }
        
    }

    public Bombero ObtenerBomberoenTurno(){
        return bomberos[turnoActual];
    }

    public void SiguienteTurno(){

        if (VerificarFinDeJuego())return;
        GameStateManager.Instance.TriggerSmokeProcess();
        GameStateManager.Instance.TriggerPOIProcess();

        turnoActual = (turnoActual + 1) % bomberos.Count;
        NotificarTurno();
        IniciarTurnoDeAgente();
    }

    void IniciarTurnoDeAgente(){
        Bombero bomberoActual = ObtenerBomberoenTurno();
        Agent agenteData = GameStateManager.Instance.GetAgentById(bomberoActual.agentId);
        if(agenteData != null)
        {
            int totalPropuesto = agenteData.ap + ReglasJuego.AP;
            agenteData.ap = Mathf.Min(totalPropuesto, ReglasJuego.AP_MAX);
            bomberoActual.apDisponibles = agenteData.ap;
        }
        GameStateManager.Instance.SetCurrentAgent(bomberoActual.agentId);
        webClient.EnviarTurno(GameStateManager.Instance.CurrentState);
    }

    bool VerificarFinDeJuego(){
        GameStats stats = GameStateManager.Instance.CurrentState.game;
        if(stats.rescued >= 7)
        {
            Debug.Log("¡Has ganado! Has rescatado a suficientes personas.");
            return true;
        }
        if(stats.lost >= 4)
        {
            Debug.Log("¡Has perdido! Has tenido demasiadas bajas.");
            return true;
        }
        if(stats.damage >= 24)
        {
            Debug.Log("¡Has perdido! El edificio colaps[o por daño estructural].");
            return true;
        }
        return false;
    }



    void NotificarTurno(){
        CambiodeTurno?.Invoke(ObtenerBomberoenTurno());
    }

}
