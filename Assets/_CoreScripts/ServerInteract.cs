using UnityEngine;
using UnityEngine.UI; // 🟢 เพิ่ม namespace สำหรับใช้ Button

public class ServerInteract : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject terminalPanel;
    [SerializeField] private Button closeButton; // 🟢 เพิ่ม Reference ปุ่มปิด UI

    [SerializeField] private bool isPlayerInRange;

    private void Awake()
    {
        if (terminalPanel != null)
        {
            terminalPanel.SetActive(false);
        }

        // 🟢 ผูก Event กดปุ่ม Close ให้สั่งปิด UI ทันที
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseTerminal);
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

    // 🟢 สั่งปิดหน้าต่าง UI และปลดล็อก Input
    public void CloseTerminal()
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