using UnityEngine;

public class AbrirPuerta : MonoBehaviour
{
    
    public KeyCode keyToPress = KeyCode.E;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(keyToPress))
        {
            if(transform.rotation.eulerAngles.y < 90f)
            {
                // Lógica para abrir la puerta
                Debug.Log("Puerta abierta");
                transform.Rotate(0f, 90f, 0f); // Gira la puerta 90 grados en el eje Y
            }
            else
            {
                // Lógica para cerrar la puerta
                Debug.Log("Puerta cerrada");
                transform.Rotate(0f, -90f, 0f); // Gira la puerta -90 grados en el eje Y
            }
        }
    }
}