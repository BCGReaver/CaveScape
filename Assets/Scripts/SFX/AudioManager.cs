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

    private Coroutine enemyCoroutine;
    private Coroutine stepsCoroutine;

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

    public void PlaySFXWithTime(AudioClip clip, float startTime = 0f)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.Stop(); // Detiene el sonido anterior si quieres que este sea el principal
        sfxSource.clip = clip;
        sfxSource.time = startTime; // Aquí le dices en qué segundo empezar
        sfxSource.Play();
    }

    public void PlaySFXCustom(AudioClip clip, float duration, bool isStep = false)
    {
        if (clip == null || sfxSource == null) return;

        // Si es un paso, usamos el sfxSource normal, 
        // pero si quieres que no se corten entre ellos, lo ideal es PlayOneShot.
        // Sin embargo, para LIMITAR la duración, haremos esto:

        StartCoroutine(PlayAndStop(clip, duration));
    }

    public void PlayShortSFX(AudioClip clip, float duration)
    {
        StartCoroutine(PlayAndStop(clip, duration));
    }

    public System.Collections.IEnumerator PlayAndStop(AudioClip clip, float duration)
    {
        // Creamos un objeto temporal para que los sonidos no se corten entre sí
        GameObject tempGO = new GameObject("TempAudio");
        AudioSource tempSource = tempGO.AddComponent<AudioSource>();

        tempSource.clip = clip;
        tempSource.pitch = sfxSource.pitch; // Copiamos el pitch variable si existe
        tempSource.spatialBlend = 0; // 2D
        tempSource.Play();

        yield return new WaitForSeconds(duration);

        tempSource.Stop();
        Destroy(tempGO); // Borramos el objeto para no llenar la memoria
    }
}