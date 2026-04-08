using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Collider2D))]
public class CrystalSolo : MonoBehaviourPun
{
    private bool consumed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        if (!other.CompareTag("Player")) return;

        // --- LÓGICA DE VALIDACIÓN ---
        if (PhotonNetwork.IsConnected)
        {
            var playerPV = other.GetComponent<PhotonView>();
            // En red, solo el dueño del player que lo toca puede "recomogerlo"
            if (playerPV == null || !playerPV.IsMine) return;
        }

        // Si llegamos aquí, es porque o no hay red (Single Player) 
        // o soy el dueño en Multiplayer.
        Collect();
    }

    private void Collect()
    {
        consumed = true;

        // Sumar al manager (asegúrate de que TeamCrystalsManager funcione offline)
        TeamCrystalsManager.AddCrystal(1);

        // --- DESTRUCCIÓN HÍBRIDA ---
        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.Destroy(gameObject);
            else
                photonView.RPC(nameof(RPC_RequestDestroy), RpcTarget.MasterClient);
        }
        else
        {
            // En Single Player, un simple Destroy de Unity basta
            Destroy(gameObject);
        }
    }

    [PunRPC]
    private void RPC_RequestDestroy()
    {
        // Solo se ejecuta en el MasterClient cuando un cliente toca el cristal
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }
}