using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDBinderSolo : MonoBehaviour
{
    [Header("UI References")]
    public Image[] hearts;
    public TMP_Text crystalText;
    public GameObject loseCanvas;
    public GameObject winCanvas;

    void Start()
    {
        // Esperamos un momento a que el jugador aparezca
        Invoke(nameof(BindToPlayer), 0.1f);
    }

    void BindToPlayer()
    {
        PlayerControllerSolo player = FindObjectOfType<PlayerControllerSolo>();
        if (player != null)
        {
            player.BindHUD(hearts, crystalText, loseCanvas, winCanvas);
            Debug.Log("HUD vinculado exitosamente al jugador.");
        }
    }
}