using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    // Colecciones para rastrear instancias existentes
    private Dictionary<int, GameObject> spawnedPOIs = new Dictionary<int, GameObject>();
    private List<GameObject> spawnedAPs = new List<GameObject>();

    private void Start()
    {
        if (poiContainer == null) poiContainer = transform;
        if (apContainer == null) apContainer = transform;

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
        // Crear un HashSet con los IDs de POI presentes en la actualización actual
        HashSet<int> currentPOIIds = new HashSet<int>();
        if (state.poi != null)
        {
            foreach (POI poiData in state.poi)
            {
                currentPOIIds.Add(poiData.id);
            }
        }

        // PASO 1: Eliminar instancias de POIs que ya NO existen en el GameState
        List<int> idsToRemove = new List<int>();
        foreach (var pair in spawnedPOIs)
        {
            if (!currentPOIIds.Contains(pair.Key))
            {
                if (pair.Value != null) Destroy(pair.Value);
                idsToRemove.Add(pair.Key);
            }
        }

        foreach (int id in idsToRemove)
        {
            spawnedPOIs.Remove(id);
        }

        // PASO 2: Procesar los POIs activos en GameState
        if (state.poi == null) return;

        foreach (POI poiData in state.poi)
        {
            GameObject prefabToSpawn = null;

            // Determinar qué prefab usar según status y result
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

            // Si el POI ya tenía una instancia, la destruimos para actualizarla (por si cambió de status/result)
            if (spawnedPOIs.ContainsKey(poiData.id))
            {
                if (spawnedPOIs[poiData.id] != null)
                {
                    Destroy(spawnedPOIs[poiData.id]);
                }
                spawnedPOIs.Remove(poiData.id);
            }

            // Instanciar y posicionar el nuevo prefab correspondiente a este ID
            if (prefabToSpawn != null)
            {
                GameObject newPOIInstance = Instantiate(prefabToSpawn, poiContainer);
                spawnedPOIs[poiData.id] = newPOIInstance;

                RectTransform rectTransform = newPOIInstance.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // Anchor Preset: Top Right (1, 1)
                    rectTransform.anchorMin = new Vector2(1, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(1, 1);

                    // Posición base para id 1: x = -370, y = -10. +170 en X por cada id sucesivo
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

        Agent currentAgent = state.agents.Find(a => a.id == state.current_agent);
        int targetAP = (currentAgent != null) ? currentAgent.ap : 0;

        // PASO 1: Si hay más elementos instanciados de los necesarios, destruimos el excedente desde el final
        while (spawnedAPs.Count > targetAP)
        {
            int lastIndex = spawnedAPs.Count - 1;
            if (spawnedAPs[lastIndex] != null)
            {
                Destroy(spawnedAPs[lastIndex]);
            }
            spawnedAPs.RemoveAt(lastIndex);
        }

        // PASO 2: Si faltan elementos para llegar a la cantidad necesaria, agregamos únicamente los faltantes
        while (spawnedAPs.Count < targetAP)
        {
            int i = spawnedAPs.Count; // El índice del nuevo icono a instanciar

            GameObject spawnedAP = Instantiate(prefabAP, apContainer);
            spawnedAPs.Add(spawnedAP);

            RectTransform rectTransform = spawnedAP.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Anchor Preset: Top Left (0, 1)
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0, 1);

                // Posición inicial i=0: x = 0, y = -10. +80 en X por cada AP sucesivo
                float posX = 0f + (i * 80f);
                float posY = -10f;

                rectTransform.anchoredPosition = new Vector2(posX, posY);
            }
        }
    }
}