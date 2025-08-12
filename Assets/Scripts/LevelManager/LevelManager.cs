using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField]
    private GameObject pause;
    private bool isPaused;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isPaused = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Pause();
        }
    }

    public void Pause()
    {
        if(isPaused == false)
        {
            pause.SetActive(true);
            Time.timeScale = 0.0f;
            isPaused = true;
        }
        else
        {
            pause.SetActive(false);
            Time.timeScale = 1.0f;
            isPaused = false;
        }
    }
    public void ExitToMainMenu()
    {
        Time.timeScale = 1.0f; 
        SceneManager.LoadScene(0);
    }

    public void Salir()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
