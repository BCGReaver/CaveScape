using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class MainMenuManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button playButton;      // arrástralo desde el Inspector
    [SerializeField] private string lobbyScene = "Lobby";

    void Start()
    {
        playButton.interactable = false;

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = SystemInfo.deviceName;  // o pide un InputField si quieres
        PhotonNetwork.GameVersion = "1.0.0";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        playButton.interactable = true;
    }

    public void GoLobby()
    {
        // Carga sincronizada hacia el Lobby
        PhotonNetwork.LoadLevel(lobbyScene);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"Disconnected: {cause}");
        playButton.interactable = false;
    }
}
