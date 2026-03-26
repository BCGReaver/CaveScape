using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviourPunCallbacks
{
    [Header("UI Buttons")]
    [SerializeField] private Button multiplayerButton; // Botón para ir al Lobby
    [SerializeField] private Button soloButton;        // Botón para jugar solo

    [Header("Scenes")]
    [SerializeField] private string lobbyScene = "Lobby";
    [SerializeField] private string gameScene = "GameScene"; // ¡Asegúrate de poner aquí el nombre exacto de tu escena de nivel!

    void Start()
    {
        // Por ahora, desactivamos el multi hasta estar conectados al servidor real
        if (multiplayerButton != null) multiplayerButton.interactable = false;
        if (soloButton != null) soloButton.interactable = true; // El modo solo siempre está disponible

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = SystemInfo.deviceName;
        PhotonNetwork.GameVersion = "1.0.0";

        // Intentamos conectar para el modo multiplayer al abrir el juego
        PhotonNetwork.ConnectUsingSettings();
    }

    // --- ¡AQUÍ ESTÁ LA CORRECCIÓN! Solo un OnConnectedToMaster ---
    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectado al Master de Photon. ¿Modo Offline?: " + PhotonNetwork.OfflineMode);

        if (PhotonNetwork.OfflineMode)
        {
            // Si el jugador presionó "Solo", Photon se "conecta" en modo offline.
            // Creamos una sala "fantasma" para que el NetworkSpawner pueda funcionar.
            PhotonNetwork.CreateRoom("SoloRoom", new RoomOptions { MaxPlayers = 1 });
        }
        else
        {
            // Si se conectó normal a internet, habilitamos el botón de Multiplayer
            if (multiplayerButton != null) multiplayerButton.interactable = true;
        }
    }

    // --- MODO MULTIJUGADOR ---
    public void GoLobby()
    {
        PhotonNetwork.OfflineMode = false; // Aseguramos que el modo offline esté apagado
        PhotonNetwork.LoadLevel(lobbyScene);
    }

    // --- MODO SINGLE PLAYER ---
    public void GoSolo()
    {
        Debug.Log("Iniciando modo Solo...");

        // Al activar esto, Photon corta internet y simula una conexión local.
        // Esto automáticamente disparará OnConnectedToMaster() de nuevo, pero ahora en modo Offline.
        PhotonNetwork.OfflineMode = true;
    }

    public override void OnJoinedRoom()
    {
        // Si estamos en modo solo, vamos directo al juego, saltándonos el Lobby
        if (PhotonNetwork.OfflineMode)
        {
            SceneManager.LoadScene(gameScene);
        }
    }
}