using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public Image[] Heart;
    
    public void actualizarCorazones(int vida)
    {
        for (int i = 0; i < Heart.Length; i++)
        {
            Heart[i].gameObject.SetActive(i<vida);
        }
    }
}
