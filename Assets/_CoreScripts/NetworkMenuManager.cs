using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkMenuManager : MonoBehaviour
{
    public const string PlayerNamePrefsKey = "PlayerName";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject selectModePanel;
    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private GameObject joinRoomPanel;
    [SerializeField] private GameObject loadingPanel;

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField createRoomNameInput;
    [SerializeField] private TMP_InputField joinRoomNameInput;

    [Header("Fusion")]
    [SerializeField] private int gameplaySceneBuildIndex = 1;
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private string defaultRoomName = "ProjectZ";

    private NetworkRunner _runner;
    private bool _isStartingSession;

    private void Start()
    {
        LoadSavedPlayerName();
        ShowPanel(mainPanel);
    }

    public void ShowPanel(GameObject panelToShow)
    {
        SetPanelActive(mainPanel, panelToShow == mainPanel);
        SetPanelActive(selectModePanel, panelToShow == selectModePanel);
        SetPanelActive(createRoomPanel, panelToShow == createRoomPanel);
        SetPanelActive(joinRoomPanel, panelToShow == joinRoomPanel);
        SetPanelActive(loadingPanel, panelToShow == loadingPanel);
    }

    public void ShowMainPanel()
    {
        ShowPanel(mainPanel);
    }

    public void ShowSelectModePanel()
    {
        ShowPanel(selectModePanel);
    }

    public void ShowCreateRoomPanel()
    {
        ShowPanel(createRoomPanel);
    }

    public void ShowJoinRoomPanel()
    {
        ShowPanel(joinRoomPanel);
    }

    public void OnCreateRoomSubmit()
    {
        SavePlayerName();
        StartSharedSession(GetRoomName(createRoomNameInput));
    }

    public void OnJoinRoomSubmit()
    {
        SavePlayerName();
        StartSharedSession(GetRoomName(joinRoomNameInput));
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public async void StartSharedSession(string sessionName)
    {
        if (_isStartingSession)
        {
            return;
        }

        string roomName = string.IsNullOrWhiteSpace(sessionName) ? defaultRoomName : sessionName.Trim();
        _isStartingSession = true;
        ShowPanel(loadingPanel);

        NetworkRunner runner = EnsureRunner();
        if (runner.IsRunning)
        {
            Debug.LogWarning("NetworkMenuManager: NetworkRunner is already running.");
            _isStartingSession = false;
            return;
        }

        NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(SceneRef.FromIndex(gameplaySceneBuildIndex), LoadSceneMode.Single);

        StartGameResult result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = roomName,
            Scene = sceneInfo,
            SceneManager = runner.GetComponent<INetworkSceneManager>(),
            ObjectProvider = runner.GetComponent<INetworkObjectProvider>(),
            PlayerCount = Mathf.Max(2, maxPlayers)
        });

        if (result.Ok == false)
        {
            Debug.LogError($"NetworkMenuManager: StartGame failed ({result.ShutdownReason}).");
            _isStartingSession = false;
            ShowPanel(mainPanel);
        }
    }

    private NetworkRunner EnsureRunner()
    {
        if (_runner != null)
        {
            return _runner;
        }

        _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner == null)
        {
            GameObject runnerGo = new GameObject("NetworkRunner");
            _runner = runnerGo.AddComponent<NetworkRunner>();
        }

        if (_runner.GetComponent<NetworkSceneManagerDefault>() == null)
        {
            _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        if (_runner.GetComponent<NetworkObjectProviderDefault>() == null)
        {
            _runner.gameObject.AddComponent<NetworkObjectProviderDefault>();
        }

        DontDestroyOnLoad(_runner.gameObject);
        return _runner;
    }

    private void SavePlayerName()
    {
        string playerName = playerNameInput != null ? playerNameInput.text : "";
        playerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();

        PlayerPrefs.SetString(PlayerNamePrefsKey, playerName);
        PlayerPrefs.Save();
    }

    private void LoadSavedPlayerName()
    {
        if (playerNameInput == null)
        {
            return;
        }

        if (PlayerPrefs.HasKey(PlayerNamePrefsKey))
        {
            playerNameInput.text = PlayerPrefs.GetString(PlayerNamePrefsKey);
        }
    }

    private string GetRoomName(TMP_InputField roomInput)
    {
        if (roomInput == null || string.IsNullOrWhiteSpace(roomInput.text))
        {
            return defaultRoomName;
        }

        return roomInput.text.Trim();
    }

    private static void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }
}
