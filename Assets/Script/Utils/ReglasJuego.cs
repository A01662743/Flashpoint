using UnityEngine;

public class ReglasJuego{

    // AP por turno
    public const int AP = 4;
    public const int AP_MAX = 4;

    //Costos de Movimiento

    public const int COSTO_MOVER_SIN_FUEGO = 1;
    public const int COSTO_MOVER_CON_FUEGO = 2;
    public const int COSTO_CARGAR_VICTIMA =2;

    // Costos acciones sobre entorno
    public const int CostoabrirPuerta = 1;
    public const int CostocerrarPuerta = 1;
    public const int CostoQuitarHumo = 1;
    public const int CostoFuegoAHumo = 1;
    public const int CostofuegoaVacia = 2;
    public const int CostodaniarPared = 2;

    // Costo Revelacion POI
    public const int CostorevelarPoi = 0;

    //Condiciones fin del juego
    public const int VictimasparaGanar = 7;
    public const int Victimasperdidas_Perder = 4;
    public const int LimiteDanio_Colapso = 24;
    public const int POIsRequeridos = 3;


}