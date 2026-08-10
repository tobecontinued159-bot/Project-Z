using UnityEngine;

public class SafeZone : MonoBehaviour
{
    private bool _playerInside = false;
    private bool _survivorInside = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = true;
            CheckMissionComplete();
        }
        else if (other.CompareTag("Survivor"))
        {
            _survivorInside = true;
            CheckMissionComplete();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = false;
        }
        else if (other.CompareTag("Survivor"))
        {
            _survivorInside = false;
        }
    }

    private void CheckMissionComplete()
    {
        if (_playerInside && _survivorInside)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel(true);
            }
        }
    }
}