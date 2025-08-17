/**
 * @file SimpleFollow.cs
 * @brief Script sencillito para que la cámara (o cualquier objeto) siga a otro.
 *
 * @details
 * La idea es que este compa agarra un "target" (otro objeto, normalmente el Player)
 * y mueve su posición poquito a poquito con un efecto suave.  
 * O sea, no se teletransporta, sino que se va "lerpeando" (mezclando posiciones).
 *
 * Cosas básicas:
 * - El `offset` es como la distancia fija desde donde quieres que lo mire (ej. cámara atrás del Player).
 * - El `smooth` es qué tan rápido se va acercando a la nueva posición.
 * - Se corre en `LateUpdate()` para que siempre se actualice después de que el Player ya se movió.
 */

using UnityEngine;

/**
 * @class SimpleFollow
 * @brief Hace que este objeto siga al target con un desplazamiento y suavizado.
 */
public class SimpleFollow : MonoBehaviour
{
    /// @brief El objetivo a seguir (ej. tu personaje).
    public Transform target;

    /// @brief Distancia fija respecto al target.  
    /// @details Por default está a 10 unidades hacia atrás en Z, como cámara clásica en 2D.
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    /// @brief Qué tan rápido se ajusta la posición (mientras más alto, más pegadito sigue).
    public float smooth = 10f;

    /**
     * @brief Se llama al final de cada frame para ajustar la posición.
     *
     * @details
     * - Si no hay target, no hace nada (por si se te olvida asignarlo).
     * - Usa `Vector3.Lerp` para mover de la posición actual a la del target + offset.
     * - Multiplica el smooth por `Time.deltaTime` para que sea dependiente del tiempo.
     */
    void LateUpdate()
    {
        if (!target) return; ///< Checa que no sea null, porque si no truena.

        transform.position = Vector3.Lerp(
            transform.position,       // Donde estoy ahorita
            target.position + offset, // Donde debería estar (el Player + offset)
            smooth * Time.deltaTime   // Qué tan rápido me acerco
        );
    }
}
