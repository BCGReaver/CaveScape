/**
 * @file Health.cs
 * @brief Maneja los corazoncitos del HUD que representan la vida del jugador.
 *
 * @details
 * Este script se monta en un GameObject que tenga hijos con imágenes llamadas "Heart".
 * Lo que hace es:
 * - En `Awake()`, si no llenaste el array en el Inspector, busca automáticamente todos
 *   los hijos tipo `Image` que empiecen con "Heart" y los ordena por nombre.
 * - En `actualizarCorazones(int vida)`, enciende o apaga cada corazón según el valor de vida.
 *
 * Notas rápidas:
 * - El orden importa: si tus imágenes se llaman "Heart", "Heart (1)", "Heart (2)", etc.,
 *   quedarán acomodadas correctamente por el `OrderBy`.
 * - Si tu HUD tiene 3 corazones y `vida = 2`, solo los dos primeros quedan activos.
 */

using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/**
 * @class Health
 * @brief Control visual de la vida en el HUD mediante corazoncitos.
 */
public class Health : MonoBehaviour
{
    /// @brief Arreglo de imágenes de corazones (se llena en Inspector o auto en Awake).
    public Image[] Heart;

    /**
     * @brief En Awake, si no arrastraste los corazones en el Inspector, los busca por nombre.
     *
     * @details
     * - Busca entre todos los `Image` hijos.
     * - Filtra los que empiecen con "Heart".
     * - Los ordena alfabéticamente (queda "Heart", "Heart (1)", "Heart (2)", ...).
     */
    void Awake()
    {
        if (Heart == null || Heart.Length == 0)
        {
            Heart = GetComponentsInChildren<Image>(true)
                .Where(i => i && i.name.StartsWith("Heart"))
                .OrderBy(i => i.name)
                .ToArray();
        }
    }

    /**
     * @brief Activa o desactiva corazones según la vida actual.
     * @param vida La cantidad de corazones que deben estar visibles.
     *
     * @details
     * - Recorre el array de corazones.
     * - Si el índice es menor a `vida`, se activa; si no, se apaga.
     */
    public void actualizarCorazones(int vida)
    {
        if (Heart == null) return;
        for (int i = 0; i < Heart.Length; i++)
            if (Heart[i]) Heart[i].gameObject.SetActive(i < vida);
    }
}
