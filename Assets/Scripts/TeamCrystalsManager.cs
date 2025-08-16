using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using TMPro;

[RequireComponent(typeof(PhotonView))]
public class TeamCrystalsManager : MonoBehaviourPunCallbacks
{
    public const string ROOM_KEY = "TC";
    [SerializeField] private int target = 30;

    [Header("HUD Tags (opcionales)")]
    [SerializeField] private string crystalCounterTag = "HUD_CrystalText";
    [SerializeField] private string winCanvasTag = "HUD_WinCanvas";

    private TMP_Text crystalText;
    private GameObject winCanvas;

    void Start()
    {
        HookHUD();
        InitRoomPropIfNeeded();
        RefreshUI();
        Debug.Log("[TCM] Start listo.");
    }

    private GameObject FindWithTagSafe(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return null;
        try { return GameObject.FindWithTag(tag); }
        catch { return null; } // la Tag no existe
    }

    private void HookHUD()
    {
        if (!crystalText)
        {
            var go = FindWithTagSafe(crystalCounterTag);
            if (!go) go = GameObject.Find("Contador_Crystals"); // fallback por nombre
            if (go) crystalText = go.GetComponent<TMP_Text>();
        }

        if (!winCanvas)
        {
            var go = FindWithTagSafe(winCanvasTag);
            if (!go) go = GameObject.Find("Won_Canvas"); // fallback por nombre
            if (go) winCanvas = go;
        }
    }

    private void InitRoomPropIfNeeded()
    {
        if (!PhotonNetwork.InRoom) return;
        if (PhotonNetwork.IsMasterClient &&
            !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ROOM_KEY))
        {
            var init = new Hashtable { { ROOM_KEY, 0 } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(init);
            Debug.Log("[TCM] Inicialicé ROOM_KEY en 0 (Master).");
        }
    }

    public static void AddCrystal(int amount = 1)
    {
        if (!PhotonNetwork.InRoom) return;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ROOM_KEY))
        {
            var mgr = FindObjectOfType<TeamCrystalsManager>();
            if (mgr) mgr.photonView.RPC(nameof(RPC_MasterAdd), RpcTarget.MasterClient, amount);
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            int current = (int)PhotonNetwork.CurrentRoom.CustomProperties[ROOM_KEY];
            var expected = new Hashtable { { ROOM_KEY, current } };
            var updated = new Hashtable { { ROOM_KEY, current + amount } };

            if (PhotonNetwork.CurrentRoom.SetCustomProperties(updated, expected))
                return; // OnRoomPropertiesUpdate disparará RefreshUI
        }

        var mgr2 = FindObjectOfType<TeamCrystalsManager>();
        if (mgr2) mgr2.photonView.RPC(nameof(RPC_MasterAdd), RpcTarget.MasterClient, amount);
    }

    [PunRPC]
    private void RPC_MasterAdd(int amount)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int current = 0;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ROOM_KEY, out object v))
            current = (int)v;

        var updated = new Hashtable { { ROOM_KEY, current + amount } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(updated);
        Debug.Log($"[TCM] MasterAdd -> {current} -> {current + amount}");
    }

    public override void OnRoomPropertiesUpdate(Hashtable changed)
    {
        if (changed.ContainsKey(ROOM_KEY))
        {
            Debug.Log("[TCM] OnRoomPropertiesUpdate recibido.");
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        HookHUD(); // por si el HUD aparece después

        int current = 0;
        if (PhotonNetwork.InRoom &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ROOM_KEY, out object v))
            current = (int)v;

        if (crystalText) crystalText.text = current + " x";
        if (winCanvas) winCanvas.SetActive(current >= target);

        Debug.Log($"[TCM] UI -> {current}");
    }
}
