using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkPlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkObject playerPrefab;
    [Tooltip("Survivor 1 to Survivor 4, same order as the lobby cards. Used when the lobby selection has no prefab copy.")]
    [SerializeField] private NetworkObject[] characterPrefabs = new NetworkObject[4];
    [SerializeField] private string sessionName = "TestRoom";
    public Transform[] SpawnPoints;

    public static NetworkPlayerSpawner Instance { get; private set; }
    public static readonly List<NetworkObject> AllPlayers = new List<NetworkObject>();

    private readonly List<NetworkObject> _spawnedPlayers = new List<NetworkObject>();
    private NetworkRunner _runner;
    private static bool _hasSpawnedLocalPlayer;
    private static NetworkObject _localPlayerObject;
    private bool _loggedMissingPrefab;
    private bool _loggedMissingCharacter;

    private Camera _cachedMainCamera;

    // Flag ùùùù?ùù???ù·ùùùùùù? Update ùùù? OnInput
    private bool _fireRequestPending;
    private bool _reloadRequestPending;
    private readonly List<PlayerRef> _playersWaitingToSpawn = new List<PlayerRef>();
    private static bool _spawnInProgress;
    private int _framesRunnerReady;
    private bool _refusedScenePrefab;

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
        ProcessSpawnQueue();

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

        // ????????????????? Lobby ???? OnPlayerJoined ?????????????????????
        // ???? Runner ????? ??????????????????? spawn ????
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

        _runner.ProvideInput = true;
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
        if (_runner != null && runner != _runner)
        {
            Debug.LogWarning("NetworkPlayerSpawner: ignored PlayerJoined from a duplicate runner.");
            return;
        }

        QueuePlayerSpawn(runner, player);
    }

    private static bool IsRunnerReadyToSpawn(NetworkRunner runner)
    {
        if (runner == null || runner.IsStarting || runner.IsShutdown || runner.IsRunning == false)
        {
            return false;
        }

        // Spawning while the scene is still loading allocates an id before Simulation can,
        // which throws in GetNextId and leaves the clone owned by this runner.
        if (runner.IsSceneManagerBusy)
        {
            return false;
        }

        return runner.CanSpawn;
    }

    private static bool HasAuthorityToSpawn(NetworkRunner runner, PlayerRef player)
    {
        if (runner == null || runner.IsRunning == false || player.IsRealPlayer == false)
        {
            return false;
        }

        // Host / Server: only the server has state authority to create player objects.
        if (runner.GameMode != GameMode.Shared)
        {
            return runner.IsServer;
        }

        // Shared: the peer that calls Spawn becomes state authority, so only spawn yourself.
        return player == runner.LocalPlayer;
    }

    private void QueuePlayerSpawn(NetworkRunner runner, PlayerRef player)
    {
        if (_refusedScenePrefab || _spawnInProgress || HasAuthorityToSpawn(runner, player) == false)
        {
            return;
        }

        if (player == runner.LocalPlayer && _hasSpawnedLocalPlayer)
        {
            return;
        }

        if (HasLocalPlayer(runner, player))
        {
            if (player == runner.LocalPlayer)
            {
                _hasSpawnedLocalPlayer = true;
            }

            return;
        }

        for (int i = 0; i < _playersWaitingToSpawn.Count; i++)
        {
            if (_playersWaitingToSpawn[i] == player)
            {
                return;
            }
        }

        _playersWaitingToSpawn.Add(player);
    }

    private void ProcessSpawnQueue()
    {
        if (_refusedScenePrefab || _spawnInProgress)
        {
            return;
        }

        if (_runner != null && _hasSpawnedLocalPlayer == false && IsRunnerReadyToSpawn(_runner))
        {
            QueuePlayerSpawn(_runner, _runner.LocalPlayer);
        }

        if (_playersWaitingToSpawn.Count == 0 || IsRunnerReadyToSpawn(_runner) == false)
        {
            _framesRunnerReady = 0;
            return;
        }

        _framesRunnerReady++;
        if (_framesRunnerReady < 2)
        {
            return;
        }

        PlayerRef player = _playersWaitingToSpawn[0];
        _playersWaitingToSpawn.RemoveAt(0);
        _framesRunnerReady = 0;
        SpawnPlayer(_runner, player);
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnInProgress || _refusedScenePrefab || HasAuthorityToSpawn(runner, player) == false)
        {
            return;
        }

        if (HasLocalPlayer(runner, player))
        {
            if (player == runner.LocalPlayer)
            {
                _hasSpawnedLocalPlayer = true;
            }

            return;
        }

        NetworkObject prefab = player == runner.LocalPlayer ? ResolvePlayerPrefab() : playerPrefab;
        if (prefab == null)
        {
            if (_loggedMissingPrefab == false)
            {
                _loggedMissingPrefab = true;
                Debug.LogError("NetworkPlayerSpawner: playerPrefab is not assigned.");
            }

            return;
        }

        if (prefab.gameObject.scene.IsValid())
        {
            _refusedScenePrefab = true;
            _playersWaitingToSpawn.Clear();
            Debug.LogError("NetworkPlayerSpawner: player prefab is a scene instance already owned by the runner. Assign the prefab asset from the Project window.");
            return;
        }

        if (TryGetSpawnPose(player, out Vector3 spawnPosition, out Quaternion spawnRotation) == false)
        {
            spawnPosition = new Vector3(0f, 1f, 0f);
            spawnRotation = Quaternion.identity;
        }

        // Block every other spawner before Instantiate, so a second Spawn cannot claim the same clone.
        _spawnInProgress = true;

        NetworkObject networkPlayer = runner.Spawn(prefab, spawnPosition, spawnRotation, player);
        if (networkPlayer == null)
        {
            Debug.LogError("NetworkPlayerSpawner: runner.Spawn returned null. Check that PlayerPrefab is a NetworkObject and has been baked.");
            return;
        }

        if (networkPlayer.transform.parent != null)
        {
            networkPlayer.transform.SetParent(null, true);
        }

        if (player == runner.LocalPlayer)
        {
            _hasSpawnedLocalPlayer = true;
            _localPlayerObject = networkPlayer;
        }

        runner.SetPlayerObject(player, networkPlayer);
        _spawnedPlayers.Add(networkPlayer);
        RegisterPlayer(networkPlayer);
        _spawnInProgress = false;
        Debug.Log("Spawned player " + player + " at " + spawnPosition);
    }

    private NetworkObject ResolvePlayerPrefab()
    {
        if (LobbyCarry.TryGetPrefab(out NetworkObject fromLobby))
        {
            return fromLobby;
        }

        if (LobbyCarry.HasChoice && characterPrefabs != null && LobbyCarry.CharacterIndex >= 0 && LobbyCarry.CharacterIndex < characterPrefabs.Length && characterPrefabs[LobbyCarry.CharacterIndex] != null)
        {
            return characterPrefabs[LobbyCarry.CharacterIndex];
        }

        if (LobbyCarry.HasChoice && _loggedMissingCharacter == false)
        {
            _loggedMissingCharacter = true;
            Debug.LogWarning("NetworkPlayerSpawner: Survivor " + (LobbyCarry.CharacterIndex + 1) + " has no prefab. Assign it on LobbyManager or Character Prefabs here. Using Player Prefab.");
        }

        return playerPrefab;
    }

    private bool HasLocalPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (_localPlayerObject != null && runner != null && player == runner.LocalPlayer)
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
        _playersWaitingToSpawn.Clear();
        AllPlayers.Clear();
        _localPlayerObject = null;
        _hasSpawnedLocalPlayer = false;
        _spawnInProgress = false;
        _framesRunnerReady = 0;
        _refusedScenePrefab = false;
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

        if (runner == null || runner.IsRunning == false || runner.IsStarting)
        {
            return;
        }

        QueuePlayerSpawn(runner, runner.LocalPlayer);
    }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
}