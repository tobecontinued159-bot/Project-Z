using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkPlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private string sessionName = "TestRoom";
    public Transform[] SpawnPoints;

    public static NetworkPlayerSpawner Instance { get; private set; }
    public static readonly List<NetworkObject> AllPlayers = new List<NetworkObject>();

    private readonly List<NetworkObject> _spawnedPlayers = new List<NetworkObject>();
    private NetworkRunner _runner;
    private static bool _hasSpawnedLocalPlayer;
    private static NetworkObject _localPlayerObject;

    private Camera _cachedMainCamera;

    // Flag ùùùù?ùù???ù·ùùùùùù? Update ùùù? OnInput
    private bool _fireRequestPending;
    private bool _reloadRequestPending;

    public void SetPlayerPrefab(NetworkObject prefab)
    {
        playerPrefab = prefab;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (PlayerInputLock.IsTerminalOpen)
        {
            _fireRequestPending = false;
            _reloadRequestPending = false;
            return;
        }

        // [ù?ùùùù 1]: ù?ù?ùù·ùùùùù Fire (ùù?ùùùù/ùù?) ùùù Reload (ùùùù R) ù Update
        if (Input.GetButton("Fire1") || Input.GetButtonDown("Fire1"))
        {
            _fireRequestPending = true;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            _reloadRequestPending = true;
        }
    }

    private async void Start()
    {
        NetworkRunner externalRunner = FindExternalRunner();
        if (externalRunner != null)
        {
            AdoptExternalRunner(externalRunner);
            return;
        }

        _runner = GetComponent<NetworkRunner>();
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
        }

        if (_runner.IsRunning)
        {
            DisableFusionDebugIMGUI();
            Subscribe(_runner);
            return;
        }

        DisableFusionDebugIMGUI();

        _runner.AddCallbacks(this);
        DontDestroyOnLoad(gameObject);

        if (GetComponent<NetworkSceneManagerDefault>() == null)
        {
            gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        if (GetComponent<NetworkObjectProviderDefault>() == null)
        {
            gameObject.AddComponent<NetworkObjectProviderDefault>();
        }

        INetworkSceneManager sceneManager = GetComponent<INetworkSceneManager>();
        INetworkObjectProvider objectProvider = GetComponent<INetworkObjectProvider>();

        NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0 && activeScene.buildIndex < SceneManager.sceneCountInBuildSettings)
        {
            sceneInfo.AddSceneRef(SceneRef.FromIndex(activeScene.buildIndex), LoadSceneMode.Additive);
        }

        StartGameResult result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            Scene = sceneInfo,
            SceneManager = sceneManager,
            ObjectProvider = objectProvider,
            PlayerCount = 4
        });

        if (result.Ok == false)
        {
            Debug.LogError($"Fusion StartGame failed: {result.ShutdownReason}");
            return;
        }

        DisableFusionDebugIMGUI();
    }

    private NetworkRunner FindExternalRunner()
    {
        NetworkRunner[] runners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
        NetworkRunner running = null;
        NetworkRunner idle = null;
        for (int i = 0; i < runners.Length; i++)
        {
            NetworkRunner runner = runners[i];
            if (runner == null || runner.gameObject == gameObject)
            {
                continue;
            }

            if (runner.IsRunning)
            {
                running = runner;
            }
            else if (idle == null)
            {
                idle = runner;
            }
        }

        return running != null ? running : idle;
    }

    private void AdoptExternalRunner(NetworkRunner runner)
    {
        _runner = runner;
        DisableFusionDebugIMGUI();
        DisableUnusedLocalRunner(runner);
        ShutdownDuplicateRunners(runner);
        Subscribe(runner);
    }

    private void DisableUnusedLocalRunner(NetworkRunner keptRunner)
    {
        NetworkRunner localRunner = GetComponent<NetworkRunner>();
        if (localRunner == null || localRunner == keptRunner || localRunner.IsRunning)
        {
            return;
        }

        localRunner.enabled = false;
    }

    private static void ShutdownDuplicateRunners(NetworkRunner keptRunner)
    {
        NetworkRunner[] runners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
        for (int i = 0; i < runners.Length; i++)
        {
            NetworkRunner runner = runners[i];
            if (runner == null || runner == keptRunner || runner.IsRunning == false)
            {
                continue;
            }

            Debug.LogWarning("NetworkPlayerSpawner: shutting down duplicate runner on " + runner.gameObject.name);
            runner.Shutdown(false);
        }
    }

    private void Subscribe(NetworkRunner runner)
    {
        runner.RemoveCallbacks(this);
        runner.AddCallbacks(this);
    }

    private static void DisableFusionDebugIMGUI()
    {
        FusionBootstrapDebugGUI[] allDebugGuis = UnityEngine.Object.FindObjectsOfType<FusionBootstrapDebugGUI>(true);
        if (allDebugGuis == null || allDebugGuis.Length == 0)
        {
            return;
        }

        for (int i = 0; i < allDebugGuis.Length; i++)
        {
            FusionBootstrapDebugGUI gui = allDebugGuis[i];
            if (gui == null)
            {
                continue;
            }

            gui.enabled = false;
            UnityEngine.Object.Destroy(gui);
            Debug.Log($"Removed Fusion IMGUI Debug component from: {gui.gameObject.name}");
        }
    }

    private void OnDestroy()
    {
        Instance = null;
        if (_runner != null)
        {
            _runner.RemoveCallbacks(this);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player != runner.LocalPlayer)
        {
            return;
        }

        if (_runner != null && runner != _runner)
        {
            Debug.LogWarning("NetworkPlayerSpawner: ignored PlayerJoined from a duplicate runner.");
            return;
        }

        SpawnLocalPlayerIfNeeded(runner, player);
    }

    private void SpawnLocalPlayerIfNeeded(NetworkRunner runner, PlayerRef player)
    {
        if (player != runner.LocalPlayer)
        {
            return;
        }

        if (_hasSpawnedLocalPlayer || HasLocalPlayer(runner, player))
        {
            _hasSpawnedLocalPlayer = true;
            return;
        }

        if (runner.IsRunning == false || runner.CanSpawn == false)
        {
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("NetworkPlayerSpawner: playerPrefab is not assigned.");
            return;
        }

        _hasSpawnedLocalPlayer = true;

        if (TryGetSpawnPose(player, out Vector3 spawnPosition, out Quaternion spawnRotation) == false)
        {
            spawnPosition = new Vector3(0f, 1f, 0f);
            spawnRotation = Quaternion.identity;
        }

        NetworkObject networkPlayer = runner.Spawn(playerPrefab, spawnPosition, spawnRotation, player);
        if (networkPlayer == null)
        {
            _hasSpawnedLocalPlayer = false;
            Debug.LogError("NetworkPlayerSpawner: runner.Spawn returned null. Check that PlayerPrefab is a NetworkObject and has been baked.");
            return;
        }

        _localPlayerObject = networkPlayer;
        runner.SetPlayerObject(player, networkPlayer);
        _spawnedPlayers.Add(networkPlayer);
        RegisterPlayer(networkPlayer);
        Debug.Log($"Spawned local player for {player} at {spawnPosition}");
    }

    private bool HasLocalPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (_localPlayerObject != null)
        {
            return true;
        }

        if (runner != null && runner.TryGetPlayerObject(player, out NetworkObject registered) && registered != null)
        {
            _localPlayerObject = registered;
            return true;
        }

        for (int i = 0; i < AllPlayers.Count; i++)
        {
            NetworkObject candidate = AllPlayers[i];
            if (candidate != null && candidate.InputAuthority == player)
            {
                _localPlayerObject = candidate;
                return true;
            }
        }

        PlayerStats[] players = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            PlayerStats stats = players[i];
            if (stats == null || stats.Object == null || stats.Object.InputAuthority != player)
            {
                continue;
            }

            _localPlayerObject = stats.Object;
            return true;
        }

        return false;
    }

    public bool TryGetSpawnPose(PlayerRef player, out Vector3 position, out Quaternion rotation)
    {
        Transform point = GetSpawnPoint(player);
        if (point == null)
        {
            position = default;
            rotation = Quaternion.identity;
            return false;
        }

        position = point.position;
        rotation = point.rotation;
        return true;
    }

    private Transform GetSpawnPoint(PlayerRef player)
    {
        if (SpawnPoints == null || SpawnPoints.Length == 0)
        {
            return null;
        }

        int slot = player.AsIndex;
        if (slot < 0)
        {
            slot = _spawnedPlayers.Count;
        }

        int startIndex = slot % SpawnPoints.Length;
        for (int attempt = 0; attempt < SpawnPoints.Length; attempt++)
        {
            int index = (startIndex + attempt) % SpawnPoints.Length;
            Transform point = SpawnPoints[index];
            if (point != null)
            {
                return point;
            }
        }

        return null;
    }

    public static void RegisterPlayer(NetworkObject networkPlayer)
    {
        if (networkPlayer == null || AllPlayers.Contains(networkPlayer))
        {
            return;
        }

        AllPlayers.Add(networkPlayer);
    }

    public static void UnregisterPlayer(NetworkObject networkPlayer)
    {
        if (networkPlayer == null)
        {
            return;
        }

        AllPlayers.Remove(networkPlayer);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        for (int i = _spawnedPlayers.Count - 1; i >= 0; i--)
        {
            NetworkObject networkObject = _spawnedPlayers[i];
            if (networkObject == null)
            {
                _spawnedPlayers.RemoveAt(i);
                AllPlayers.Remove(networkObject);
                continue;
            }

            if (networkObject.InputAuthority != player)
            {
                continue;
            }

            if (networkObject.HasStateAuthority)
            {
                runner.Despawn(networkObject);
            }

            _spawnedPlayers.RemoveAt(i);
            AllPlayers.Remove(networkObject);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (PlayerInputLock.IsTerminalOpen)
        {
            PlayerInput lockedInput = new PlayerInput();
            lockedInput.MoveInput = Vector2.zero;
            lockedInput.FirePressed = false;
            lockedInput.ReloadPressed = false;
            input.Set(lockedInput);

            _fireRequestPending = false;
            _reloadRequestPending = false;
            return;
        }

        if (_cachedMainCamera == null)
        {
            _cachedMainCamera = Camera.main;
        }

        PlayerInput data = new PlayerInput();

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        data.MoveInput = new Vector2(horizontal, vertical);

        if (_cachedMainCamera != null)
        {
            Ray ray = _cachedMainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 lookPoint;

            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                lookPoint = hit.point;
            }
            else
            {
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enterDistance))
                {
                    lookPoint = ray.GetPoint(enterDistance);
                }
                else
                {
                    lookPoint = Vector3.zero;
                }
            }

            data.LookDirection = lookPoint;
        }

        // [ù?ùùùù 3]: ù?ù?ùùù Fire ùùù Reload ùùù Network Input ùùùùùùùùùùùùùùùùùù?ù?ù
        data.FirePressed = _fireRequestPending;
        data.ReloadPressed = _reloadRequestPending;

        _fireRequestPending = false;
        _reloadRequestPending = false;

        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (_runner != null && runner != _runner)
        {
            return;
        }

        _spawnedPlayers.Clear();
        AllPlayers.Clear();
        _localPlayerObject = null;
        _hasSpawnedLocalPlayer = false;
        _fireRequestPending = false;
        _reloadRequestPending = false;
        Debug.Log($"Fusion shutdown: {shutdownReason}");
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"Fusion disconnected: {reason}");
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        request.Accept();
    }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"Fusion connect failed: {reason}");
    }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (_runner != null && runner != _runner)
        {
            return;
        }

        if (runner.IsRunning == false || runner.LocalPlayer.IsRealPlayer == false)
        {
            return;
        }

        OnPlayerJoined(runner, runner.LocalPlayer);
    }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
}