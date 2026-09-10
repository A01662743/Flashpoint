using UnityEngine;
using UnityEditor;

public class LimpiadorDeScripts
{
    [MenuItem("Tools/Limpiar Scripts Faltantes")]
    public static void Limpiar()
    {
        // Busca en la escena actual (incluyendo objetos desactivados)
        GameObject[] objetosEscena = Object.FindObjectsOfType<GameObject>(true);
        int eliminadosEscena = 0;

        foreach (GameObject go in objetosEscena)
        {
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (count > 0)
            {
                Debug.LogWarning($"¡Encontrado y limpiado! Se eliminaron {count} script(s) faltantes en el objeto de la escena: '{go.name}'", go);
                eliminadosEscena += count;
            }
        }

        Debug.Log($"<color=green>Proceso terminado. Total de scripts borrados: {eliminadosEscena}</color>");
    }
}