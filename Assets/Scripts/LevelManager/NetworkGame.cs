/**
 * @file NetworkSpawner.cs
 * @brief Se encarga de spawnear al jugador local en una sala de Photon y amarrar la cámara a su player.
 *
 * @details
 * La idea es simple: cuando estás dentro de una Room, este script busca un punto de spawn (por Tag),
 * instancia tu prefab del jugador con PhotonNetwork.Instantiate y guarda la referencia en
 * `LocalPlayer.TagObject` para no duplicar el spawn si recargas escena.
 *
 * Cosas importantes:
 * - El prefab del jugador debe estar en `Resources/` y el nombre en `playerPrefabPath`.
 * - Los puntos de spawn deben tener el Tag `spawnTag` (ej. "Spawn") y los ordenamos por nombre para consistencia.
 * - Calculamos el índice de spawn usando `ActorNumber` (1-based) para repartir a la banda.
 * - Después de instanciar, ligamos la cámara principal al jugador local (y nos aseguramos que tenga AudioListener activo).
 *
 * Pro tips:
 * - Si no entraste a una Room, no spawnea (evita errores si pruebas la escena en local).
 * - Si ya se había spawneado (TagObject != null), solo volvemos a amarrar la cámara por si se perdió la referencia.
 *
 * @author Tú
 * @date 2025-08-16
 */

using Photon.Pun;
using UnityEngine;
using System.Linq;

/**
 * @class NetworkSpawner
 * @brief Admin de spawns en red con Photon: hace el spawn del jugador local y ajusta la cámara.
 *
 * @remarks
 * Monta este script en un GameObject de la escena (puede ser un "Network" vacío).
 * Asegúrate de que la escena ya tenga los puntos de spawn con el Tag correcto.
 */
public class NetworkSpawner : MonoBehaviourPunCallbacks
{
    [Header("Prefab en Resources/")]

    /// @brief Ruta dentro de `Resources/` del prefab del Player (sin extensión).
    /// @details Ejemplo: si tu prefab está en `Assets/Resources/Player.prefab`, aquí va "Player".
    [SerializeField] private string playerPrefabPath = "Player"; // Resources/Player.prefab

    [Header("Tag de los puntos de spawn")]

    /// @brief Tag que deben tener los puntos de spawn en la escena.
    /// @details Con esto encontramos y ordenamos los spawns para repartirlos por ActorNumber.
    [SerializeField] private string spawnTag = "Spawn";

    /// @brief Caché de transforms de los puntos de spawn (ordenados por nombre).
    private Transform[] spawns;

    /**
     * @brief Antes de todo, cacheamos los puntos de spawn para no estar buscando a cada rato.
     */
    void Awake()
    {
        CacheSpawns();
    }

    /**
     * @brief Al iniciar la escena intentamos spawnear por si ya estabas dentro de una Room.
     * @note Útil cuando recargas la escena estando conectado.
     */
    void Start()
    {
        // Por si esta escena se carga ya estando en una Room:
        TrySpawnIfNeeded("[Start]");
    }

    /**
     * @brief Callback de Photon cuando entras a una Room.
     * @details Aquí también intentamos spawnear (por si entraste justo ahora).
     */
    public override void OnJoinedRoom()
    {
        // Por si entras a la Room y aún no has spawneado:
        TrySpawnIfNeeded("[OnJoinedRoom]");
    }

    /**
     * @brief Busca y cachea los puntos de spawn por Tag; los ordena por nombre para consistencia.
     * @warning Si no encuentra ninguno, loguea un error para que ajustes la escena.
     */
    private void CacheSpawns()
    {
        spawns = GameObject.FindGameObjectsWithTag(spawnTag)
                 .OrderBy(go => go.name)
                 .Select(go => go.transform)
                 .ToArray();

        if (spawns.Length == 0)
            Debug.LogError($"[Spawner] No encontré spawns con Tag '{spawnTag}' en la escena.");
    }

    /**
     * @brief Intenta spawnear al jugador local si hace falta (o solo reata la cámara si ya existe).
     * @param caller Texto para identificar desde dónde se llamó (solo para logs bonitos).
     *
     * @details
     * Flujo:
     * 1) Si no estás en una Room, no spawneamos (evita errores en local).
     * 2) Si `LocalPlayer.TagObject` ya tiene algo, significa que el player ya fue instanciado:
     *    solo re-ligamos cámara (útil tras recargas).
     * 3) Si no hay spawns cacheados, los recacheamos.
     * 4) Elegimos spawn por `ActorNumber` y hacemos `PhotonNetwork.Instantiate`.
     * 5) Guardamos la referencia en `TagObject` y ligamos la cámara.
     */
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

        // Repartimos el índice de spawn usando ActorNumber (1-based)
        int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawns.Length;
        Transform spawn = spawns[index];

        // Instanciamos el player en red, importante: el prefab debe estar en Resources/
        GameObject playerGO = PhotonNetwork.Instantiate(playerPrefabPath, spawn.position, spawn.rotation);

        // Guardamos el GO del player local para detectar que ya spawneamos
        PhotonNetwork.LocalPlayer.TagObject = playerGO;

        // Amarramos la cámara principal al jugador local
        BindCameraToLocal(playerGO);

        Debug.Log($"[Spawner]{caller} Instancié '{playerPrefabPath}' para Actor #{PhotonNetwork.LocalPlayer.ActorNumber} en {spawn.name}.");
    }

    /**
     * @brief Liga la cámara principal al jugador local (si el PhotonView es mío).
     * @param playerGO GameObject del jugador local instanciado.
     *
     * @details
     * - Busca un componente `CameraController` en la cámara y le asigna el objetivo.
     * - Activa el `AudioListener` de la cámara (por si se había desactivado).
     * @note Asegúrate de que tu cámara tenga el script correcto (propiedad `objetive` en este ejemplo).
     */
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
