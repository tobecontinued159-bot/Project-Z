using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponWallBuy : NetworkBehaviour
{
    public int weaponCost = 150;

    [Networked] public NetworkBool IsPurchased { get; set; }

    private bool _localPlayerInRange;
    private PlayerStats _localPlayerStats;
    private PlayerWeapon _localPlayerWeapon;
    private static TMP_Text _sharedPromptText;

    public override void Spawned()
    {
        EnsurePrompt();
        ApplyPurchasedState();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || Object.IsValid == false)
        {
            return;
        }

        ApplyPurchasedState();
    }

    private void Update()
    {
        if (Object == null || Object.IsValid == false)
        {
            return;
        }

        RefreshPrompt();

        if (IsPurchased || _localPlayerInRange == false)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) == false)
        {
            return;
        }

        TryPurchase();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || Object.IsValid == false)
        {
            return;
        }

        if (TryGetLocalPlayer(other, out PlayerStats stats, out PlayerWeapon weapon) == false)
        {
            return;
        }

        _localPlayerInRange = true;
        _localPlayerStats = stats;
        _localPlayerWeapon = weapon;
        RefreshPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (Object == null || Object.IsValid == false)
        {
            return;
        }

        if (TryGetLocalPlayer(other, out _, out _) == false)
        {
            return;
        }

        ClearLocalPlayer();
        RefreshPrompt();
    }

    private void TryPurchase()
    {
        if (Object == null || Object.IsValid == false)
        {
            return;
        }

        if (_localPlayerStats == null || _localPlayerWeapon == null)
        {
            return;
        }

        if (_localPlayerStats.HasStateAuthority == false)
        {
            return;
        }

        if (_localPlayerStats.IsDead)
        {
            return;
        }

        if (_localPlayerStats.Points < weaponCost)
        {
            Debug.Log($"Not enough points. Need {weaponCost}, have {_localPlayerStats.Points}.");
            return;
        }

        _localPlayerStats.Points -= weaponCost;
        _localPlayerWeapon.UpgradeWeapon();
        Debug.Log($"{_localPlayerStats.name} bought weapon upgrade for {weaponCost}. Remaining: {_localPlayerStats.Points}");

        ClearLocalPlayer();
        RefreshPrompt();
        RPC_HideWeaponBox();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_HideWeaponBox()
    {
        if (HasStateAuthority == false || IsPurchased)
        {
            return;
        }

        IsPurchased = true;
        RPC_SetBoxVisible(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    private void RPC_SetBoxVisible(NetworkBool visible)
    {
        if (visible == false)
        {
            HideBox();
        }
    }

    private bool TryGetLocalPlayer(Collider other, out PlayerStats stats, out PlayerWeapon weapon)
    {
        stats = null;
        weapon = null;

        if (Object == null || Object.IsValid == false)
        {
            return false;
        }

        if (IsPurchased)
        {
            return false;
        }

        if (other.CompareTag("Player") == false)
        {
            return false;
        }

        stats = other.GetComponentInParent<PlayerStats>();
        weapon = other.GetComponentInParent<PlayerWeapon>();
        if (stats == null || weapon == null)
        {
            return false;
        }

        return stats.HasStateAuthority || stats.HasInputAuthority;
    }

    private void ApplyPurchasedState()
    {
        if (Object == null || Object.IsValid == false)
        {
            return;
        }

        if (IsPurchased)
        {
            HideBox();
        }
    }

    private void HideBox()
    {
        ClearLocalPlayer();
        RefreshPrompt();

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void ClearLocalPlayer()
    {
        _localPlayerInRange = false;
        _localPlayerStats = null;
        _localPlayerWeapon = null;
    }

    private void RefreshPrompt()
    {
        EnsurePrompt();
        if (_sharedPromptText == null)
        {
            return;
        }

        if (Object == null || Object.IsValid == false)
        {
            _sharedPromptText.enabled = false;
            return;
        }

        bool showPrompt = IsPurchased == false && _localPlayerInRange;
        if (showPrompt)
        {
            _sharedPromptText.text = $"Press E to Buy Weapon ({weaponCost} Pts)";
            _sharedPromptText.enabled = true;
            return;
        }

        if (_sharedPromptText.enabled)
        {
            _sharedPromptText.enabled = false;
        }
    }

    private static void EnsurePrompt()
    {
        if (_sharedPromptText != null)
        {
            return;
        }

        GameObject hudCanvasGo = GameObject.Find("HUD_Canvas");
        Canvas canvas;
        if (hudCanvasGo == null)
        {
            hudCanvasGo = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = hudCanvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = hudCanvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject promptGo = new GameObject("WeaponBuyPrompt", typeof(RectTransform));
        promptGo.transform.SetParent(hudCanvasGo.transform, false);

        RectTransform rt = promptGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 80f);
        rt.sizeDelta = new Vector2(900f, 70f);

        _sharedPromptText = promptGo.AddComponent<TextMeshProUGUI>();
        _sharedPromptText.fontSize = 36;
        _sharedPromptText.fontStyle = FontStyles.Bold;
        _sharedPromptText.color = Color.white;
        _sharedPromptText.alignment = TextAlignmentOptions.Center;
        _sharedPromptText.text = "";
        _sharedPromptText.enabled = false;
    }

    private void OnDisable()
    {
        ClearLocalPlayer();
        if (_sharedPromptText != null)
        {
            _sharedPromptText.enabled = false;
        }
    }
}
