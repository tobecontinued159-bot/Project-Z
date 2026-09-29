using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AmmoBoxBuy : NetworkBehaviour
{
    [Header("Cost Settings")]
    [SerializeField] private int ammoCost = 125; // ราคาซื้อกระสุน 125 Points

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
        if (Object == null || Object.IsValid == false)
        {
            return;
        }

        RefreshPrompt();

        if (_localPlayerInRange == false)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) == false)
        {
            return;
        }

        TryPurchaseAmmo();
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

    private void TryPurchaseAmmo()
    {
        if (Object == null || Object.IsValid == false) return;
        if (_localPlayerStats == null || _localPlayerWeapon == null) return;
        if (_localPlayerStats.HasStateAuthority == false) return;
        if (_localPlayerStats.IsDead) return;

        // 1. เช็กว่า Stock กระสุนสำรองเต็มอยู่แล้วหรือไม่
        if (_localPlayerWeapon.ReserveAmmo >= _localPlayerWeapon.MaxReserveAmmo)
        {
            Debug.Log("Reserve Ammo stock is already full!");
            return;
        }

        // 2. ตรวจสอบคะแนน Points ว่าเพียงพอต่อราคา 125 หรือไม่
        if (_localPlayerStats.TrySpendPoints(ammoCost) == false)
        {
            Debug.Log($"Not enough points. Need {ammoCost}, have {_localPlayerStats.Points}.");
            return;
        }

        // 🟢 [สั่งเติมกระสุนสำรองเข้า Stock]:
        _localPlayerWeapon.BuyReserveAmmo();

        Debug.Log($"{_localPlayerStats.name} bought reserve ammo stock for {ammoCost}. Remaining Points: {_localPlayerStats.Points}");

        RefreshPrompt();
    }

    private bool TryGetLocalPlayer(Collider other, out PlayerStats stats, out PlayerWeapon weapon)
    {
        stats = null;
        weapon = null;

        if (Object == null || Object.IsValid == false)
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

        if (_localPlayerInRange)
        {
            _sharedPromptText.text = $"Press E to Buy Ammo ({ammoCost} Pts)";
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

        GameObject promptGo = new GameObject("AmmoBuyPrompt", typeof(RectTransform));
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