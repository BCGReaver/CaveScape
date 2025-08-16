using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class HUDBinder : MonoBehaviour
{
    [Header("Arrastra desde la escena")]
    public Image[] hearts;                  // Heart, Heart (1), Heart (2)
    public TMP_Text crystalCounter;         // Contador_Crystals (opcional)
    public GameObject loseCanvas;           // Final_Canvas
    public GameObject winCanvas;            // Won_Canvas

    private IEnumerator Start()
    {
        // Espera a que Photon instancie al jugador local
        while (true)
        {
            var players = FindObjectsOfType<PlayerController>();
            var local = players.FirstOrDefault(p => p && p.photonView && p.photonView.IsMine);
            if (local != null)
            {
                // pasa TODAS las refs al player local
                local.BindHUD(hearts, crystalCounter, loseCanvas, winCanvas);
                Debug.Log($"[HUDBinder] Bound -> {local.name} hearts={hearts?.Length ?? 0}");
                yield break;
            }
            yield return null;
        }
    }
}
