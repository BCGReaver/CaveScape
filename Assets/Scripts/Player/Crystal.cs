using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Collider2D))]
public class Crystal : MonoBehaviourPun
{
    private bool consumed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        if (!other.CompareTag("Player")) return;

        var playerPV = other.GetComponent<PhotonView>();
        if (playerPV == null || !playerPV.IsMine) return; // solo el dueño procesa

        consumed = true;

        TeamCrystalsManager.AddCrystal(1);

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
        else
            photonView.RPC(nameof(RPC_RequestDestroy), RpcTarget.MasterClient);
    }

    [PunRPC]
    private void RPC_RequestDestroy()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }
}
