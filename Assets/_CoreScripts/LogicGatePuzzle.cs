using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LogicGatePuzzle : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject logicGatePanel;
    [SerializeField] private TMP_Text inputAText;
    [SerializeField] private TMP_Text inputBText;
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private TMP_Dropdown gateTypeDropdown;
    [SerializeField] private Button submitButton;

    [Header("Events")]
    public UnityEvent OnPuzzleSolved;

    private bool _inputA;
    private bool _inputB;

    private void Awake()
    {
        if (logicGatePanel == null)
        {
            logicGatePanel = gameObject;
        }

        EnsureDropdownOptions();
    }

    private void OnEnable()
    {
        PlayerInputLock.SetTerminalOpen(true);

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitClicked);
        }

        StartPuzzle();
    }

    private void OnDisable()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(OnSubmitClicked);
        }

        PlayerInputLock.SetTerminalOpen(false);
    }

    public void OpenPuzzle()
    {
        if (logicGatePanel != null)
        {
            logicGatePanel.SetActive(true);
        }
    }

    public void ClosePuzzle()
    {
        if (logicGatePanel != null)
        {
            logicGatePanel.SetActive(false);
        }
    }

    private void StartPuzzle()
    {
        RandomizeInputs();
        RefreshInputLabels();

        if (outputText != null)
        {
            outputText.text = "System Locked";
        }

        if (gateTypeDropdown != null)
        {
            gateTypeDropdown.value = 0;
            gateTypeDropdown.RefreshShownValue();
        }
    }

    private void RandomizeInputs()
    {
        int combination = Random.Range(0, 3);
        if (combination == 0)
        {
            _inputA = true;
            _inputB = true;
        }
        else if (combination == 1)
        {
            _inputA = true;
            _inputB = false;
        }
        else
        {
            _inputA = false;
            _inputB = true;
        }
    }

    private void RefreshInputLabels()
    {
        if (inputAText != null)
        {
            inputAText.text = BoolToBit(_inputA);
        }

        if (inputBText != null)
        {
            inputBText.text = BoolToBit(_inputB);
        }
    }

    private void OnSubmitClicked()
    {
        bool result = EvaluateSelectedGate();
        if (result)
        {
            if (outputText != null)
            {
                outputText.text = "ACCESS GRANTED";
            }

            OnPuzzleSolved.Invoke();
            ClosePuzzle();
            return;
        }

        if (outputText != null)
        {
            outputText.text = "ERROR: Output is 0";
        }
    }

    private bool EvaluateSelectedGate()
    {
        string gateName = GetSelectedGateName();
        if (gateName == "AND")
        {
            return _inputA && _inputB;
        }

        if (gateName == "OR")
        {
            return _inputA || _inputB;
        }

        if (gateName == "XOR")
        {
            return _inputA != _inputB;
        }

        return false;
    }

    private string GetSelectedGateName()
    {
        if (gateTypeDropdown == null || gateTypeDropdown.options == null || gateTypeDropdown.options.Count == 0)
        {
            return "AND";
        }

        int index = gateTypeDropdown.value;
        if (index < 0 || index >= gateTypeDropdown.options.Count)
        {
            index = 0;
        }

        string option = gateTypeDropdown.options[index].text;
        if (string.IsNullOrEmpty(option))
        {
            return "AND";
        }

        return option.Trim().ToUpperInvariant();
    }

    private void EnsureDropdownOptions()
    {
        if (gateTypeDropdown == null)
        {
            return;
        }

        if (gateTypeDropdown.options != null && gateTypeDropdown.options.Count > 0)
        {
            return;
        }

        gateTypeDropdown.ClearOptions();
        gateTypeDropdown.AddOptions(new List<string> { "AND", "OR", "XOR" });
    }

    private static string BoolToBit(bool value)
    {
        return value ? "1" : "0";
    }
}
