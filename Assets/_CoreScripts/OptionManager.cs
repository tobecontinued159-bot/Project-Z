using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Audio sliders and tab switching for OptionPanel.
/// Graphics and Controls stay empty; this script only shows or hides those content roots.
/// Attach this to OptionPanel.
/// </summary>
public class OptionManager : MonoBehaviour
{
    public const string MasterPrefsKey = "Option.MasterVolume";
    public const string MusicPrefsKey = "Option.MusicVolume";
    public const string SfxPrefsKey = "Option.SfxVolume";

    [Header("Tabs")]
    [SerializeField] private Button tabAudio;
    [SerializeField] private Button tabGraphics;
    [SerializeField] private Button tabControls;
    [SerializeField] private GameObject audioContent;
    [SerializeField] private GameObject graphicsContent;
    [SerializeField] private GameObject controlsContent;

    [Header("Audio Rows")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TMP_Text masterValueText;
    [SerializeField] private TMP_Text musicValueText;
    [SerializeField] private TMP_Text sfxValueText;

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterParameter = "MasterVolume";
    [SerializeField] private string musicParameter = "MusicVolume";
    [SerializeField] private string sfxParameter = "SfxVolume";

    private bool _ready;
    private bool _loggedMaster;
    private bool _loggedMusic;
    private bool _loggedSfx;

    private void OnEnable()
    {
        Initialize();
    }

    public void Initialize()
    {
        Resolve();
        if (Application.isPlaying == false || _ready)
        {
            return;
        }

        _ready = true;
        Bind();
        LoadAndApply();
        ShowAudioTab();
    }

    public void ShowAudioTab()
    {
        ShowContent(audioContent, graphicsContent, controlsContent);
        TintTabs(true, false, false);
    }

    public void ShowGraphicsTab()
    {
        ShowContent(graphicsContent, audioContent, controlsContent);
        TintTabs(false, true, false);
    }

    public void ShowControlsTab()
    {
        ShowContent(controlsContent, audioContent, graphicsContent);
        TintTabs(false, false, true);
    }

    private void Resolve()
    {
        if (audioContent == null)
        {
            audioContent = FindObject(transform, "AudioContent");
        }

        if (graphicsContent == null)
        {
            graphicsContent = FindObject(transform, "GraphicsContent");
        }

        if (controlsContent == null)
        {
            controlsContent = FindObject(transform, "ControlsContent");
        }

        if (tabAudio == null)
        {
            tabAudio = FindButton(transform, "TabAudio");
        }

        if (tabGraphics == null)
        {
            tabGraphics = FindButton(transform, "TabGraphics");
        }

        if (tabControls == null)
        {
            tabControls = FindButton(transform, "TabControls");
        }

        Transform audioRoot = audioContent != null ? audioContent.transform : null;
        BindRow(audioRoot, "MasterRow", ref masterSlider, ref masterValueText);
        BindRow(audioRoot, "MusicRow", ref musicSlider, ref musicValueText);
        BindRow(audioRoot, "SfxRow", ref sfxSlider, ref sfxValueText);
    }

    private void Bind()
    {
        BindTab(tabAudio, ShowAudioTab);
        BindTab(tabGraphics, ShowGraphicsTab);
        BindTab(tabControls, ShowControlsTab);
        BindSlider(masterSlider, OnMasterChanged);
        BindSlider(musicSlider, OnMusicChanged);
        BindSlider(sfxSlider, OnSfxChanged);
    }

    private void LoadAndApply()
    {
        float master = PlayerPrefs.GetFloat(MasterPrefsKey, 0.8f);
        float music = PlayerPrefs.GetFloat(MusicPrefsKey, 0.6f);
        float sfx = PlayerPrefs.GetFloat(SfxPrefsKey, 0.75f);
        SetSlider(masterSlider, master);
        SetSlider(musicSlider, music);
        SetSlider(sfxSlider, sfx);
        ApplyMaster(master);
        ApplyMusic(music);
        ApplySfx(sfx);
    }

    private void OnMasterChanged(float value)
    {
        ApplyMaster(value);
        PlayerPrefs.SetFloat(MasterPrefsKey, value);
        PlayerPrefs.Save();
    }

    private void OnMusicChanged(float value)
    {
        ApplyMusic(value);
        PlayerPrefs.SetFloat(MusicPrefsKey, value);
        PlayerPrefs.Save();
    }

    private void OnSfxChanged(float value)
    {
        ApplySfx(value);
        PlayerPrefs.SetFloat(SfxPrefsKey, value);
        PlayerPrefs.Save();
    }

    private void ApplyMaster(float value)
    {
        SetPercent(masterValueText, value);
        if (audioMixer != null)
        {
            AudioListener.volume = 1f;
            SetMixer(masterParameter, value, ref _loggedMaster);
            return;
        }

        AudioListener.volume = Mathf.Clamp01(value);
    }

    private void ApplyMusic(float value)
    {
        SetPercent(musicValueText, value);
        SetMixer(musicParameter, value, ref _loggedMusic);
    }

    private void ApplySfx(float value)
    {
        SetPercent(sfxValueText, value);
        SetMixer(sfxParameter, value, ref _loggedSfx);
    }

    private void SetMixer(string parameter, float linear, ref bool loggedMissing)
    {
        if (audioMixer == null || string.IsNullOrEmpty(parameter))
        {
            return;
        }

        float decibels = linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
        if (audioMixer.SetFloat(parameter, decibels) || loggedMissing)
        {
            return;
        }

        loggedMissing = true;
        Debug.LogWarning("OptionManager: AudioMixer is missing an exposed parameter named " + parameter + ".");
    }

    private static void SetSlider(Slider slider, float value)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }

    private static void SetPercent(TMP_Text label, float value)
    {
        if (label == null)
        {
            return;
        }

        int percent = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
        label.text = percent + "%";
    }

    private static void ShowContent(GameObject active, GameObject hiddenA, GameObject hiddenB)
    {
        SetActive(active, true);
        SetActive(hiddenA, false);
        SetActive(hiddenB, false);
    }

    private void TintTabs(bool audio, bool graphics, bool controls)
    {
        MenuButtonTint.Apply(tabAudio, audio);
        MenuButtonTint.Apply(tabGraphics, graphics);
        MenuButtonTint.Apply(tabControls, controls);
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private static void BindTab(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null)
        {
            return;
        }

        slider.onValueChanged.RemoveListener(action);
        slider.onValueChanged.AddListener(action);
    }

    private static void BindRow(Transform audioRoot, string rowName, ref Slider slider, ref TMP_Text valueText)
    {
        if (audioRoot == null)
        {
            return;
        }

        Transform row = FindDeep(audioRoot, rowName);
        if (row == null)
        {
            return;
        }

        if (slider == null)
        {
            slider = row.GetComponentInChildren<Slider>(true);
        }

        if (valueText == null)
        {
            Transform value = FindDeep(row, "ValueText");
            if (value != null)
            {
                valueText = value.GetComponent<TMP_Text>();
            }
        }
    }

    private static Button FindButton(Transform root, string childName)
    {
        Transform found = FindDeep(root, childName);
        if (found == null)
        {
            return null;
        }

        return found.GetComponent<Button>();
    }

    private static GameObject FindObject(Transform root, string childName)
    {
        Transform found = FindDeep(root, childName);
        if (found == null)
        {
            return null;
        }

        return found.gameObject;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private static Transform FindDeep(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
