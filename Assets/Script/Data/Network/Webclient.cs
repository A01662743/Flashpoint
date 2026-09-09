using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class WebClient : MonoBehaviour
{
    private string url = "http://localhost:8585";

    public void EnviarTurno(GameState estado)
    {
        StartCoroutine(SendData(estado));
    }

    IEnumerator SendData(GameState estado)
    {
        string json = JsonConvert.SerializeObject(estado);

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error en la petición: " + www.error);
            }
            else
            {
                string respuestaTexto = www.downloadHandler.text;
                Debug.Log("Respuesta de Python: " + respuestaTexto);

                PythonToUnityData respuesta = JsonConvert.DeserializeObject<PythonToUnityData>(respuestaTexto);
                AplicarAcciones(respuesta);
            }
        }
    }

    void AplicarAcciones(PythonToUnityData respuesta)
    {
        Bombero bombero = EncontrarBomberoPorId(respuesta.agent_id);
        if (bombero == null)
        {
            Debug.LogWarning($"No se encontró el GameObject del bombero con id {respuesta.agent_id}");
            return;
        }

        StartCoroutine(EjecutarAccionesEnOrden(bombero, respuesta.actions));
    }

    Bombero EncontrarBomberoPorId(int id)
    {
        Bombero[] todos = FindObjectsOfType<Bombero>();
        foreach (Bombero b in todos)
        {
            if (b.agentId == id) return b;
        }
        return null;
    }

    IEnumerator EjecutarAccionesEnOrden(Bombero bombero, List<GameAction> acciones)
    {
        foreach (GameAction accion in acciones)
        {
            // primero actualiza el estado de datos
            GameStateManager.Instance.AplicarAccion(bombero.agentId, accion);

            // luego anima el movimiento visual, esperando a que termine antes de la siguiente
            bool terminada = false;
            bombero.EjecutarAccion(accion, () => terminada = true);

            while (!terminada)
            {
                yield return null;
            }
        }

        Debug.Log($"[WebClient] Agente {bombero.agentId} terminó de ejecutar todas sus acciones.");
    }

    void Start()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != null)
        {
            EnviarTurno(GameStateManager.Instance.CurrentState);
        }
        else
        {
            Debug.LogWarning("[WebClient] GameStateManager no tiene un CurrentState cargado todavía.");
        }
    }

    void Update() { }
}