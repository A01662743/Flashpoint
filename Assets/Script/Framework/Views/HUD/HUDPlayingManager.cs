using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Asegúrate de incluirlo si usas TextMeshPro

public class HUDPlayingManager : MonoBehaviour
{
    [Header("Prefabs UI")]
    [Tooltip("Prefab de la Imagen POI para status 'unknown' o result 'false_alarm'")]
    [SerializeField] private GameObject prefabImagePOI;

    [Tooltip("Prefab de la Imagen Víctima para result 'victim'")]
    [SerializeField] private GameObject prefabVictim;

    [Tooltip("Prefab del indicador de Puntos de Acción (AP)")]
    [SerializeField] private GameObject prefabAP;

    [Header("Contenedores para Instancias")]
    [Tooltip("Transform/Panel donde se spawnerán los POIs/Víctimas")]
    [SerializeField] private Transform poiContainer;

    [Tooltip("Transform/Panel donde se spawnerán las imágenes de AP")]
    [SerializeField] private Transform apContainer;

    [Header("Referencias de Textos")]
    [SerializeField] private TextMeshProUGUI TEXTTurno;
    [SerializeField] private TextMeshProUGUI TEXTVictimas;
    [SerializeField] private TextMeshProUGUI TEXTDamage;

    // Listas internas para controlar los objetos instanciados
    private List<GameObject> spawnedPOIs = new List<GameObject>();
    private List<GameObject> spawnedAPs = new List<GameObject>();

    private void Start()
    {
        // Si no se asignaron contenedores específicos, usa este mismo Transform (el Panel HUD)
        if (poiContainer == null) poiContainer = transform;
        if (apContainer == null) apContainer = transform;

        // Suscribirse al evento inicial de GameStateManager si está disponible
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SubscribeToInitialState(RefreshHUD);
        }
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.UnsubscribeFromInitialState(RefreshHUD);
        }
    }

    /// <summary>
    /// Función principal encargada de sincronizar la UI con los datos de GameState.
    /// </summary>
    public void RefreshHUD()
    {
        if (GameStateManager.Instance == null || GameStateManager.Instance.CurrentState == null)
        {
            Debug.LogWarning("[HUDPlayingManager] No hay un GameState cargado para refrescar el HUD.");
            return;
        }

        GameState state = GameStateManager.Instance.CurrentState;

        // Limpiar elementos instanciados en ejecuciones anteriores
        ClearSpawnedElements();

        // 1. PIPELINE DE POIs
        ProcessPOIs(state);

        // 2. PIPELINE DE AGENTES & AP
        ProcessAgentAP(state);

        // 3. TEXTO TURNO
        if (TEXTTurno != null)
        {
            TEXTTurno.text = $"TURNO {state.turn}";
        }

        // 4. TEXTO VÍCTIMAS
        if (TEXTVictimas != null && state.game != null)
        {
            TEXTVictimas.text = $"{state.game.rescued}/5 Victimas Rescatadas";
        }

        // 5. TEXTO DAÑO
        if (TEXTDamage != null && state.game != null)
        {
            TEXTDamage.text = state.game.damage.ToString();
        }
    }

    private void ProcessPOIs(GameState state)
    {
        if (state.poi == null) return;

        foreach (POI poiData in state.poi)
        {
            GameObject prefabToSpawn = null;

            // Lógica de selección del Prefab según status y result
            if (poiData.status == "unknown")
            {
                prefabToSpawn = prefabImagePOI;
            }
            else if (poiData.status == "known")
            {
                if (poiData.result == "victim")
                {
                    prefabToSpawn = prefabVictim;
                }
                else if (poiData.result == "false_alarm")
                {
                    prefabToSpawn = prefabImagePOI;
                }
            }

            // Instanciar y posicionar si corresponde
            if (prefabToSpawn != null)
            {
                GameObject spawnedPOI = Instantiate(prefabToSpawn, poiContainer);
                spawnedPOIs.Add(spawnedPOI);

                RectTransform rectTransform = spawnedPOI.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // Configurar Anchor Preset a Top Right (1, 1)
                    rectTransform.anchorMin = new Vector2(1, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(1, 1);

                    // Posición base para id 1: x = -370, y = -10
                    // Cada id sucesivo suma +170 a X: -370 + (id - 1) * 170
                    float posX = -370f + ((poiData.id - 1) * 170f);
                    float posY = -10f;

                    rectTransform.anchoredPosition = new Vector2(posX, posY);
                }
            }
        }
    }

    private void ProcessAgentAP(GameState state)
    {
        if (state.agents == null || prefabAP == null) return;

        // Buscar el agente con id == current_agent
        Agent currentAgent = state.agents.Find(a => a.id == state.current_agent);

        if (currentAgent != null)
        {
            int apAmount = currentAgent.ap;

            for (int i = 0; i < apAmount; i++)
            {
                GameObject spawnedAP = Instantiate(prefabAP, apContainer);
                spawnedAPs.Add(spawnedAP);

                RectTransform rectTransform = spawnedAP.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // Configurar Anchor Preset a Top Left (0, 1)
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    rectTransform.pivot = new Vector2(0, 1);

                    // Posición inicial i=0: x = 0, y = -10
                    // Cada AP sucesivo suma +80 a X: 0 + (i * 80)
                    float posX = 0f + (i * 80f);
                    float posY = -10f;

                    rectTransform.anchoredPosition = new Vector2(posX, posY);
                }
            }
        }
    }

    private void ClearSpawnedElements()
    {
        foreach (GameObject obj in spawnedPOIs)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedPOIs.Clear();

        foreach (GameObject obj in spawnedAPs)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedAPs.Clear();
    }
}