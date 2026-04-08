using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class CrystalBootstrapSolo : MonoBehaviourPunCallbacks
{
    [SerializeField] private string sceneMarkerTag = "CrystalMarker";
    [SerializeField] private string networkPrefabPath = "Crystal";    // Ruta en Resources
    [SerializeField] private GameObject localPrefab;                 // OPCIONAL: Arrastra aquí el prefab si es modo solo
    [SerializeField] private Transform clonesParent;

    private GameObject[] markers;
    private bool spawned;

    void Awake()
    {
        CacheMarkers();

        if (!clonesParent)
        {
            var go = GameObject.Find("Network_Crystals");
            if (!go) go = new GameObject("Network_Crystals");
            clonesParent = go.transform;
        }
    }

    void Start()
    {
        // En modo Solo, intentamos spawnear de inmediato
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            TrySpawn("[SoloMode]");
        }
        else
        {
            TrySpawn("[Start_Network]");
        }
    }

    public override void OnJoinedRoom()
    {
        // Si entramos a una sala de Photon, intentamos el spawn de red
        TrySpawn("[OnJoinedRoom]");
    }

    private void CacheMarkers()
    {
        markers = GameObject.FindGameObjectsWithTag(sceneMarkerTag);
        if (markers.Length == 0)
        {
            var parent = GameObject.Find("Crystals");
            if (parent)
            {
                var list = new List<GameObject>();
                foreach (Transform t in parent.transform) list.Add(t.gameObject);
                markers = list.ToArray();
            }
        }
    }

    private void TrySpawn(string who)
    {
        if (spawned) return;

        // --- LÓGICA PARA MODO SOLO (Sin Internet o fuera de sala) ---
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            SpawnLocally(who);
            return;
        }

        // --- LÓGICA PARA MULTIJUGADOR (Photon) ---
        if (PhotonNetwork.IsMasterClient)
        {
            int count = 0;
            foreach (var m in markers)
            {
                if (!m) continue;
                // Photon necesita que el prefab esté en una carpeta "Resources"
                var go = PhotonNetwork.InstantiateRoomObject(
                    networkPrefabPath, m.transform.position, m.transform.rotation
                );
                if (go) go.transform.SetParent(clonesParent, true);
                count++;
            }
            Debug.Log($"[Bootstrap]{who} instancié {count} cristales por RED.");
        }

        FinalizeSpawn();
    }

    private void SpawnLocally(string who)
    {
        int count = 0;
        // Si no asignaste un localPrefab en el inspector, intentamos cargar el de Resources
        GameObject prefabToUse = localPrefab;
        if (prefabToUse == null) prefabToUse = Resources.Load<GameObject>(networkPrefabPath);

        if (prefabToUse == null)
        {
            Debug.LogError("[Bootstrap] ¡No encontré el prefab del cristal para modo Solo!");
            return;
        }

        foreach (var m in markers)
        {
            if (!m) continue;
            GameObject go = Instantiate(prefabToUse, m.transform.position, m.transform.rotation);
            go.transform.SetParent(clonesParent, true);
            count++;
        }

        Debug.Log($"[Bootstrap]{who} instancié {count} cristales LOCALMENTE.");
        FinalizeSpawn();
    }

    private void FinalizeSpawn()
    {
        // Desactivamos los visuales de los marcadores (bolitas blancas o lo que uses)
        foreach (var m in markers) if (m) m.SetActive(false);
        spawned = true;
    }
}