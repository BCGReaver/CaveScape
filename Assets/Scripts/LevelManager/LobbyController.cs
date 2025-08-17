using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class LobbyController : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button quickPlayButton;
    [SerializeField] private Button startGameButton;   // opcional si usas “Start”
    [SerializeField] private TextMeshProUGUI statusText;

    private const int MaxPlayers = 2;                  // ajusta a tu necesidad
    private const string GameVer = "1.0.0";            // usa la MISMA versión en el menú

    void Start()
    {
        quickPlayButton.interactable = false;
        if (startGameButton) startGameButton.gameObject.SetActive(false);

        PhotonNetwork.AutomaticallySyncScene = true;

        // 1) ¿Ya estoy conectado?
        if (!PhotonNetwork.IsConnected)
        {
            statusText.text = "Conectando a Photon...";
            PhotonNetwork.GameVersion = GameVer;
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            // 2) Ya conectado: asegurar que estemos en el lobby
            if (!PhotonNetwork.InLobby)
            {
                statusText.text = "Entrando al lobby...";
                PhotonNetwork.JoinLobby();
            }
            else
            {
                statusText.text = "En lobby. Listo para jugar.";
                quickPlayButton.interactable = true;
            }
        }
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "Conectado al Master. Entrando al lobby...";
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        statusText.text = "En lobby. Listo para Quick Play.";
        quickPlayButton.interactable = true;
    }

    public override void OnLeftLobby()
    {
        quickPlayButton.interactable = false;
        statusText.text = "Saliendo del lobby...";
    }

    public void QuickPlay()
    {
        // 3) Solo buscamos sala si YA estamos listos
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InLobby)
        {
            statusText.text = $"Esperando conexión... ({PhotonNetwork.NetworkClientState})";
            return;
        }

        statusText.text = "Buscando sala...";
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        statusText.text = "No hay salas. Creando una nueva...";
        RoomOptions opts = new RoomOptions { MaxPlayers = MaxPlayers };
        PhotonNetwork.CreateRoom(null, opts);
    }

    public override void OnCreatedRoom()
    {
        statusText.text = $"Sala creada: {PhotonNetwork.CurrentRoom.Name}";
        UpdateStartButton();
    }

    public override void OnJoinedRoom()
    {
        statusText.text = $"Unido a: {PhotonNetwork.CurrentRoom.Name} " +
                          $"({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})";
        UpdateStartButton();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        statusText.text = $"Entró {newPlayer.NickName} " +
                          $"({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})";
        UpdateStartButton();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        statusText.text = $"Salió {otherPlayer.NickName} " +
                          $"({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})";
        UpdateStartButton();
    }

    private void UpdateStartButton()
    {
        if (!startGameButton) return;

        bool canStart = PhotonNetwork.IsMasterClient &&
                        PhotonNetwork.CurrentRoom != null &&
                        PhotonNetwork.CurrentRoom.PlayerCount >= MaxPlayers;

        startGameButton.gameObject.SetActive(canStart);
        startGameButton.interactable = canStart;
    }

    public void StartMatch() // botón Start (solo Master)
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("GameScene");
    }
}
