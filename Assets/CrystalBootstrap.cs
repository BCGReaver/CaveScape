using Photon.Pun;
using UnityEngine;

public class CrystalBootstrap : MonoBehaviourPunCallbacks
{
    [SerializeField] private string sceneMarkerTag = "CrystalMarker"; // Tag de marcadores en escena
    [SerializeField] private string networkPrefabPath = "Crystal";    // Resources/Crystal.prefab
    [SerializeField] private Transform clonesParent;                  // opcional: arrástrale un GO vacío

    private GameObject[] markers;
    private bool spawned;

    void Awake()
    {
        CacheMarkers();

        // Contenedor solo para orden visual
        if (!clonesParent)
        {
            var go = GameObject.Find("Network_Crystals");
            if (!go) go = new GameObject("Network_Crystals");
            clonesParent = go.transform;
        }
    }

    void Start()
    {
        TrySpawn("[Start]");   // <- si ya entraste a la escena estando en Room
    }

    public override void OnJoinedRoom()
    {
        TrySpawn("[OnJoinedRoom]"); // <- si entras a Room desde esta escena
    }

    private void CacheMarkers()
    {
        markers = GameObject.FindGameObjectsWithTag(sceneMarkerTag);
        if (markers.Length == 0)
        {
            var parent = GameObject.Find("Crystals");
            if (parent)
            {
                var list = new System.Collections.Generic.List<GameObject>();
                foreach (Transform t in parent.transform) list.Add(t.gameObject);
                markers = list.ToArray();
                Debug.Log($"[Bootstrap] No encontré tag '{sceneMarkerTag}'. Usando hijos de 'Crystals': {markers.Length}");
            }
        }
        else
        {
            Debug.Log($"[Bootstrap] Marcadores con tag '{sceneMarkerTag}': {markers.Length}");
        }
    }

    private void TrySpawn(string who)
    {
        if (spawned) return;
        if (!PhotonNetwork.InRoom) return;

        if (PhotonNetwork.IsMasterClient)
        {
            int count = 0;
            foreach (var m in markers)
            {
                if (!m) continue;
                var go = PhotonNetwork.InstantiateRoomObject(
                    networkPrefabPath, m.transform.position, m.transform.rotation
                );
                if (go) go.transform.SetParent(clonesParent, true); // orden local
                count++;
            }
            Debug.Log($"[Bootstrap]{who} instancié {count} cristales de red.");
        }

        foreach (var m in markers) if (m) m.SetActive(false); // ocultar marcadores locales
        spawned = true;
    }
}
