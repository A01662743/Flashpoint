using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public List<Bombero> bomberos = new List<Bombero>();
    public int turnoActual = 0;
    public TableroManager tableroManager;
    public WebClient webClient;

    public event Action<Bombero> CambiodeTurno;
    private bool primerTurnoNotificado = false;

    void Start()
    {
        NotificarTurno();
    }

    void Update()
    {
        if (!primerTurnoNotificado)
        {
            NotificarTurno();
            primerTurnoNotificado = true;
            IniciarTurnoDeAgente();
        }
    }

    public Bombero ObtenerBomberoenTurno()
    {
        return bomberos[turnoActual];
    }

    public void SiguienteTurno()
    {
        if (VerificarFinDeJuego()) return;

        GameStateManager.Instance.TriggerSmokeProcess();

        turnoActual = (turnoActual + 1) % bomberos.Count;

        NotificarTurno();
        IniciarTurnoDeAgente();
    }

    void IniciarTurnoDeAgente()
    {
        Bombero bomberoActual = ObtenerBomberoenTurno();

        Agent agenteData = GameStateManager.Instance.GetAgentById(bomberoActual.agentId);
        if (agenteData != null)
        {
            int totalPropuesto = agenteData.ap + ReglasJuego.AP;
            agenteData.ap = Mathf.Min(totalPropuesto, ReglasJuego.AP_MAX);
            bomberoActual.apDisponibles = agenteData.ap;
        }

        GameStateManager.Instance.SetCurrentAgent(bomberoActual.agentId);
        webClient.EnviarTurno(GameStateManager.Instance.CurrentState);
    }

    bool VerificarFinDeJuego()
    {
        GameStats stats = GameStateManager.Instance.CurrentState.game;

        if (stats.rescued >= 7)
        {
            Debug.Log("¡VICTORIA! Se rescataron suficientes víctimas.");
            return true;
        }

        if (stats.lost >= 4)
        {
            Debug.Log("DERROTA. Se perdieron demasiadas víctimas.");
            return true;
        }

        if (stats.damage >= 24)
        {
            Debug.Log("DERROTA. El edificio colapsó por daño estructural.");
            return true;
        }

        return false;
    }

    void NotificarTurno()
    {
        CambiodeTurno?.Invoke(ObtenerBomberoenTurno());
    }
}