using TMPro;
using UnityEngine;

public class WaveHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI waveText;

    private WaveManager _waveManager;

    private void Awake()
    {
        if (waveText == null)
        {
            waveText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        if (waveText == null)
        {
            return;
        }

        if (TryGetWaveManager() == false)
        {
            waveText.text = "Wave: -- | Zombies: --";
            return;
        }

        if (_waveManager.Object == null || _waveManager.Object.IsValid == false)
        {
            waveText.text = "Wave: -- | Zombies: --";
            return;
        }

        if (_waveManager.IsBreakTime)
        {
            float remaining = _waveManager.RemainingBreakSeconds;
            if (remaining > 0f)
            {
                waveText.text = $"Break Time... Get Ready! ({Mathf.CeilToInt(remaining)}s)";
            }
            else
            {
                waveText.text = "Break Time... Get Ready!";
            }

            return;
        }

        waveText.text = $"Wave: {_waveManager.CurrentWave} | Zombies: {_waveManager.ZombiesRemaining}";
    }

    private bool TryGetWaveManager()
    {
        if (_waveManager != null)
        {
            return true;
        }

        if (WaveManager.Instance != null)
        {
            _waveManager = WaveManager.Instance;
            return true;
        }

        _waveManager = FindFirstObjectByType<WaveManager>();
        return _waveManager != null;
    }
}
