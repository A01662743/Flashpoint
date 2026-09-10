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

    void ActualizarTexto(Bombero bomberoActivo)
    {
        if (bomberoActivo == null) return;

        Debug.Log("Evento recibido: " + bomberoActivo.nombreJugador);

        // Validar componente de texto
        if (textoTurno != null)
        {
            textoTurno.text = "Turno: " + bomberoActivo.nombreJugador;
            textoTurno.color = bomberoActivo.colorjugador;
        }

        if (playerManager == null || playerManager.bomberos == null) return;

        foreach (var bombero in playerManager.bomberos)
        {
            if (bombero == null) continue;

            bool esElActivo = (bombero == bomberoActivo);
            bombero.esMiTurno = esElActivo;

            // Validar cámara antes de activar/desactivar
            if (bombero.miCamara != null)
            {
                bombero.miCamara.gameObject.SetActive(esElActivo);
            }

            if (esElActivo)
            {
                int apGuardado = Mathf.Min(bombero.apDisponibles, ReglasJuego.AP_MAX);
                bombero.apDisponibles = ReglasJuego.AP + apGuardado;
            }
        }
    }
}
