using UnityEngine;
using TMPro;

public class UITurnoManager : MonoBehaviour
{
    public TextMeshProUGUI textoTurno;
    public PlayerManager playerManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable(){
        playerManager.CambiodeTurno += ActualizarTexto;
    }

    void OnDisable(){
        playerManager.CambiodeTurno -= ActualizarTexto;
    }

    void ActualizarTexto(Bombero bomberoActivo){
        Debug.Log("Evento recibido: " + bomberoActivo.nombreJugador);
        textoTurno.text = "Turno:"+ bomberoActivo.nombreJugador;
        textoTurno.color = bomberoActivo.colorjugador;

        foreach (var bombero in playerManager.bomberos){
            bombero.esMiTurno = (bombero == bomberoActivo);
            bombero.miCamara.gameObject.SetActive(bombero == bomberoActivo);
            if (bombero == bomberoActivo){
                int apGuardado = Mathf.Min(bombero.apDisponibles, ReglasJuego.AP_MAX);
                bombero.apDisponibles = ReglasJuego.AP + apGuardado;
            }
        }
    }
}
