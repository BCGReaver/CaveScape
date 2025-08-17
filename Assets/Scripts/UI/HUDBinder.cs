using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDBinder : MonoBehaviour
{
    [Header("Arrastra desde la escena")]
    public Image[] hearts;              // Heart, Heart (1), Heart (2)
    public TMP_Text crystalCounter;     // Contador_Crystals (opcional)
    public GameObject loseCanvas;       // Canvas de derrota (desactivado por defecto)
    public GameObject winCanvas;        // Canvas de victoria (desactivado por defecto)

    private IEnumerator Start()
    {
        // Espera a que el Player local exista
        while (true)
        {
            var players = FindObjectsOfType<PlayerController>();
            var local = players.FirstOrDefault(p => p && p.photonView && p.photonView.IsMine);
            if (local != null)
            {
                // Llama a cualquiera de los 2 overloads; ambos existen en PlayerController
                if (loseCanvas || winCanvas)
                    local.BindHUD(hearts, crystalCounter, loseCanvas, winCanvas);
                else
                    local.BindHUD(hearts, crystalCounter);

                Debug.Log($"[HUDBinder] Bound -> {local.name} hearts={(hearts != null ? hearts.Length : 0)}");
                yield break;
            }
            yield return null;
        }
    }
}
