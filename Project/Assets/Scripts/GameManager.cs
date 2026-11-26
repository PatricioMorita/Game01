using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Guarda el resultado de la partida para mostrarlo en la escena GameOver.
/// </summary>
public static class GameResult
{
    public static bool Won;
    public static int Score;
}

/// <summary>
/// Controla el estado del nivel: puntaje, victoria y derrota.
/// Debe existir uno en la escena Level1.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Escenas")]
    [SerializeField] private string gameOverScene = "GameOver";
    [SerializeField] private float delayBeforeEnd = 1.2f;   // espera para que suene el SFX

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private string scoreFormat = "Monedas: {0}";

    [Header("Audio")]
    [SerializeField] private AudioClip deathSfx;
    [SerializeField] private AudioClip winSfx;

    public int Score { get; private set; }
    private bool gameEnded;

    private void Awake()
    {
        Instance = this;
        GameResult.Won = false;
        GameResult.Score = 0;
        UpdateScoreUI();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddScore(int amount)
    {
        Score += amount;
        UpdateScoreUI();
    }

    public void PlayerDied() => EndGame(false, deathSfx);
    public void PlayerWon() => EndGame(true, winSfx);

    private void EndGame(bool won, AudioClip clip)
    {
        if (gameEnded) return;   // evita terminar dos veces
        gameEnded = true;

        GameResult.Won = won;
        GameResult.Score = Score;
        AudioManager.PlaySfx(clip);
        Invoke(nameof(LoadGameOver), delayBeforeEnd);
    }

    private void LoadGameOver() => SceneManager.LoadScene(gameOverScene);

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = string.Format(scoreFormat, Score);
    }
}
