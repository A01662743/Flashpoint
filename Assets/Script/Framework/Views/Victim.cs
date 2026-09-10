using UnityEngine;

public class Victim : MonoBehaviour
{
    public Vector3 axisRotation = Vector3.up; // (0, 1, 0) = Eje Y por defecto
    public float rotationSpeed = 30f;          // Grados por segundo
    public float amplitude = 0.35f; // Distancia del movimiento (arriba/abajo)
    public float frequency = 1.0f;   // Velocidad de la oscilación
    private Vector3 startPos;
    public int num = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Asegurarse de que la velocidad de rotación sea positiva
        transform.Rotate(Vector3.forward * 20.0f);
        startPos = transform.position;
    }

    void Update()
    {
        // Rota continuamente en el eje elegido de forma suave e independiente de los FPS
        transform.Rotate(axisRotation * rotationSpeed * Time.deltaTime, Space.World);

        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

    }
}
