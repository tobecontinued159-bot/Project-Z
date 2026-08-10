using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    public enum DifficultyLevel { Easy, Normal, Hard, Nightmare }
    [SerializeField] private DifficultyLevel currentDifficulty = DifficultyLevel.Normal;

    private void Awake()
    {
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

    public float GetDamageMultiplier()
    {
        switch (currentDifficulty)
        {
            case DifficultyLevel.Easy:
                return 0.5f;
            case DifficultyLevel.Normal:
                return 1.0f;
            case DifficultyLevel.Hard:
                return 1.5f;
            case DifficultyLevel.Nightmare:
                return 2.5f;
            default:
                return 1.0f;
        }
    }

    public void SetDifficulty(DifficultyLevel newDifficulty)
    {
        currentDifficulty = newDifficulty;
    }
}