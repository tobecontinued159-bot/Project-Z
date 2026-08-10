using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject scorePanel;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI survivorText;

    private int _zombiesKilled = 0;
    private bool _isGameOver = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddZombieKill()
    {
        if (_isGameOver) return;
        _zombiesKilled++;
        Debug.Log("Zombies Defeated: " + _zombiesKilled);
    }

    public void CompleteLevel(bool survivorRescued)
    {
        if (_isGameOver) return;
        _isGameOver = true;

        Time.timeScale = 0f;

        if (scorePanel != null && killsText != null && survivorText != null)
        {
            killsText.text = "Zombies Hunted: " + _zombiesKilled;
            survivorText.text = "Survivor Status: " + (survivorRescued ? "RESCUED (+500 pts)" : "ABANDONED");
            scorePanel.SetActive(true);
        }

        Debug.Log("====== STAGE CLEAR ======");
        Debug.Log("Total Zombies Hunted: " + _zombiesKilled);
        Debug.Log("Survivor Status: " + (survivorRescued ? "RESCUED" : "ABANDONED"));
        Debug.Log("=========================");
    }
}