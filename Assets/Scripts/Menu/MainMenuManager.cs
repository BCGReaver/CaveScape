/**
 * @file MainMenuManager.cs
 * @brief Maneja el botón Play del menú principal y la conexión inicial a Photon.
 *
 * @details
 * Flujo rapidito:
 * - En Start(): deshabilita el botón Play, configura Photon (sync de escena, nickname, versión)
 *   y se conecta con `ConnectUsingSettings()`.
 * - Cuando conecta al Master (`OnConnectedToMaster`): habilita el botón Play.
 * - Al presionar Play (`GoLobby`): carga la escena de Lobby de forma sincronizada.
 * - Si se desconecta (`OnDisconnected`): deshabilita el botón y avisa por consola.
 *
 * Tips:
 * - `AutomaticallySyncScene = true` hace que cuando el Master cambie de escena, todos lo sigan.
 * - El `NickName` aquí lo puse como el nombre del dispositivo; si quieres algo más pro, usa un InputField.
 * - Asegúrate de que la escena `lobbyScene` esté en Build Settings (File > Build Settings).
 */

using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

/**
 * @class MainMenuManager
 * @brief Control del menú principal: conexión a Photon y salto al Lobby.
 */
public class MainMenuManager : MonoBehaviourPunCallbacks
{
    /// @brief Botón de Play del menú (así evitas spamear antes de conectar).
    [SerializeField] private Button playButton;      // arrástralo desde el Inspector

    /// @brief Nombre de la escena del Lobby a cargar cuando presiones Play.
    [SerializeField] private string lobbyScene = "Lobby";

    /**
     * @brief Configura Photon y arranca la conexión; deshabilita Play hasta estar online.
     */
    void Start()
    {
        playButton.interactable = false;

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = SystemInfo.deviceName;  // o pide un InputField si quieres
        PhotonNetwork.GameVersion = "1.0.0";
        PhotonNetwork.ConnectUsingSettings();
    }

    /**
     * @brief Callback cuando te conectas al Master de Photon.
     * @details Habilita el botón Play para poder ir al Lobby.
     */
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        playButton.interactable = true;
    }

    /**
     * @brief Handler del botón Play: carga la escena de Lobby de forma sincronizada.
     * @note Requiere `PhotonNetwork.AutomaticallySyncScene = true`.
     */
    public void GoLobby()
    {
        // Carga sincronizada hacia el Lobby
        PhotonNetwork.LoadLevel(lobbyScene);
    }

    /**
     * @brief Callback cuando te desconectas de Photon.
     * @param cause Motivo de la desconexión (por si quieres mostrarlo en UI).
     */
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"Disconnected: {cause}");
        playButton.interactable = false;
    }
}
