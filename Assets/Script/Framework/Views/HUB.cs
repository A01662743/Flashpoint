using UnityEngine;
using TMPro;

public class HubManager : MonoBehaviour
{
    [Header("Referencias de Sistemas")]
    public PlayerManager playerManager;
    public GameStateManager gameStateManager;

    [Header("Textos del Hub")]
    public TextMeshProUGUI textoDanioEstructura;
    public TextMeshProUGUI textoVictimasPorSalvar;
    public TextMeshProUGUI textoTurnoActual;
    public TextMeshProUGUI textoPOIsRestantes;

    private const int DANIO_MAXIMO = 24;
    private const int VICTIMAS_PARA_GANAR = 7;

    void OnEnable()
    {
        if (playerManager != null)
            playerManager.CambiodeTurno += ActualizarHub;
    }

    void OnDisable()
    {
        if (playerManager != null)
            playerManager.CambiodeTurno -= ActualizarHub;
    }

    void ActualizarHub(Bombero bomberoActivo)
    {
        GameState state = gameStateManager.CurrentState;
        if (state == null) return;

        // 1. Daño de estructura
        textoDanioEstructura.text = $"Daño: {state.game.damage} / {DANIO_MAXIMO}";

        // 2. Víctimas por salvar
        int faltantes = VICTIMAS_PARA_GANAR - state.game.rescued;
        textoVictimasPorSalvar.text = $"Víctimas por salvar: {faltantes}";

        // 3. Turno actual
        if (bomberoActivo != null)
        {
            textoTurnoActual.text = $"Turno: {bomberoActivo.nombreJugador}";
        }

        // 4. POIs restantes
        int poisRestantes = state.poi != null ? state.poi.Count : 0;
        textoPOIsRestantes.text = $"POIs restantes: {poisRestantes}";
    }
}