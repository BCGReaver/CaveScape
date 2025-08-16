using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Health : MonoBehaviour
{
    public Image[] Heart;

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

    public void actualizarCorazones(int vida)
    {
        if (Heart == null) return;
        for (int i = 0; i < Heart.Length; i++)
            if (Heart[i]) Heart[i].gameObject.SetActive(i < vida);
    }
}
