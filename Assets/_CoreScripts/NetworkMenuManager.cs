using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// MainMenu flow for Photon Fusion 2 Shared Mode.
/// Switches the existing panels, validates the player name, and creates or joins sessions.
/// Attach this to Canvas. Button OnClick events are registered in code.
/// </summary>
public class NetworkMenuManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public const string PlayerNamePrefsKey = "PlayerName";
    public const int MinNameLength = 2;
    public const int MaxNameLength = 16;
    public const string SessionDifficultyKey = "d";
    public const string SessionMapKey = "m";
    public const string SessionPrivateKey = "p";

    public static readonly string[] DifficultyLabels = { "EASY", "NORMAL", "HARD" };
    public static readonly string[] MapLabels = { "SCHOOL", "HOSPITAL", "WAREHOUSE" };

    private const string UserIdPrefsKey = "FusionUserId";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject modePanel;
    [SerializeField] private GameObject createPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject lobbyPanel;

    [Header("Main")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Text nameError;

    [Header("Create")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_Text maxPlayersText;
    [SerializeField] private Toggle privateToggle;

    [Header("Menu Scripts")]
    [SerializeField] private SessionListUI sessionListUI;
    [SerializeField] private OptionManager optionManager;
    [SerializeField] private LoadingSpinner loadingSpinner;

    [Header("Fusion")]
    [Tooltip("NetworkObject prefab with LobbyManager. The host spawns it when the session starts. The gameplay scene is loaded later from the lobby.")]
    [SerializeField] private NetworkObject lobbyStatePrefab;
    [Tooltip("Kept so existing menu data stays assigned. LobbyManager loads the gameplay scene by name.")]
    [SerializeField] private int gameplaySceneBuildIndex = 1;
    [SerializeField] private float connectionTimeoutSeconds = 20f;
    [SerializeField] private float errorDisplaySeconds = 2.5f;

    private readonly List<SessionRow> _rooms = new List<SessionRow>();

    private NetworkRunner _runner;
    private AuthenticationValues _auth;
    private Task<StartGameResult> _lobbyTask;
    private GameObject _returnPanel;
    private Button _easyButton;
    private Button _normalButton;
    private Button _hardButton;
    private Button _schoolButton;
    private Button _hospitalButton;
    private Button _warehouseButton;
    private int _maxPlayers = 4;
    private int _difficulty;
    private int _map;
    private bool _privateRoom;
    private bool _listenersBound;
    private bool _isStartingSession;
    private bool _matchStarted;
    private bool _lobbyStateSpawned;

    public static NetworkMenuManager Instance { get; private set; }

    public int GameplaySceneBuildIndex
    {
        get { return gameplaySceneBuildIndex; }
    }
    private bool _timedOut;
    private bool _alive = true;

    public static string SavedPlayerName
    {
        get
        {
            string saved = PlayerPrefs.GetString(PlayerNamePrefsKey, string.Empty).Trim();
            if (saved.Length < MinNameLength)
            {
                return "Player";
            }

            if (saved.Length > MaxNameLength)
            {
                return saved.Substring(0, MaxNameLength);
            }

            return saved;
        }
    }

    private void OnValidate()
    {
        AutoAssign();
    }

    private void Awake()
    {
        Instance = this;
        AutoAssign();
    }

    private void Start()
    {
        AutoAssign();
        WarnIfMissing();
        if (nameInput != null)
        {
            nameInput.characterLimit = MaxNameLength;
            LoadSavedName();
        }

        BindListeners();
        if (optionManager != null)
        {
            optionManager.Initialize();
        }

        if (sessionListUI != null)
        {
            sessionListUI.Initialize();
        }

        ReadMaxPlayers();
        ApplySelectionVisuals();
        ShowMenuPanel(mainPanel);
        if (loadingSpinner != null)
        {
            loadingSpinner.Hide();
        }
        else if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        _alive = false;
        if (Instance == this)
        {
            Instance = null;
        }

        if (_runner != null)
        {
            _runner.RemoveCallbacks(this);
        }
    }

    public void LeaveLobby()
    {
        _lobbyStateSpawned = false;
        _isStartingSession = false;
        _matchStarted = false;
        LobbyCarry.Clear();
        HideLoading();
        ShowMenuPanel(mainPanel);
        LeaveLobbyAsync();
    }

    private async void LeaveLobbyAsync()
    {
        await ShutdownRunner(false);
    }

    public void OnPlayPressed()
    {
        if (TrySavePlayerName() == false)
        {
            return;
        }

        ShowMenuPanel(modePanel);
    }

    public void OnOptionsPressed()
    {
        ShowMenuPanel(optionPanel);
        if (optionManager != null)
        {
            optionManager.Initialize();
        }
    }

    public void OnExitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnQuickPlayPressed()
    {
        StartMatch(MatchKind.QuickPlay, null, modePanel, "Loading...", "Connecting to Server...");
    }

    public void OnCreateRoomPressed()
    {
        ShowMenuPanel(createPanel);
        ApplySelectionVisuals();
    }

    public void OnJoinRoomPressed()
    {
        ShowMenuPanel(joinPanel);
        if (sessionListUI != null)
        {
            sessionListUI.ShowRooms(_rooms);
        }

        RefreshSessionLobby();
    }

    public void OnBackToMain()
    {
        ShowMenuPanel(mainPanel);
    }

    public void OnBackToMode()
    {
        ShowMenuPanel(modePanel);
    }

    public void OnCreatePressed()
    {
        string roomName = roomNameInput != null ? roomNameInput.text.Trim() : string.Empty;
        if (roomName.Length == 0)
        {
            roomName = "PZ-" + UnityEngine.Random.Range(1000, 10000);
        }

        if (roomName.Length > 64)
        {
            roomName = roomName.Substring(0, 64);
        }

        _privateRoom = privateToggle != null && privateToggle.isOn;
        StartMatch(MatchKind.Create, roomName, createPanel, "Creating Room...", "Connecting to Server...");
    }

    public void JoinListedSession(string sessionName)
    {
        if (string.IsNullOrEmpty(sessionName))
        {
            return;
        }

        StartMatch(MatchKind.Join, sessionName, joinPanel, "Joining Room...", "Connecting to Server...");
    }

    public void OnMaxPlayersMinus()
    {
        SetMaxPlayers(_maxPlayers - 1);
    }

    public void OnMaxPlayersPlus()
    {
        SetMaxPlayers(_maxPlayers + 1);
    }

    public void OnEasyPressed()
    {
        SetDifficulty(0);
    }

    public void OnNormalPressed()
    {
        SetDifficulty(1);
    }

    public void OnHardPressed()
    {
        SetDifficulty(2);
    }

    public void OnSchoolPressed()
    {
        SetMap(0);
    }

    public void OnHospitalPressed()
    {
        SetMap(1);
    }

    public void OnWarehousePressed()
    {
        SetMap(2);
    }

    private async void StartMatch(MatchKind kind, string sessionName, GameObject returnPanel, string title, string subtitle)
    {
        if (_isStartingSession)
        {
            return;
        }

        if (TrySavePlayerName() == false)
        {
            ShowMenuPanel(mainPanel);
            return;
        }

        _isStartingSession = true;
        _matchStarted = false;
        _timedOut = false;
        _returnPanel = returnPanel;
        ShowLoading(title, subtitle);

        try
        {
            StartGameResult result = await RunStart(kind, sessionName);
            if (_alive == false || _matchStarted)
            {
                return;
            }

            if (_timedOut)
            {
                await PresentFailure("Connection timed out.");
                return;
            }

            if (result.Ok)
            {
                _matchStarted = true;
                EnterLobby();
                return;
            }

            await PresentFailure(Describe(result));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (_alive && _matchStarted == false)
            {
                await PresentFailure("Could not connect.");
            }
        }
    }

    private async Task<StartGameResult> RunStart(MatchKind kind, string sessionName)
    {
        if (_lobbyTask != null && _lobbyTask.IsCompleted == false)
        {
            try
            {
                await _lobbyTask;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        NetworkRunner runner = EnsureRunner();
        bool retried = false;
        while (_alive)
        {
            runner.ProvideInput = true;
            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(connectionTimeoutSeconds)))
            {
                StartGameArgs args = BuildArgs(runner, kind, sessionName, cts.Token);
                Task<StartGameResult> startTask = runner.StartGame(args);
                Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(Mathf.Max(1f, connectionTimeoutSeconds) + 2f));
                Task finished = await Task.WhenAny(startTask, timeoutTask);
                if (finished != startTask)
                {
                    _timedOut = true;
                    await ShutdownRunner(true);
                    return default;
                }

                StartGameResult result = await startTask;
                if (result.Ok || retried || result.ShutdownReason != ShutdownReason.AlreadyRunning)
                {
                    return result;
                }
            }

            retried = true;
            await ShutdownRunner(false);
            runner = EnsureRunner();
        }

        return default;
    }

    private StartGameArgs BuildArgs(NetworkRunner runner, MatchKind kind, string sessionName, CancellationToken token)
    {
        StartGameArgs args = new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = kind == MatchKind.QuickPlay ? null : sessionName,
            SceneManager = runner.GetComponent<INetworkSceneManager>(),
            ObjectProvider = runner.GetComponent<INetworkObjectProvider>(),
            AuthValues = BuildAuth(),
            IsVisible = true,
            IsOpen = true,
            StartGameCancellationToken = token
        };

        if (kind == MatchKind.QuickPlay)
        {
            args.PlayerCount = 4;
            args.SessionProperties = new Dictionary<string, SessionProperty>
            {
                { SessionPrivateKey, false }
            };
        }
        else if (kind == MatchKind.Create)
        {
            args.PlayerCount = Mathf.Clamp(_maxPlayers, 1, 4);
            args.SessionProperties = new Dictionary<string, SessionProperty>
            {
                { SessionDifficultyKey, _difficulty },
                { SessionMapKey, _map },
                { SessionPrivateKey, _privateRoom }
            };
        }

        return args;
    }

    private async void RefreshSessionLobby()
    {
        try
        {
            await EnsureLobbyAsync();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private async Task EnsureLobbyAsync()
    {
        NetworkRunner runner = EnsureRunner();
        if (runner.IsRunning)
        {
            await ShutdownRunner(false);
        }

        if (_alive == false)
        {
            return;
        }

        if (runner.IsCloudReady)
        {
            return;
        }

        if (_lobbyTask != null && _lobbyTask.IsCompleted == false)
        {
            await _lobbyTask;
            return;
        }

        _lobbyTask = runner.JoinSessionLobby(SessionLobby.Shared, null, BuildAuth(), null, CancellationToken.None, true);
        StartGameResult result = await _lobbyTask;
        if (_alive == false)
        {
            return;
        }

        if (result.Ok == false)
        {
            Debug.LogWarning("NetworkMenuManager: session list unavailable (" + result.ShutdownReason + ").");
        }
    }

    private async Task PresentFailure(string message)
    {
        Debug.LogWarning("NetworkMenuManager: " + message);
        if (loadingSpinner != null)
        {
            loadingSpinner.SetMessages("Failed", message);
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(errorDisplaySeconds));
        }
        catch (Exception)
        {
            return;
        }

        if (_alive == false)
        {
            return;
        }

        HideLoading();
        ShowMenuPanel(_returnPanel != null ? _returnPanel : mainPanel);
        _isStartingSession = false;
        if (_returnPanel == joinPanel)
        {
            RefreshSessionLobby();
        }
    }

    private async Task ShutdownRunner(bool force)
    {
        if (_runner == null)
        {
            return;
        }

        try
        {
            await _runner.Shutdown(false, ShutdownReason.Ok, force);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private NetworkRunner EnsureRunner()
    {
        if (_runner == null)
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
        }

        if (_runner == null)
        {
            GameObject runnerObject = new GameObject("NetworkRunner");
            _runner = runnerObject.AddComponent<NetworkRunner>();
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
        _runner.RemoveCallbacks(this);
        _runner.AddCallbacks(this);
        return _runner;
    }

    private AuthenticationValues BuildAuth()
    {
        if (_auth != null)
        {
            return _auth;
        }

        string userId = PlayerPrefs.GetString(UserIdPrefsKey, string.Empty);
        if (string.IsNullOrEmpty(userId))
        {
            userId = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(UserIdPrefsKey, userId);
            PlayerPrefs.Save();
        }

        _auth = new AuthenticationValues(userId);
        return _auth;
    }

    private void ShowLoading(string title, string subtitle)
    {
        if (loadingSpinner != null)
        {
            loadingSpinner.Show(title, subtitle);
            return;
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            loadingPanel.transform.SetAsLastSibling();
        }
    }

    private void HideLoading()
    {
        if (loadingSpinner != null)
        {
            loadingSpinner.Hide();
            return;
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    private void ShowMenuPanel(GameObject panel)
    {
        SetPanel(mainPanel, panel == mainPanel);
        SetPanel(modePanel, panel == modePanel);
        SetPanel(createPanel, panel == createPanel);
        SetPanel(joinPanel, panel == joinPanel);
        SetPanel(optionPanel, panel == optionPanel);
        SetPanel(lobbyPanel, panel == lobbyPanel);
    }

    private void EnterLobby()
    {
        _isStartingSession = false;
        HideLoading();
        ShowMenuPanel(lobbyPanel);
        TrySpawnLobbyState();
    }

    private void TrySpawnLobbyState()
    {
        if (_lobbyStateSpawned || _runner == null || _runner.IsRunning == false)
        {
            return;
        }

        if (_runner.IsSharedModeMasterClient == false)
        {
            return;
        }

        if (lobbyStatePrefab == null)
        {
            Debug.LogError("NetworkMenuManager: assign Lobby State Prefab (a prefab with NetworkObject and LobbyManager).");
            return;
        }

        NetworkObject spawned = _runner.Spawn(
            lobbyStatePrefab,
            Vector3.zero,
            Quaternion.identity,
            _runner.LocalPlayer,
            null,
            NetworkSpawnFlags.SharedModeStateAuthMasterClient);
        if (spawned == null)
        {
            Debug.LogError("NetworkMenuManager: failed to spawn LobbyManager. Bake the prefab in the Fusion prefab table.");
            return;
        }

        _lobbyStateSpawned = true;
    }

    private bool TrySavePlayerName()
    {
        string playerName = nameInput != null ? nameInput.text.Trim() : string.Empty;
        if (playerName.Length < MinNameLength)
        {
            ShowNameError("ENTER AT LEAST 2 CHARACTERS");
            return false;
        }

        if (playerName.Length > MaxNameLength)
        {
            playerName = playerName.Substring(0, MaxNameLength);
        }

        if (nameInput != null)
        {
            nameInput.text = playerName;
        }

        PlayerPrefs.SetString(PlayerNamePrefsKey, playerName);
        PlayerPrefs.Save();
        ClearNameError();
        return true;
    }

    private void LoadSavedName()
    {
        if (PlayerPrefs.HasKey(PlayerNamePrefsKey) == false)
        {
            return;
        }

        string saved = PlayerPrefs.GetString(PlayerNamePrefsKey, string.Empty).Trim();
        if (saved.Length > MaxNameLength)
        {
            saved = saved.Substring(0, MaxNameLength);
        }

        nameInput.text = saved;
    }

    private void ShowNameError(string message)
    {
        if (nameError == null)
        {
            Debug.LogWarning("NetworkMenuManager: " + message + ". Add a TMP object named NameError under NameInput to show this on the menu.");
            return;
        }

        nameError.gameObject.SetActive(true);
        nameError.text = message;
    }

    private void ClearNameError()
    {
        if (nameError == null)
        {
            return;
        }

        nameError.text = string.Empty;
        nameError.gameObject.SetActive(false);
    }

    private void SetMaxPlayers(int value)
    {
        _maxPlayers = Mathf.Clamp(value, 1, 4);
        if (maxPlayersText != null)
        {
            maxPlayersText.text = _maxPlayers.ToString();
        }
    }

    private void ReadMaxPlayers()
    {
        int parsed = 4;
        if (maxPlayersText != null && int.TryParse(maxPlayersText.text, out int fromLabel))
        {
            parsed = fromLabel;
        }

        SetMaxPlayers(parsed);
    }

    private void SetDifficulty(int index)
    {
        _difficulty = Mathf.Clamp(index, 0, DifficultyLabels.Length - 1);
        ApplySelectionVisuals();
    }

    private void SetMap(int index)
    {
        _map = Mathf.Clamp(index, 0, MapLabels.Length - 1);
        ApplySelectionVisuals();
    }

    private void ApplySelectionVisuals()
    {
        MenuButtonTint.Apply(_easyButton, _difficulty == 0);
        MenuButtonTint.Apply(_normalButton, _difficulty == 1);
        MenuButtonTint.Apply(_hardButton, _difficulty == 2);
        MenuButtonTint.Apply(_schoolButton, _map == 0);
        MenuButtonTint.Apply(_hospitalButton, _map == 1);
        MenuButtonTint.Apply(_warehouseButton, _map == 2);
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void BindListeners()
    {
        if (_listenersBound)
        {
            return;
        }

        _listenersBound = true;
        Bind(FindButton(mainPanel, "PlayButton"), OnPlayPressed);
        Bind(FindButton(mainPanel, "OptionButton"), OnOptionsPressed);
        Bind(FindButton(mainPanel, "ExitButton"), OnExitPressed);
        Bind(FindButton(modePanel, "QuickPlayButton"), OnQuickPlayPressed);
        Bind(FindButton(modePanel, "CreateRoomButton"), OnCreateRoomPressed);
        Bind(FindButton(modePanel, "JoinRoomButton"), OnJoinRoomPressed);
        Bind(FindButton(modePanel, "BackButton"), OnBackToMain);
        Bind(FindButton(createPanel, "BackButton"), OnBackToMode);
        Bind(FindButton(createPanel, "CreateButton"), OnCreatePressed);
        Bind(FindButton(createPanel, "MinusButton"), OnMaxPlayersMinus);
        Bind(FindButton(createPanel, "PlusButton"), OnMaxPlayersPlus);
        Bind(_easyButton, OnEasyPressed);
        Bind(_normalButton, OnNormalPressed);
        Bind(_hardButton, OnHardPressed);
        Bind(_schoolButton, OnSchoolPressed);
        Bind(_hospitalButton, OnHospitalPressed);
        Bind(_warehouseButton, OnWarehousePressed);
        Bind(FindButton(joinPanel, "BackButton"), OnBackToMode);
        Bind(FindButton(optionPanel, "BackButton"), OnBackToMain);
    }

    private void AutoAssign()
    {
        Transform root = UiRoot();
        if (mainPanel == null)
        {
            mainPanel = FindObject(root, "MainPanel");
        }

        if (modePanel == null)
        {
            modePanel = FindObject(root, "ModePanel");
        }

        if (createPanel == null)
        {
            createPanel = FindObject(root, "CreatePanel");
        }

        if (joinPanel == null)
        {
            joinPanel = FindObject(root, "JoinPanel");
        }

        if (optionPanel == null)
        {
            optionPanel = FindObject(root, "OptionPanel");
        }

        if (loadingPanel == null)
        {
            loadingPanel = FindObject(root, "LoadingPanel");
        }

        if (lobbyPanel == null)
        {
            lobbyPanel = FindObject(root, "LobbyPanel");
        }

        if (nameInput == null)
        {
            nameInput = FindInput(mainPanel, "NameInput");
        }

        if (nameError == null)
        {
            nameError = FindText(mainPanel, "NameError");
        }

        if (roomNameInput == null)
        {
            roomNameInput = FindInput(createPanel, "RoomNameInput");
        }

        if (maxPlayersText == null)
        {
            maxPlayersText = FindText(createPanel, "MaxPlayersText");
        }

        if (privateToggle == null)
        {
            Transform toggle = createPanel != null ? FindDeep(createPanel.transform, "PrivateToggle") : null;
            if (toggle != null)
            {
                privateToggle = toggle.GetComponent<Toggle>();
            }
        }

        _easyButton = FindButton(createPanel, "EasyButton");
        _normalButton = FindButton(createPanel, "NormalButton");
        _hardButton = FindButton(createPanel, "HardButton");
        _schoolButton = FindButton(createPanel, "SchoolButton");
        _hospitalButton = FindButton(createPanel, "HospitalButton");
        _warehouseButton = FindButton(createPanel, "WarehouseButton");

        if (sessionListUI == null && joinPanel != null)
        {
            sessionListUI = joinPanel.GetComponent<SessionListUI>();
        }

        if (optionManager == null && optionPanel != null)
        {
            optionManager = optionPanel.GetComponent<OptionManager>();
        }

        if (loadingSpinner == null && loadingPanel != null)
        {
            loadingSpinner = loadingPanel.GetComponentInChildren<LoadingSpinner>(true);
        }
    }

    private void WarnIfMissing()
    {
        if (mainPanel == null || modePanel == null || createPanel == null || joinPanel == null || optionPanel == null || loadingPanel == null || lobbyPanel == null)
        {
            Debug.LogWarning("NetworkMenuManager: one or more panels were not found. Put this component on Canvas.");
        }

        if (lobbyStatePrefab == null)
        {
            Debug.LogWarning("NetworkMenuManager: assign Lobby State Prefab so the lobby can sync ready state and character picks.");
        }

        if (nameInput == null)
        {
            Debug.LogWarning("NetworkMenuManager: NameInput was not found.");
        }

        if (sessionListUI == null)
        {
            Debug.LogWarning("NetworkMenuManager: add SessionListUI to JoinPanel.");
        }

        if (optionManager == null)
        {
            Debug.LogWarning("NetworkMenuManager: add OptionManager to OptionPanel.");
        }

        if (loadingSpinner == null)
        {
            Debug.LogWarning("NetworkMenuManager: add LoadingSpinner to Spinner.");
        }

        if (_easyButton == null || _schoolButton == null)
        {
            Debug.LogWarning("NetworkMenuManager: difficulty or map buttons were not found under CreatePanel.");
        }
    }

    private Transform UiRoot()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            return parentCanvas.transform;
        }

        Canvas sceneCanvas = FindFirstObjectByType<Canvas>();
        if (sceneCanvas != null)
        {
            return sceneCanvas.transform;
        }

        return transform;
    }

    private static string Describe(StartGameResult result)
    {
        switch (result.ShutdownReason)
        {
            case ShutdownReason.GameIsFull:
                return "That room is full.";
            case ShutdownReason.GameNotFound:
                return "That room no longer exists.";
            case ShutdownReason.GameIdAlreadyExists:
                return "A room with that name already exists.";
            case ShutdownReason.GameClosed:
                return "That room is closed.";
            case ShutdownReason.PhotonCloudTimeout:
            case ShutdownReason.ConnectionTimeout:
            case ShutdownReason.OperationTimeout:
                return "Connection timed out.";
            case ShutdownReason.MaxCcuReached:
                return "The server is full. Try again later.";
            case ShutdownReason.OperationCanceled:
                return "Connection canceled.";
            default:
                if (string.IsNullOrEmpty(result.ErrorMessage) == false)
                {
                    return result.ErrorMessage;
                }

                return "Could not connect (" + result.ShutdownReason + ").";
        }
    }

    private static string FormatInfo(SessionInfo info)
    {
        bool hasDifficulty = TryReadInt(info, SessionDifficultyKey, out int difficulty);
        bool hasMap = TryReadInt(info, SessionMapKey, out int map);
        if (hasDifficulty == false || hasMap == false)
        {
            return "QUICK PLAY";
        }

        difficulty = Mathf.Clamp(difficulty, 0, DifficultyLabels.Length - 1);
        map = Mathf.Clamp(map, 0, MapLabels.Length - 1);
        return DifficultyLabels[difficulty] + " / " + MapLabels[map];
    }

    private static bool IsPrivateSession(SessionInfo info)
    {
        if (info.IsVisible == false)
        {
            return true;
        }

        if (TryReadBool(info, SessionPrivateKey, out bool isPrivate))
        {
            return isPrivate;
        }

        return false;
    }

    private static bool TryReadInt(SessionInfo info, string key, out int value)
    {
        value = 0;
        if (info == null || info.Properties == null)
        {
            return false;
        }

        if (info.Properties.TryGetValue(key, out SessionProperty property) == false || property.IsInt == false)
        {
            return false;
        }

        value = property;
        return true;
    }

    private static bool TryReadBool(SessionInfo info, string key, out bool value)
    {
        value = false;
        if (info == null || info.Properties == null)
        {
            return false;
        }

        if (info.Properties.TryGetValue(key, out SessionProperty property) == false || property.Isbool == false)
        {
            return false;
        }

        value = property;
        return true;
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static Button FindButton(GameObject panel, string childName)
    {
        if (panel == null)
        {
            return null;
        }

        Transform found = FindDeep(panel.transform, childName);
        if (found == null)
        {
            return null;
        }

        return found.GetComponent<Button>();
    }

    private static TMP_InputField FindInput(GameObject panel, string childName)
    {
        if (panel == null)
        {
            return null;
        }

        Transform found = FindDeep(panel.transform, childName);
        if (found == null)
        {
            return null;
        }

        return found.GetComponent<TMP_InputField>();
    }

    private static TMP_Text FindText(GameObject panel, string childName)
    {
        if (panel == null)
        {
            return null;
        }

        Transform found = FindDeep(panel.transform, childName);
        if (found == null)
        {
            return null;
        }

        return found.GetComponent<TMP_Text>();
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

    private static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null && panel.activeSelf != active)
        {
            panel.SetActive(active);
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

    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        _rooms.Clear();
        if (sessionList != null)
        {
            for (int i = 0; i < sessionList.Count; i++)
            {
                SessionInfo info = sessionList[i];
                if (info == null || info.IsValid == false || string.IsNullOrEmpty(info.Name))
                {
                    continue;
                }

                bool isFull = info.IsOpen == false || (info.MaxPlayers > 0 && info.PlayerCount >= info.MaxPlayers);
                _rooms.Add(new SessionRow
                {
                    RoomName = info.Name,
                    InfoLabel = FormatInfo(info),
                    PlayerCount = info.PlayerCount,
                    MaxPlayers = info.MaxPlayers,
                    IsPrivate = IsPrivateSession(info),
                    IsFull = isFull
                });
            }
        }

        _rooms.Sort((a, b) => string.Compare(a.RoomName, b.RoomName, StringComparison.Ordinal));
        if (sessionListUI != null)
        {
            sessionListUI.ShowRooms(_rooms);
        }
    }

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (shutdownReason != ShutdownReason.Ok)
        {
            Debug.LogWarning("NetworkMenuManager: runner shutdown (" + shutdownReason + ").");
        }
    }
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    private enum MatchKind
    {
        QuickPlay,
        Create,
        Join
    }
}

public static class MenuButtonTint
{
    public static readonly Color Idle = new Color32(0x11, 0x16, 0x10, 0xFF);
    public static readonly Color Selected = new Color32(0x3B, 0x52, 0x28, 0xFF);

    public static void Apply(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = selected ? Selected : Idle;
        button.colors = colors;
    }
}
