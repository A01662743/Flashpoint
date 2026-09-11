using UnityEngine;
using TMPro;

public class EndGamePanelManager : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerManager playerManager;

    [Header("Paneles")]
    public GameObject panelWin;
    public GameObject panelLose;

    [Header("Textos de resumen (opcional)")]
    public TextMeshProUGUI textoWin;
    public TextMeshProUGUI textoLose;

    void OnEnable()
    {
        if (playerManager != null)
            playerManager.JuegoTerminado += MostrarPanel;
    }

    void OnDisable()
    {
        if (playerManager != null)
            playerManager.JuegoTerminado -= MostrarPanel;
    }

    void MostrarPanel(bool esGanado, string motivo)
    {
        GameStats stats = GameStateManager.Instance.CurrentState.game;

        if (esGanado)
        {
            if (panelWin != null) panelWin.SetActive(true);
            if (textoWin != null)
                textoWin.text = $"¡Victoria!\n{motivo}\nVíctimas rescatadas: {stats.rescued}";
        }
        else
        {
            if (panelLose != null) panelLose.SetActive(true);
            if (textoLose != null)
                textoLose.text = $"Derrota\n{motivo}\nVíctimas perdidas: {stats.lost} | Daño: {stats.damage}/24";
        }

        Time.timeScale = 0f;
    }
}