using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Collider2D))]
public class CrystalSolo : MonoBehaviourPun
{
    private bool consumed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;

        // Verificamos si lo que tocó el cristal es el Jugador
        if (other.CompareTag("Player"))
        {
            // Intentamos obtener el script del jugador
            PlayerControllerSolo player = other.GetComponent<PlayerControllerSolo>();

            if (player != null)
            {
                // Verificación de red (si estamos en multiplayer)
                if (PhotonNetwork.IsConnected)
                {
                    var playerPV = player.GetComponent<PhotonView>();
                    if (playerPV == null || !playerPV.IsMine) return;
                }

                // SI PASA LAS PRUEBAS: Recoger
                Collect(player);
            }
        }
    }

    private void Collect(PlayerControllerSolo player)
    {
        consumed = true;

        // LLAMADA CLAVE: Le decimos al script del jugador que sume el cristal
        player.actualizarCrystals();

        // Destrucción Híbrida
        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.Destroy(gameObject);
            else
                photonView.RPC(nameof(RPC_RequestDestroy), RpcTarget.MasterClient);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [PunRPC]
    private void RPC_RequestDestroy()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }
}