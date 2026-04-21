using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("SFX Clips")]
    public AudioClip crystalCollect;
    public AudioClip footsteps;
    public AudioClip getHit;
    public AudioClip ghostWakeUp;
    public AudioClip jump; // Aunque te falte, deja el espacio

    void Awake()
    {
        // Patron Singleton: para que solo haya uno y no se destruya al cambiar de escena
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlaySFXVariable(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.pitch = Random.Range(0.85f, 1.15f); // Variación de tono
        sfxSource.PlayOneShot(clip);
        sfxSource.pitch = 1f; // Resetear el tono a la normalidad
    }
}