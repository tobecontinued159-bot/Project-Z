using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WallBuyInteract : NetworkBehaviour
{
    [Header("Costs")]
    [SerializeField] private int weaponCost = 1500;
    [SerializeField] private int ammoCost = 500;

    [Header("Weapon")]
    [SerializeField] private int weaponID = 1;
    [SerializeField] private PlayerWeapon weaponPrefab;
    [SerializeField] private string weaponDisplayName = "Wall Weapon";

    [Header("Fallback Stats (used if no prefab)")]
    [SerializeField] private int fallbackDamage = 40;
    [SerializeField] private float fallbackFireRate = 0.15f;
    [SerializeField] private int fallbackMaxAmmo = 30;
    [SerializeField] private int fallbackMaxReserveAmmo = 90;

    private bool _localPlayerInRange;
    private PlayerStats _localPlayerStats;
    private PlayerWeapon _localPlayerWeapon;
    private static TMP_Text _sharedPromptText;

    public override void Spawned()
    {
        EnsurePrompt();
    }

    private void Update()
    {
        if (IsNetworkReady() == false || PlayerInputLock.IsTerminalOpen)
        {
            return;
        }

        RefreshPrompt();

        if (_localPlayerInRange == false || _localPlayerStats == null || _localPlayerWeapon == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) == false)
        {
            return;
        }

        TryInteract();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsNetworkReady() == false)
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
        if (TryGetLocalPlayer(other, out _, out _) == false)
        {
            return;
        }

        ClearLocalPlayer();
        RefreshPrompt();
    }

    private void TryInteract()
    {
        if (_localPlayerStats.Object == null || _localPlayerStats.Object.IsValid == false)
        {
            return;
        }

        if (_localPlayerWeapon.Object == null || _localPlayerWeapon.Object.IsValid == false)
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

        int resolvedWeaponId = GetResolvedWeaponId();
        if (_localPlayerWeapon.HasWeapon(resolvedWeaponId))
        {
            TryBuyAmmo();
            return;
        }

        TryBuyWeapon(resolvedWeaponId);
    }

    private void TryBuyWeapon(int resolvedWeaponId)
    {
        if (_localPlayerStats.Points < weaponCost)
        {
            Debug.Log($"Not enough points to buy {GetWeaponName()}. Need {weaponCost}, have {_localPlayerStats.Points}.");
            return;
        }

        if (_localPlayerStats.TrySpendPoints(weaponCost) == false)
        {
            Debug.Log($"Not enough points to buy {GetWeaponName()}. Need {weaponCost}, have {_localPlayerStats.Points}.");
            return;
        }

        _localPlayerWeapon.EquipWeapon(
            resolvedWeaponId,
            GetResolvedDamage(),
            GetResolvedFireRate(),
            GetResolvedMaxAmmo(),
            GetResolvedMaxReserveAmmo());

        Debug.Log($"{_localPlayerStats.name} bought {GetWeaponName()} for {weaponCost}. Remaining: {_localPlayerStats.Points}");
        RefreshPrompt();
    }

    private void TryBuyAmmo()
    {
        if (_localPlayerWeapon.IsAmmoFull())
        {
            Debug.Log($"{GetWeaponName()} ammo is already full.");
            return;
        }

        if (_localPlayerStats.Points < ammoCost)
        {
            Debug.Log($"Not enough points to buy ammo. Need {ammoCost}, have {_localPlayerStats.Points}.");
            return;
        }

        if (_localPlayerStats.TrySpendPoints(ammoCost) == false)
        {
            Debug.Log($"Not enough points to buy ammo. Need {ammoCost}, have {_localPlayerStats.Points}.");
            return;
        }

        _localPlayerWeapon.RefillAmmo();
        Debug.Log($"{_localPlayerStats.name} bought ammo for {ammoCost}. Remaining: {_localPlayerStats.Points}");
        RefreshPrompt();
    }

    private bool TryGetLocalPlayer(Collider other, out PlayerStats stats, out PlayerWeapon weapon)
    {
        stats = null;
        weapon = null;

        if (other == null || other.CompareTag("Player") == false)
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

    private int GetResolvedWeaponId()
    {
        if (weaponID != 0)
        {
            return weaponID;
        }

        if (weaponPrefab != null)
        {
            return weaponPrefab.DefinitionWeaponId;
        }

        return 1;
    }

    private int GetResolvedDamage()
    {
        return weaponPrefab != null ? weaponPrefab.DefinitionDamage : fallbackDamage;
    }

    private float GetResolvedFireRate()
    {
        return weaponPrefab != null ? weaponPrefab.DefinitionFireRate : fallbackFireRate;
    }

    private int GetResolvedMaxAmmo()
    {
        return weaponPrefab != null ? weaponPrefab.DefinitionMaxAmmo : fallbackMaxAmmo;
    }

    private int GetResolvedMaxReserveAmmo()
    {
        return weaponPrefab != null ? weaponPrefab.DefinitionMaxReserveAmmo : fallbackMaxReserveAmmo;
    }

    private string GetWeaponName()
    {
        if (string.IsNullOrWhiteSpace(weaponDisplayName) == false)
        {
            return weaponDisplayName;
        }

        if (weaponPrefab != null)
        {
            return weaponPrefab.name;
        }

        return "Weapon";
    }

    private void RefreshPrompt()
    {
        EnsurePrompt();
        if (_sharedPromptText == null)
        {
            return;
        }

        if (IsNetworkReady() == false || _localPlayerInRange == false || _localPlayerWeapon == null)
        {
            if (_sharedPromptText.enabled)
            {
                _sharedPromptText.enabled = false;
            }

            return;
        }

        if (_localPlayerWeapon.HasWeapon(GetResolvedWeaponId()))
        {
            _sharedPromptText.text = $"Press E to Buy Ammo ({ammoCost} Pts)";
        }
        else
        {
            _sharedPromptText.text = $"Press E to Buy {GetWeaponName()} ({weaponCost} Pts)";
        }

        _sharedPromptText.enabled = true;
    }

    private static void EnsurePrompt()
    {
        if (_sharedPromptText != null)
        {
            return;
        }

        GameObject hudCanvasGo = GameObject.Find("HUD_Canvas");
        if (hudCanvasGo == null)
        {
            hudCanvasGo = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = hudCanvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = hudCanvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform existing = hudCanvasGo.transform.Find("WeaponBuyPrompt");
        if (existing != null)
        {
            _sharedPromptText = existing.GetComponent<TMP_Text>();
            if (_sharedPromptText != null)
            {
                return;
            }
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

    private bool IsNetworkReady()
    {
        return Object != null && Object.IsValid;
    }

    private void ClearLocalPlayer()
    {
        _localPlayerInRange = false;
        _localPlayerStats = null;
        _localPlayerWeapon = null;
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
