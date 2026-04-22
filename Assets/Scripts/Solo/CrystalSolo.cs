using UnityEngine;
using Photon.Pun;

public class CrystalSolo : MonoBehaviourPun
{
    private bool consumed = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;

        // Importante: Asegúrate de que el Player tenga el Tag "Player"
        if (other.CompareTag("Player"))
        {
            PlayerControllerSolo player = other.GetComponent<PlayerControllerSolo>();

            if (player != null)
            {
                // Si hay red, solo el dueño del personaje puede recogerlo
                if (PhotonNetwork.IsConnected && photonView != null)
                {
                    // En modo Solo, esto suele ser true o no existir
                    if (player.photonView != null && !player.photonView.IsMine) return;
                }

                Collect(player);
            }
        }
    }

    private void Collect(PlayerControllerSolo player)
    {
        consumed = true;
        Debug.Log("¡Cristal detectado y recogido!"); // Si esto no sale en consola, es problema de Rigidbody o Tag

        // 1. Sonido
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.crystalCollect);

        // 2. AVISAR AL MANAGER (Esto te faltaba)
        CrystalsManagerSolo.AddCrystal(1);

        // 3. Destruir
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.Destroy(gameObject);
            else
                photonView.RPC("RPC_RequestDestroy", RpcTarget.MasterClient);
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