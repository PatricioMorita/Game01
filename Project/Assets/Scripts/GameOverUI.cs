using TMPro;
using UnityEngine;

/// <summary>
/// Muestra en la escena GameOver si el jugador ganó o perdió, y su puntaje.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private string winMessage = "¡Ganaste!";
    [SerializeField] private string loseMessage = "Game Over";
    [SerializeField] private string scoreFormat = "Monedas: {0}";

    private void Start()
    {
        titleText.text = GameResult.Won ? winMessage : loseMessage;
        scoreText.text = string.Format(scoreFormat, GameResult.Score);
    }
}
