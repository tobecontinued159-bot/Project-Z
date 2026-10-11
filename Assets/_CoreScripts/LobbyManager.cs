using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Survives the lobby scene change. Each client spawns only their own avatar,
/// so the local character index and the Survivor prefabs are cached here
/// before Fusion loads the gameplay scene.
/// </summary>
public static class LobbyCarry
{
    public static bool HasChoice { get; private set; }
    public static int CharacterIndex { get; private set; } = -1;
    public static NetworkObject[] Prefabs { get; private set; }

    public static void RememberChoice(int characterIndex)
    {
        CharacterIndex = characterIndex;
        HasChoice = characterIndex >= 0;
    }

    public static void RememberPrefabs(NetworkObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            return;
        }

        if (Prefabs == null || Prefabs.Length != prefabs.Length)
        {
            Prefabs = new NetworkObject[prefabs.Length];
        }

        for (int i = 0; i < prefabs.Length; i++)
        {
            Prefabs[i] = prefabs[i];
        }
    }

    public static bool TryGetPrefab(out NetworkObject prefab)
    {
        prefab = null;
        if (HasChoice == false || Prefabs == null)
        {
            return false;
        }

        if (CharacterIndex < 0 || CharacterIndex >= Prefabs.Length)
        {
            return false;
        }

        prefab = Prefabs[CharacterIndex];
        return prefab != null;
    }

    public static void Clear()
    {
        HasChoice = false;
        CharacterIndex = -1;
        Prefabs = null;
    }
}

public struct LobbySlotData : INetworkStruct
{
    public PlayerRef Player;
    public NetworkBool Occupied;
    public NetworkBool Ready;
    public int Character;
    public NetworkString<_16> DisplayName;
}

/// <summary>
/// Synced lobby for the existing LobbyPanel. Spawned by NetworkMenuManager
/// after the Fusion session starts. Do not place this on the Canvas.
/// </summary>
public class LobbyManager : NetworkBehaviour
{
    public const int SlotCount = 4;
    public const int CharacterCount = 4;

    [Header("Characters")]
    [Tooltip("Element 0 is Survivor 1. Element 3 is Survivor 4. Each prefab needs a NetworkObject.")]
    [SerializeField] private NetworkObject[] characterPrefabs = new NetworkObject[CharacterCount];

    [Header("Game")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Networked, Capacity(SlotCount)]
    public NetworkArray<LobbySlotData> Slots => default;

    private readonly Dictionary<PlayerRef, string> _pendingNames = new Dictionary<PlayerRef, string>();
    private readonly SlotView[] _slots = new SlotView[SlotCount];
    private readonly CardView[] _cards = new CardView[CharacterCount];
    private readonly UnityAction[] _cardActions = new UnityAction[CharacterCount];

    private Button _readyButton;
    private Button _startButton;
    private Button _leaveButton;
    private Button _copyButton;
    private TMP_Text _readyLabel;
    private TMP_Text _roomNameText;
    private TMP_Text _codeText;
    private UnityAction _readyAction;
    private UnityAction _startAction;
    private UnityAction _leaveAction;
    private UnityAction _copyAction;
    private bool _listenersBound;
    private bool _warnedReadyButton;
    private bool _warnedCards;
    private bool _gameStartRequested;

    public override void Spawned()
    {
        LobbyCarry.RememberPrefabs(characterPrefabs);
        if (Object.HasStateAuthority)
        {
            SyncRoster();
        }

        RPC_SubmitProfile(NetworkMenuManager.SavedPlayerName);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        UnbindUi();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority == false)
        {
            return;
        }

        SyncRoster();
        ApplyPendingNames();
    }

    public override void Render()
    {
        LobbyCarry.RememberPrefabs(characterPrefabs);
        if (_listenersBound == false)
        {
            TryBindUi();
        }

        RememberLocalChoice();
        RefreshUi();
    }

    public void OnReadyPressed()
    {
        if (Runner == null || Runner.LocalPlayer.IsRealPlayer == false)
        {
            return;
        }

        RPC_SetReady(!IsLocalReady());
    }

    public void OnStartPressed()
    {
        if (Runner == null || Runner.IsSharedModeMasterClient == false || _gameStartRequested)
        {
            return;
        }

        if (CountOccupied() <= 0)
        {
            return;
        }

        if (SceneIsInBuild(gameSceneName) == false)
        {
            Debug.LogError("LobbyManager: add scene '" + gameSceneName + "' to File > Build Settings before starting.");
            return;
        }

        // Host เป็นคนสั่งโหลดฉาก คนที่ยังไม่ READY ก็เริ่มได้ เพราะ Host บังคับเริ่ม
        _gameStartRequested = true;
        RememberLocalChoice();
        Runner.LoadScene(gameSceneName, LoadSceneMode.Single, LocalPhysicsMode.None, true);
    }

    public void OnLeavePressed()
    {
        if (Runner != null && Runner.IsRunning)
        {
            Runner.Shutdown();
        }
    }

    public void OnCopyPressed()
    {
        string roomName = CurrentRoomName();
        if (string.IsNullOrEmpty(roomName))
        {
            return;
        }

        GUIUtility.systemCopyBuffer = roomName;
    }

    public void OnCardPressed(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= CharacterCount)
        {
            return;
        }

        RPC_SelectCharacter(characterIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_SubmitProfile(string playerName, RpcInfo info = default)
    {
        if (Object.HasStateAuthority == false)
        {
            return;
        }

        PlayerRef source = SourceOf(info);
        if (source.IsRealPlayer == false)
        {
            return;
        }

        _pendingNames[source] = SanitizeName(playerName);
        SyncRoster();
        ApplyPendingNames();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_SetReady(NetworkBool ready, RpcInfo info = default)
    {
        if (Object.HasStateAuthority == false)
        {
            return;
        }

        PlayerRef source = SourceOf(info);
        int index = FindSlot(source);
        if (index < 0)
        {
            SyncRoster();
            index = FindSlot(source);
        }

        if (index < 0)
        {
            return;
        }

        LobbySlotData slot = Slots[index];
        slot.Ready = ready;
        Slots.Set(index, slot);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_SelectCharacter(int characterIndex, RpcInfo info = default)
    {
        if (Object.HasStateAuthority == false)
        {
            return;
        }

        if (characterIndex < 0 || characterIndex >= CharacterCount)
        {
            return;
        }

        PlayerRef source = SourceOf(info);
        int index = FindSlot(source);
        if (index < 0)
        {
            SyncRoster();
            index = FindSlot(source);
        }

        if (index < 0)
        {
            return;
        }

        // การ์ดที่คนอื่นเลือกแล้วเป็น TAKEN คนเดิมเลือกใบเดิมซ้ำได้ และสลับไปใบว่างได้
        for (int i = 0; i < SlotCount; i++)
        {
            LobbySlotData other = Slots[i];
            if (other.Occupied && other.Character == characterIndex && other.Player != source)
            {
                return;
            }
        }

        LobbySlotData slot = Slots[index];
        slot.Character = characterIndex;
        Slots.Set(index, slot);
    }

    private void SyncRoster()
    {
        if (Runner == null)
        {
            return;
        }

        int activeCount = 0;
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (player.IsRealPlayer == false)
            {
                continue;
            }

            activeCount++;
            if (activeCount > SlotCount)
            {
                break;
            }

            if (FindSlot(player) >= 0)
            {
                continue;
            }

            int free = FindFreeSlot();
            if (free < 0)
            {
                break;
            }

            LobbySlotData created = default;
            created.Occupied = true;
            created.Player = player;
            created.Ready = false;
            created.Character = -1;
            created.DisplayName = "Player";
            if (_pendingNames.TryGetValue(player, out string pending))
            {
                created.DisplayName = pending;
                _pendingNames.Remove(player);
            }

            Slots.Set(free, created);
        }

        for (int i = 0; i < SlotCount; i++)
        {
            LobbySlotData slot = Slots[i];
            if (slot.Occupied == false)
            {
                continue;
            }

            if (IsActive(slot.Player))
            {
                continue;
            }

            _pendingNames.Remove(slot.Player);
            Slots.Set(i, default);
        }
    }

    private void ApplyPendingNames()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            LobbySlotData slot = Slots[i];
            if (slot.Occupied == false)
            {
                continue;
            }

            if (_pendingNames.TryGetValue(slot.Player, out string pending) == false)
            {
                continue;
            }

            slot.DisplayName = pending;
            Slots.Set(i, slot);
            _pendingNames.Remove(slot.Player);
        }
    }

    private void RememberLocalChoice()
    {
        if (Runner == null || Runner.LocalPlayer.IsRealPlayer == false)
        {
            return;
        }

        int index = FindSlot(Runner.LocalPlayer);
        if (index < 0)
        {
            return;
        }

        LobbySlotData slot = Slots[index];
        if (slot.Occupied == false)
        {
            return;
        }

        LobbyCarry.RememberChoice(slot.Character);
    }

    private bool TryBindUi()
    {
        GameObject lobby = FindSceneObject("LobbyPanel");
        if (lobby == null)
        {
            return false;
        }

        Transform root = lobby.transform;
        for (int i = 0; i < SlotCount; i++)
        {
            Transform slotRoot = FindDeep(root, "PlayerSlot" + (i + 1));
            SlotView view = new SlotView();
            if (slotRoot != null)
            {
                view.Root = slotRoot.gameObject;
                view.Name = FindText(slotRoot, "NameText");
                view.Status = FindText(slotRoot, "StatusText");
                Transform badge = FindDeep(slotRoot, "HostBadge");
                view.HostBadge = badge != null ? badge.gameObject : null;
                view.Button = slotRoot.GetComponent<Button>();
            }

            _slots[i] = view;
        }

        for (int i = 0; i < CharacterCount; i++)
        {
            Transform cardRoot = FindDeep(root, "CharacterCard" + (i + 1));
            CardView card = new CardView();
            if (cardRoot != null)
            {
                card.Button = cardRoot.GetComponent<Button>();
                if (card.Button == null)
                {
                    card.Button = cardRoot.GetComponentInChildren<Button>(true);
                }

                Transform selected = FindDeep(cardRoot, "SelectedBar");
                Transform taken = FindDeep(cardRoot, "TakenOverlay");
                Transform takenText = FindDeep(cardRoot, "TakenText");
                card.SelectedBar = selected != null ? selected.gameObject : null;
                card.TakenOverlay = taken != null ? taken.gameObject : null;
                card.TakenText = takenText != null ? takenText.gameObject : null;
            }

            _cards[i] = card;
        }

        _readyButton = FindButton(root, "ReadyButton");
        _startButton = FindButton(root, "StartButton");
        _leaveButton = FindButton(root, "LeaveButton");
        _copyButton = FindButton(root, "CopyButton");
        _roomNameText = FindText(root, "RoomNameText");
        _codeText = FindText(root, "CodeText");
        _readyLabel = _readyButton != null ? _readyButton.GetComponentInChildren<TMP_Text>(true) : null;

        _readyAction = OnReadyPressed;
        _startAction = OnStartPressed;
        _leaveAction = OnLeavePressed;
        _copyAction = OnCopyPressed;
        AddListener(_readyButton, _readyAction);
        AddListener(_startButton, _startAction);
        AddListener(_leaveButton, _leaveAction);
        AddListener(_copyButton, _copyAction);

        for (int i = 0; i < CharacterCount; i++)
        {
            int captured = i;
            _cardActions[i] = () => OnCardPressed(captured);
            AddListener(_cards[i].Button, _cardActions[i]);
        }

        if (_readyButton == null && _warnedReadyButton == false)
        {
            _warnedReadyButton = true;
            Debug.LogWarning("LobbyManager: LobbyPanel has no button named ReadyButton. Add one and leave its OnClick list empty.");
        }

        if (_warnedCards == false && _cards[0].Button == null)
        {
            _warnedCards = true;
            Debug.LogWarning("LobbyManager: CharacterCard1 has no Button. Put a Button on each CharacterCard and leave OnClick empty.");
        }

        _listenersBound = true;
        return true;
    }

    private void UnbindUi()
    {
        RemoveListener(_readyButton, _readyAction);
        RemoveListener(_startButton, _startAction);
        RemoveListener(_leaveButton, _leaveAction);
        RemoveListener(_copyButton, _copyAction);
        for (int i = 0; i < CharacterCount; i++)
        {
            if (_cards[i] != null)
            {
                RemoveListener(_cards[i].Button, _cardActions[i]);
            }
        }

        _listenersBound = false;
    }

    private void RefreshUi()
    {
        if (_listenersBound == false || Runner == null)
        {
            return;
        }

        string roomName = CurrentRoomName();
        if (_roomNameText != null)
        {
            _roomNameText.text = roomName;
        }

        if (_codeText != null)
        {
            _codeText.text = roomName;
        }

        PlayerRef host = Runner.GetMasterClient();
        bool isHost = Runner.IsSharedModeMasterClient;
        if (_startButton != null)
        {
            _startButton.interactable = isHost && _gameStartRequested == false;
        }

        if (_readyLabel != null)
        {
            _readyLabel.text = IsLocalReady() ? "READY" : "NOT READY";
        }

        int localCharacter = -1;
        int localSlot = FindSlot(Runner.LocalPlayer);
        if (localSlot >= 0 && Slots[localSlot].Occupied)
        {
            localCharacter = Slots[localSlot].Character;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            SlotView view = _slots[i];
            if (view == null)
            {
                continue;
            }

            LobbySlotData slot = Slots[i];
            bool occupied = slot.Occupied && slot.Player.IsRealPlayer;
            if (view.Name != null)
            {
                view.Name.text = occupied ? slot.DisplayName.Value : string.Empty;
            }

            if (view.Status != null)
            {
                if (occupied == false)
                {
                    view.Status.text = string.Empty;
                }
                else
                {
                    view.Status.text = slot.Ready ? "READY" : "NOT READY";
                }
            }

            if (view.HostBadge != null)
            {
                view.HostBadge.SetActive(occupied && slot.Player == host);
            }
        }

        for (int i = 0; i < CharacterCount; i++)
        {
            CardView card = _cards[i];
            if (card == null)
            {
                continue;
            }

            bool taken = IsCharacterTaken(i);
            bool mine = localCharacter == i;
            if (card.TakenOverlay != null)
            {
                card.TakenOverlay.SetActive(taken);
            }

            if (card.TakenText != null && card.TakenText != card.TakenOverlay)
            {
                card.TakenText.SetActive(taken);
            }

            if (card.SelectedBar != null)
            {
                card.SelectedBar.SetActive(mine);
            }

            if (card.Button != null)
            {
                card.Button.interactable = taken == false || mine;
            }
        }
    }

    private bool IsCharacterTaken(int characterIndex)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            LobbySlotData slot = Slots[i];
            if (slot.Occupied && slot.Character == characterIndex)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsLocalReady()
    {
        if (Runner == null)
        {
            return false;
        }

        int index = FindSlot(Runner.LocalPlayer);
        if (index < 0)
        {
            return false;
        }

        LobbySlotData slot = Slots[index];
        return slot.Occupied && slot.Ready;
    }

    private int CountOccupied()
    {
        int count = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            if (Slots[i].Occupied)
            {
                count++;
            }
        }

        return count;
    }

    private int FindSlot(PlayerRef player)
    {
        if (player.IsRealPlayer == false)
        {
            return -1;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            LobbySlotData slot = Slots[i];
            if (slot.Occupied && slot.Player == player)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindFreeSlot()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (Slots[i].Occupied == false)
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsActive(PlayerRef player)
    {
        foreach (PlayerRef active in Runner.ActivePlayers)
        {
            if (active == player)
            {
                return true;
            }
        }

        return false;
    }

    private PlayerRef SourceOf(RpcInfo info)
    {
        if (info.Source.IsRealPlayer)
        {
            return info.Source;
        }

        return Runner != null ? Runner.LocalPlayer : PlayerRef.None;
    }

    private string CurrentRoomName()
    {
        if (Runner == null || Runner.SessionInfo == null || Runner.SessionInfo.IsValid == false)
        {
            return string.Empty;
        }

        return Runner.SessionInfo.Name;
    }

    private static string SanitizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Player";
        }

        string trimmed = value.Trim();
        if (trimmed.Length > NetworkMenuManager.MaxNameLength)
        {
            trimmed = trimmed.Substring(0, NetworkMenuManager.MaxNameLength);
        }

        if (trimmed.Length < NetworkMenuManager.MinNameLength)
        {
            return "Player";
        }

        return trimmed;
    }

    private static bool SceneIsInBuild(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return false;
        }

        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
            {
                return true;
            }
        }

        return false;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            GameObject candidate = all[i];
            if (candidate.name != objectName)
            {
                continue;
            }

            if (candidate.scene.IsValid() == false || candidate.scene.isLoaded == false)
            {
                continue;
            }

            return candidate;
        }

        return null;
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

    private static TMP_Text FindText(Transform root, string childName)
    {
        Transform found = FindDeep(root, childName);
        if (found == null)
        {
            return null;
        }

        return found.GetComponent<TMP_Text>();
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

    private static void AddListener(Button button, UnityAction action)
    {
        if (button == null || action == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void RemoveListener(Button button, UnityAction action)
    {
        if (button == null || action == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
    }

    private sealed class SlotView
    {
        public GameObject Root;
        public TMP_Text Name;
        public TMP_Text Status;
        public GameObject HostBadge;
        public Button Button;
    }

    private sealed class CardView
    {
        public Button Button;
        public GameObject SelectedBar;
        public GameObject TakenOverlay;
        public GameObject TakenText;
    }
}
