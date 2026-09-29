using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class TerminalHacker : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text targetCommandText;
    [SerializeField] private TMP_InputField terminalInput;
    [SerializeField] private TMP_Text errorText;

    [Header("Commands Pool")]
    [SerializeField]
    private string[] commandsList =
    {
        "print ammo",
        "print buff",
        "print clearzombie",
        "print point",
        "print immortal"
    };

    [Header("Events")]
    public UnityEvent OnHackSuccess;

    private string _currentCommand = "";

    private void OnEnable()
    {
        PlayerInputLock.SetTerminalOpen(true);

        PickRandomCommand();
        ClearInput();
        ClearError();

        if (terminalInput != null)
        {
            terminalInput.onSubmit.AddListener(OnTerminalSubmit);
        }

        FocusInput();
    }

    private void OnDisable()
    {
        if (terminalInput != null)
        {
            terminalInput.onSubmit.RemoveListener(OnTerminalSubmit);
        }

        PlayerInputLock.SetTerminalOpen(false);
    }

    private void OnTerminalSubmit(string submittedText)
    {
        if (string.IsNullOrEmpty(_currentCommand))
        {
            ShowAccessDenied();
            return;
        }

        // 🟢 ตรวจสอบคำสั่ง (ตัด Space หน้าหลัง และไม่สนใจตัวพิมพ์เล็ก-ใหญ่)
        if (submittedText.Trim().Equals(_currentCommand.Trim(), System.StringComparison.OrdinalIgnoreCase))
        {
            if (TerminalBuffManager.Instance != null)
            {
                TerminalBuffManager.Instance.ApplyTerminalBuff(_currentCommand);
            }
            else
            {
                Debug.LogWarning("TerminalHacker: TerminalBuffManager is missing from the scene.");
            }

            OnHackSuccess.Invoke();
            gameObject.SetActive(false);
            return;
        }

        ShowAccessDenied();
    }

    private void PickRandomCommand()
    {
        if (commandsList == null || commandsList.Length == 0)
        {
            _currentCommand = "";
            if (targetCommandText != null)
            {
                targetCommandText.text = "";
            }
            return;
        }

        int index = Random.Range(0, commandsList.Length);
        _currentCommand = commandsList[index];

        if (targetCommandText != null)
        {
            targetCommandText.text = _currentCommand;
        }
    }

    private void ShowAccessDenied()
    {
        if (errorText != null)
        {
            errorText.text = "Access Denied!";
        }

        ClearInput();
        FocusInput();
    }

    private void ClearInput()
    {
        if (terminalInput == null)
        {
            return;
        }

        terminalInput.text = "";
        terminalInput.caretPosition = 0;
    }

    private void ClearError()
    {
        if (errorText != null)
        {
            errorText.text = "";
        }
    }

    private void FocusInput()
    {
        if (terminalInput == null)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(terminalInput.gameObject);
        }

        terminalInput.Select();
        terminalInput.ActivateInputField();
    }
}