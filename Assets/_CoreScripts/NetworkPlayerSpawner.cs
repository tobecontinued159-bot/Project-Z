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

    private readonly List<NetworkObject> _spawnedPlayers = new List<NetworkObject>();
    private NetworkRunner _runner;
    private bool _hasSpawnedLocalPlayer;

    public void SetPlayerPrefab(NetworkObject prefab)
    {
        playerPrefab = prefab;
    }

    private async void Start()
    {
        _runner = GetComponent<NetworkRunner>();
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
        }

        if (_runner.IsRunning)
        {
            return;
        }

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

        if (_runner.LocalPlayer.IsRealPlayer)
        {
            SpawnLocalPlayer(_runner, _runner.LocalPlayer);
        }
    }

    private void OnDestroy()
    {
        if (_runner != null)
        {
            _runner.RemoveCallbacks(this);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer)
        {
            SpawnLocalPlayer(runner, player);
        }
    }

    private void SpawnLocalPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (_hasSpawnedLocalPlayer)
        {
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("NetworkPlayerSpawner: playerPrefab is not assigned.");
            return;
        }

        NetworkObject networkPlayer = runner.Spawn(playerPrefab, new Vector3(0f, 2f, 0f), Quaternion.identity, player);
        if (networkPlayer == null)
        {
            Debug.LogError("NetworkPlayerSpawner: runner.Spawn returned null. Check that PlayerPrefab is a NetworkObject and has been baked.");
            return;
        }

        _hasSpawnedLocalPlayer = true;
        _spawnedPlayers.Add(networkPlayer);
        Debug.Log($"Spawned local player for {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        for (int i = _spawnedPlayers.Count - 1; i >= 0; i--)
        {
            NetworkObject networkObject = _spawnedPlayers[i];
            if (networkObject == null)
            {
                _spawnedPlayers.RemoveAt(i);
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
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _spawnedPlayers.Clear();
        _hasSpawnedLocalPlayer = false;
        Debug.Log($"Fusion shutdown: {shutdownReason}");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

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

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }
}
