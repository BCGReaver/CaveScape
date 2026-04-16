using UnityEngine;

public class CrystalsManagerSolo : MonoBehaviour
{
    public static CrystalsManagerSolo Instance;
    public int totalCrystals = 0;

    void Awake()
    {
        Instance = this;
    }

    public static void AddCrystal(int amount)
    {
        if (Instance != null)
        {
            Instance.totalCrystals += amount;
            // Buscamos al jugador local para avisarle que actualice su UI
            var player = FindObjectOfType<PlayerControllerSolo>();
            if (player != null) player.actualizarCrystals();
        }
    }
}