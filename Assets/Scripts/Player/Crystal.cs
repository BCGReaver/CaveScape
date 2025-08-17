/**
 * @file Crystal.cs
 * @brief Cristal coleccionable en red (Photon) que suma al contador y se destruye de forma segura.
 *
 * @details
 * Este compa vive en cada cristal del mapa. Cuando un Player lo toca:
 * - Solo el jugador dueño de su PhotonView (IsMine) procesa la recogida (para no duplicar).
 * - Suma +1 al total con `TeamCrystalsManager.AddCrystal(1)`.
 * - Pide destruir el cristal:
 *   - Si soy MasterClient, lo destruyo directo.
 *   - Si NO lo soy, le mando un RPC al Master para que él lo destruya (autoridad central).
 *
 * Notitas:
 * - `consumed` evita que el mismo trigger dispare dos veces por latencia/overlaps.
 * - Requiere un `Collider2D` en modo Trigger para detectar la entrada del Player.
 * - El Player debe tener Tag "Player" y un `PhotonView` para validar `IsMine`.
 */

using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Collider2D))]
/**
 * @class Crystal
 * @brief Maneja la recogida de un cristal y coordina su destrucción con el MasterClient.
 */
public class Crystal : MonoBehaviourPun
{
    /// @brief Bandera para que no se procese dos veces el mismo cristal.
    private bool consumed;

    /**
     * @brief Cuando algo entra al trigger, checamos si es el Player local y recogemos.
     * @param other El collider que entró (esperamos al Player).
     *
     * @details
     * - Si ya estaba consumido o no es el Player, salimos.
     * - Solo el dueño del Player (`IsMine`) ejecuta la lógica de sumar cristal.
     * - Destrucción:
     *   - Master: `PhotonNetwork.Destroy(gameObject)`.
     *   - Cliente: `RPC_RequestDestroy` al Master para que él lo destruya.
     */
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        if (!other.CompareTag("Player")) return;

        var playerPV = other.GetComponent<PhotonView>();
        if (playerPV == null || !playerPV.IsMine) return; // solo el dueño procesa

        consumed = true;

        // Suma al total (ajusta internamente si lo llevas por equipo o por jugador)
        TeamCrystalsManager.AddCrystal(1);

        // Destruir el cristal de forma autoritativa
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
        else
            photonView.RPC(nameof(RPC_RequestDestroy), RpcTarget.MasterClient);
    }

    /**
     * @brief RPC que los clientes usan para pedirle al Master que destruya el cristal.
     * @note Solo el Master ejecuta realmente el Destroy.
     */
    [PunRPC]
    private void RPC_RequestDestroy()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }
}
