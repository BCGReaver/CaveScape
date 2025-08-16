using Photon.Pun;
using UnityEngine;
using System.Linq;

public class NetworkSpawner : MonoBehaviourPunCallbacks
{
    [Header("Prefab en Resources/")]
    [SerializeField] private string playerPrefabPath = "Player"; // Resources/Player.prefab

    [Header("Tag de los puntos de spawn")]
    [SerializeField] private string spawnTag = "Spawn";

    private Transform[] spawns;

    void Awake()
    {
        CacheSpawns();
    }

    void Start()
    {
        // Por si esta escena se carga ya estando en una Room:
        TrySpawnIfNeeded("[Start]");
    }

    public override void OnJoinedRoom()
    {
        // Por si entras a la Room y aún no has spawneado:
        TrySpawnIfNeeded("[OnJoinedRoom]");
    }

    private void CacheSpawns()
    {
        spawns = GameObject.FindGameObjectsWithTag(spawnTag)
                 .OrderBy(go => go.name)
                 .Select(go => go.transform)
                 .ToArray();

        if (spawns.Length == 0)
            Debug.LogError($"[Spawner] No encontré spawns con Tag '{spawnTag}' en la escena.");
    }

    private void TrySpawnIfNeeded(string caller)
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning($"[Spawner]{caller} No estás en una Room, no spawneo.");
            return;
        }

        // Evita doble spawn (p.ej. recarga de escena)
        if (PhotonNetwork.LocalPlayer.TagObject != null)
        {
            BindCameraToLocal(PhotonNetwork.LocalPlayer.TagObject as GameObject);
            return;
        }

        if (spawns == null || spawns.Length == 0)
            CacheSpawns();

        int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawns.Length;
        Transform spawn = spawns[index];

        GameObject playerGO = PhotonNetwork.Instantiate(playerPrefabPath, spawn.position, spawn.rotation);
        PhotonNetwork.LocalPlayer.TagObject = playerGO;

        BindCameraToLocal(playerGO);

        Debug.Log($"[Spawner]{caller} Instancié '{playerPrefabPath}' para Actor #{PhotonNetwork.LocalPlayer.ActorNumber} en {spawn.name}.");
    }

    private void BindCameraToLocal(GameObject playerGO)
    {
        if (!playerGO) return;

        var pv = playerGO.GetComponent<PhotonView>();
        if (pv && pv.IsMine)
        {
            var cam = Camera.main;
            if (cam)
            {
                var follow = cam.GetComponent<CameraController>();
                if (follow) follow.objetive = playerGO.transform;

                var al = cam.GetComponent<AudioListener>();
                if (al) al.enabled = true;
            }
        }
    }
}
