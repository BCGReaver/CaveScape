/**
 * @file LobbyController.cs
 * @brief Controla el menú/lobby para conectarte a Photon, entrar al lobby y crear/unirte a salas con Quick Play.
 *
 * @details
 * Flujo resumido:
 * 1) En Start() revisa si ya estás conectado a Photon. Si no, conecta con `ConnectUsingSettings()`.
 * 2) Cuando se conecta al Master, entra al Lobby.
 * 3) Con el botón QuickPlay busca sala al azar; si no hay, crea una nueva con `MaxPlayers`.
 * 4) Muestra estados en pantalla y habilita/deshabilita los botones según toque.
 * 5) Si usas botón "Start", solo el Master puede lanzar la escena de juego con `LoadLevel("GameScene")`.
 *
 * Tips rápidos:
 * - Usa la MISMA `GameVersion` en todas tus escenas del menú para que todos sean compatibles.
 * - `AutomaticallySyncScene = true` deja que el Master arrastre a todos a la misma escena.
 * - No llames `ConnectUsingSettings()` en tu GameScene (evitas doble conexión/conflictos). Hazlo en el menú/lobby.
 */

using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

/**
 * @class LobbyController
 * @brief Admin del lobby: conexión a Photon, entrada al Lobby, QuickPlay y arranque de partida.
 *
 * @remarks
 * Monta este script en un Canvas del menú o un GameObject vacío del menú principal.
 * Asegúrate de asignar los botones y el `statusText` en el inspector.
 */
public class LobbyController : MonoBehaviourPunCallbacks
{
    /// @brief Botón para entrar rápido a una sala (JoinRandom o crear si no hay).
    [SerializeField] private Button quickPlayButton;

    /// @brief Botón opcional “Start” (solo lo ve/usa el MasterClient cuando hay jugadores suficientes).
    [SerializeField] private Button startGameButton;   // opcional si usas “Start”

    /// @brief Texto para mostrar estados (conectando, en lobby, en sala, etc.).
    [SerializeField] private TextMeshProUGUI statusText;

    /// @brief Máximo de jugadores por sala (ajústalo a tu juego).
    private const int MaxPlayers = 2;

    /// @brief Versión del juego para matchear clientes (todos deben tener la misma).
    private const string GameVer = "1.0.0";

    /**
     * @brief Inicializa UI y prepara la conexión/entrada al lobby.
     *
     * @details
     * - Deshabilita QuickPlay al inicio para que no spamee antes de estar listos.
     * - Esconde el botón Start si lo usas.
     * - Si no estás conectado: conecta y define `GameVersion`.
     * - Si ya estabas conectado: asegúrate de estar en el lobby.
     */
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

    /**
     * @brief Callback de Photon al conectar con el Master Server.
     * @details Aquí automáticamente entramos al lobby.
     */
    public override void OnConnectedToMaster()
    {
        statusText.text = "Conectado al Master. Entrando al lobby...";
        PhotonNetwork.JoinLobby();
    }

    /**
     * @brief Callback cuando entras al Lobby.
     * @details Habilita QuickPlay para que ya puedas buscar/crear sala.
     */
    public override void OnJoinedLobby()
    {
        statusText.text = "En lobby. Listo para Quick Play.";
        quickPlayButton.interactable = true;
    }

    /**
     * @brief Callback cuando sales del Lobby.
     * @details Deshabilita QuickPlay y muestra estado.
     */
    public override void OnLeftLobby()
    {
        quickPlayButton.interactable = false;
        statusText.text = "Saliendo del lobby...";
    }

    /**
     * @brief Handler del botón QuickPlay: intenta unirse a una sala random o crea una nueva si no hay.
     *
     * @details
     * Solo corre si ya estás conectado y dentro del Lobby. Si no, muestra en qué estado de red vas.
     */
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

    /**
     * @brief Si no hay salas random disponibles, crea una nueva.
     * @param returnCode Código de Photon (no lo usamos aquí).
     * @param message Mensaje de Photon (informativo).
     */
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        statusText.text = "No hay salas. Creando una nueva...";
        RoomOptions opts = new RoomOptions { MaxPlayers = MaxPlayers };
        PhotonNetwork.CreateRoom(null, opts);
    }

    /**
     * @brief Callback cuando se crea la sala con éxito.
     * @details Actualiza el estado y revisa si se puede mostrar el botón Start.
     */
    public override void OnCreatedRoom()
    {
        statusText.text = $"Sala creada: {PhotonNetwork.CurrentRoom.Name}";
        UpdateStartButton();
    }

    /**
     * @brief Callback cuando te unes a una sala (tu propia o la de alguien más).
     * @details Muestra conteo de jugadores y actualiza botón Start.
     */
    public override void OnJoinedRoom()
    {
        statusText.text = $"Unido a: {PhotonNetwork.CurrentRoom.Name} " +
                          $"({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})";
        UpdateStartButton();
    }

    /**
     * @brief Callback cuando entra otro jugador a tu sala.
     * @param newPlayer El jugador que acaba de entrar.
     * @details Actualiza el estado y botón Start por si ya se juntó la banda.
     */
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        statusText.text = $"Entró {newPlayer.NickName} " +
                          $"({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})";
        UpdateStartButton();
    }

    /**
     * @brief Callback cuando un jugador sale de tu sala.
     * @param otherPlayer El jugador que se fue.
     * @details Actualiza el estado y botón Start por si ya no hay quorum.
     */
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        statusText.text = $"Salió {otherPlayer.NickName} " +
                          $"({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})";
        UpdateStartButton();
    }

    /**
     * @brief Muestra y habilita el botón Start si eres Master y hay jugadores suficientes.
     */
    private void UpdateStartButton()
    {
        if (!startGameButton) return;

        bool canStart = PhotonNetwork.IsMasterClient &&
                        PhotonNetwork.CurrentRoom != null &&
                        PhotonNetwork.CurrentRoom.PlayerCount >= MaxPlayers;

        startGameButton.gameObject.SetActive(canStart);
        startGameButton.interactable = canStart;
    }

    /**
     * @brief Handler del botón Start (solo Master): carga la escena de juego para todos.
     * @note Requiere `PhotonNetwork.AutomaticallySyncScene = true`.
     */
    public void StartMatch() // botón Start (solo Master)
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("GameScene");
    }
}
