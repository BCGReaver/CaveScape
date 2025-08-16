using UnityEngine;
using System.Linq;
using Photon.Pun;

public class NetworkSpawner : MonoBehaviour
{
    [SerializeField] string playerPrefabPath = "Player"; // ruta dentro de Resources
    Transform[] spawns;

    void Awake()
    {
        // Recolecta y ordena spawns por nombre para tener el mismo orden en todos los clientes
        spawns = GameObject.FindGameObjectsWithTag("Spawn")
                 .OrderBy(go => go.name)
                 .Select(go => go.transform)
                 .ToArray();

        if (spawns.Length == 0)
            Debug.LogError("No encontré spawns con Tag 'Spawn' en la escena.");
    }

    void Start()
    {
        Debug.Log($"[Spawner] Estado: InRoom={PhotonNetwork.InRoom}, Scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("No estás en una Room. Llega a GameScene usando PhotonNetwork.LoadLevel luego de unirte/crear sala.");
            return;
        }

        // evita doble spawn si recargas o vuelves a la escena
        if (PhotonNetwork.LocalPlayer.TagObject != null)
        {
            Debug.Log("[Spawner] Ya tenía un player asociado, no instancio de nuevo.");
            return;
        }

        int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawns.Length;
        var spawn = spawns[index];

        var playerGO = PhotonNetwork.Instantiate(playerPrefabPath, spawn.position, spawn.rotation);
        Debug.Log($"[Spawner] Instancié {playerPrefabPath} para Actor #{PhotonNetwork.LocalPlayer.ActorNumber} en {spawn.name}.");

        // Guarda referencia para no respawnear si reentras
        PhotonNetwork.LocalPlayer.TagObject = playerGO;

        // (Opcional) Si usas cámara de seguimiento simple, asígnala aquí
        // var follow = Camera.main ? Camera.main.GetComponent<SimpleFollow>() : null;
        // if (follow && playerGO.GetComponent<PhotonView>().IsMine) follow.target = playerGO.transform;
    }
}
