using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Acciones de botones de UI (MainMenu y GameOver).
/// En el Button → On Click() se elige LoadScene y se escribe el nombre de la escena.
/// </summary>
public class MenuUI : MonoBehaviour
{
    [SerializeField] private AudioClip clickSfx;

    public void LoadScene(string sceneName)
    {
        AudioManager.PlaySfx(clickSfx);
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        AudioManager.PlaySfx(clickSfx);
        Application.Quit();
    }
}
