using UnityEngine;

public class ServerInteract : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject terminalPanel;

    [SerializeField] private bool isPlayerInRange;

    private void Awake()
    {
        if (terminalPanel != null)
        {
            terminalPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (isPlayerInRange == false || PlayerInputLock.IsTerminalOpen)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) == false)
        {
            return;
        }

        if (terminalPanel == null)
        {
            return;
        }

        terminalPanel.SetActive(true);
        PlayerInputLock.SetTerminalOpen(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsLocalPlayer(other) == false)
        {
            return;
        }

        isPlayerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsLocalPlayer(other) == false)
        {
            return;
        }

        isPlayerInRange = false;
        CloseTerminal();
    }

    private void CloseTerminal()
    {
        if (terminalPanel != null)
        {
            terminalPanel.SetActive(false);
        }

        PlayerInputLock.SetTerminalOpen(false);
    }

    private static bool IsLocalPlayer(Collider other)
    {
        if (other == null || other.CompareTag("Player") == false)
        {
            return false;
        }

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats == null)
        {
            return true;
        }

        return stats.HasStateAuthority || stats.HasInputAuthority;
    }
}
