using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun; // Solo lo usamos para el Multi

public class MainMenuManager : MonoBehaviourPunCallbacks
{
    [Header("UI Buttons")]
    [SerializeField] private Button multiplayerButton;
    [SerializeField] private Button soloButton;

    [Header("Scenes")]
    [SerializeField] private string gameScene = "Solo"; // <--- ESCRIBE EL NOMBRE EXACTO AQUÍ

    void Start()
    {
        // El botón Solo siempre funciona porque no usa red
        if (soloButton != null) soloButton.interactable = true;

        // Conectamos a Photon SOLO para habilitar el multi más tarde
        if (!PhotonNetwork.IsConnected) PhotonNetwork.ConnectUsingSettings();
    }

    // --- ESTO ES LO QUE TE INTERESA ---
    public void GoSolo()
    {
        Debug.Log("Intentando cargar escena Solo: " + gameScene);

        // Carga nativa de Unity. Cero Photon.
        // Esto usará tus scripts PlayerControllerSolo, etc.
        SceneManager.LoadScene(gameScene);
    }

    public override void OnConnectedToMaster()
    {
        // Solo cuando Photon conecta, activamos el botón de Multi
        if (multiplayerButton != null) multiplayerButton.interactable = true;
    }

    public void GoLobby()
    {
        // El multi sí sigue su flujo de Photon
        SceneManager.LoadScene("Lobby");
    }
}