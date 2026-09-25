using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text pointsText;

    [Header("Optional UI References")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text respawnText;
    [SerializeField] private TMP_Text ammoText;

    private PlayerStats _cachedPlayerStats;
    private PlayerPoints _cachedPlayerPoints;
    private PlayerWeapon _cachedPlayerWeapon;

    private int _lastHealth = -1;
    private int _lastPoints = -1;
    private int _lastKills = -1;
    private int _lastAmmo = -1;
    private int _lastReserveAmmo = -1;
    private bool _lastIsDead = false;
    private float _lastRespawnSeconds = -1f;

    public override void Spawned()
    {
        // ป้องกันไม่ให้ Player ตัวอื่นที่ไม่ใช่เครื่องเรายุ่งกับ UI
        if (IsLocalPlayer == false)
        {
            enabled = false;
            return;
        }

        TryCachePlayerStats();
        TryCachePlayerPoints();
        TryCachePlayerWeapon();

        // เคลียร์ UI เก่าที่ลอยอยู่ใน Scene ทิ้งทั้งหมด
        CleanupLegacyUI();

        // สร้างและผูกโครงสร้าง HUD Canvas ใหม่ที่สะอาดบริสุทธิ์
        SetupHUDCanvas();

        RefreshHealthUI();
        RefreshUI(force: true);
    }

    public override void Render()
    {
        if (Object == null || Object.IsValid == false || IsLocalPlayer == false)
        {
            return;
        }

        if (_cachedPlayerStats == null) TryCachePlayerStats();
        if (_cachedPlayerPoints == null) TryCachePlayerPoints();
        if (_cachedPlayerWeapon == null) TryCachePlayerWeapon();

        if (_cachedPlayerStats == null) return;

        RefreshHealthUI();
        RefreshUI(force: false);
    }

    private void LateUpdate()
    {
        if (Object == null || Object.IsValid == false || IsLocalPlayer == false)
        {
            return;
        }

        if (_cachedPlayerStats == null) TryCachePlayerStats();
        if (_cachedPlayerPoints == null) TryCachePlayerPoints();
        if (_cachedPlayerWeapon == null) TryCachePlayerWeapon();

        if (_cachedPlayerStats == null) return;

        if (HasChanged())
        {
            RefreshUI(force: false);
        }
    }

    private bool IsLocalPlayer
    {
        get
        {
            return Object != null && Object.IsValid && HasInputAuthority;
        }
    }

    private void TryCachePlayerStats()
    {
        if (_cachedPlayerStats == null) _cachedPlayerStats = GetComponent<PlayerStats>();
    }

    private void TryCachePlayerPoints()
    {
        if (_cachedPlayerPoints == null) _cachedPlayerPoints = GetComponent<PlayerPoints>();
    }

    private void TryCachePlayerWeapon()
    {
        if (_cachedPlayerWeapon == null) _cachedPlayerWeapon = GetComponent<PlayerWeapon>();
    }

    private bool HasChanged()
    {
        if (_cachedPlayerStats == null) return false;

        bool changed = false;

        if (_cachedPlayerStats.Health != _lastHealth) changed = true;
        if (GetCurrentPoints() != _lastPoints) changed = true;
        if (_cachedPlayerStats.Kills != _lastKills) changed = true;
        if (_cachedPlayerStats.IsDead != _lastIsDead) changed = true;

        if (_cachedPlayerWeapon != null)
        {
            if (_cachedPlayerWeapon.CurrentAmmo != _lastAmmo || _cachedPlayerWeapon.ReserveAmmo != _lastReserveAmmo)
            {
                changed = true;
            }
        }

        float currentRespawn = _cachedPlayerStats.RemainingRespawnSeconds;
        if (_cachedPlayerStats.IsDead && Mathf.Abs(currentRespawn - _lastRespawnSeconds) > 0.05f)
        {
            changed = true;
        }

        return changed;
    }

    private void CleanupLegacyUI()
    {
        TMP_Text[] allTexts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (TMP_Text txt in allTexts)
        {
            if (txt == null) continue;

            if (txt.name.Contains("Health") && (txt.transform.parent == null || txt.transform.parent.name != "HUD_Canvas"))
            {
                Destroy(txt.gameObject);
            }
        }
    }

    private void SetupHUDCanvas()
    {
        if (IsLocalPlayer == false) return;

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

        Transform root = hudCanvasGo.transform;

        // 1. Health Text (มุมซ้ายล่าง)
        healthText = GetOrCreateTMPText(
            root,
            "HealthText",
            new Vector2(0, 0),
            new Vector2(0, 0),
            new Vector2(0, 0),
            new Vector2(20, 60),
            new Vector2(400, 50),
            36,
            new Color(1f, 0.3f, 0.3f, 1f),
            TextAlignmentOptions.BottomLeft
        );

        // 2. Health Fill Bar (อยู่ใต้ Health Text ที่มุมซ้ายล่าง)
        SetupHealthBar(root);

        // 3. Points Text (มุมขวาบน)
        pointsText = GetOrCreateTMPText(
            root,
            "PointsText",
            new Vector2(1, 1),
            new Vector2(1, 1),
            new Vector2(1, 1),
            new Vector2(-20, -20),
            new Vector2(400, 50),
            36,
            new Color(1f, 0.9f, 0.3f),
            TextAlignmentOptions.TopRight
        );

        // 4. Kills Text (มุมขวาบน ถัดลงมาจาก Points)
        killsText = GetOrCreateTMPText(
            root,
            "KillsText",
            new Vector2(1, 1),
            new Vector2(1, 1),
            new Vector2(1, 1),
            new Vector2(-20, -75),
            new Vector2(400, 40),
            24,
            new Color(0.9f, 0.5f, 0.2f),
            TextAlignmentOptions.TopRight
        );

        // 5. Ammo Text (มุมขวาล่าง)
        ammoText = GetOrCreateTMPText(
            root,
            "AmmoText",
            new Vector2(1, 0),
            new Vector2(1, 0),
            new Vector2(1, 0),
            new Vector2(-20, 20),
            new Vector2(400, 50),
            36,
            Color.white,
            TextAlignmentOptions.BottomRight
        );

        // 6. Respawn Text (กลางจอ)
        respawnText = GetOrCreateTMPText(
            root,
            "RespawnText",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(700, 120),
            56,
            new Color(1f, 0.25f, 0.25f),
            TextAlignmentOptions.Center
        );
        respawnText.enabled = false;
    }

    private TMP_Text GetOrCreateTMPText(Transform root, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        Transform existing = root.Find(objectName);
        TMP_Text tmpText = null;

        if (existing != null)
        {
            tmpText = existing.GetComponent<TMP_Text>();
        }

        if (tmpText == null)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(root, false);
            tmpText = go.AddComponent<TextMeshProUGUI>();
        }

        RectTransform rt = tmpText.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        tmpText.fontSize = fontSize;
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.color = color;
        tmpText.alignment = alignment;

        return tmpText;
    }

    private void SetupHealthBar(Transform root)
    {
        Transform fillTransform = root.Find("HealthFill");
        if (fillTransform == null)
        {
            GameObject fillGo = new GameObject("HealthFill", typeof(RectTransform));
            fillGo.transform.SetParent(root, false);
            fillTransform = fillGo.transform;
        }

        RectTransform rt = fillTransform.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(20, 25);
        rt.sizeDelta = new Vector2(350, 20);

        healthFillImage = fillTransform.GetComponent<Image>();
        if (healthFillImage == null)
        {
            healthFillImage = fillTransform.gameObject.AddComponent<Image>();
        }

        healthFillImage.color = new Color(1f, 0.25f, 0.25f, 1f);
        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Horizontal;
        healthFillImage.fillAmount = 1f;
    }

    public void RefreshHealthUI()
    {
        if (IsLocalPlayer == false) return;
        if (_cachedPlayerStats == null) TryCachePlayerStats();
        if (_cachedPlayerStats == null) return;

        int currentHealth = _cachedPlayerStats.Health;
        bool isDead = _cachedPlayerStats.IsDead;

        if (healthText != null)
        {
            healthText.text = isDead ? "DEAD" : $"Health: {currentHealth}";
        }

        if (healthFillImage != null)
        {
            float healthPct = isDead ? 0f : Mathf.Clamp01((float)currentHealth / _cachedPlayerStats.MaxHealth);
            healthFillImage.fillAmount = healthPct;
        }

        _lastHealth = currentHealth;
        _lastIsDead = isDead;
    }

    private int GetCurrentPoints()
    {
        if (_cachedPlayerPoints != null) return _cachedPlayerPoints.TotalPoints;
        return _cachedPlayerStats != null ? _cachedPlayerStats.Points : 0;
    }

    private void RefreshUI(bool force)
    {
        if (IsLocalPlayer == false) return;
        if (_cachedPlayerStats == null) return;

        RefreshHealthUI();

        if (respawnText != null)
        {
            if (_cachedPlayerStats.IsDead)
            {
                respawnText.enabled = true;
                float remain = Mathf.Ceil(_cachedPlayerStats.RemainingRespawnSeconds);
                if (remain < 0.5f) remain = 0f;
                respawnText.text = $"RESPAWNING IN {remain:0}...";
                _lastRespawnSeconds = remain;
            }
            else
            {
                respawnText.enabled = false;
                _lastRespawnSeconds = -1f;
            }
        }

        if (pointsText != null)
        {
            int currentPoints = GetCurrentPoints();
            pointsText.text = $"Points: {currentPoints}";
            _lastPoints = currentPoints;
        }

        if (killsText != null)
        {
            if (force || _cachedPlayerStats.Kills != _lastKills)
            {
                killsText.text = $"Kills: {_cachedPlayerStats.Kills}";
                _lastKills = _cachedPlayerStats.Kills;
            }
        }

        // 🟢 แสดงผลกระสุน: แม็กกาซีน / กระสุนสำรองใน Stock
        if (ammoText != null)
        {
            if (_cachedPlayerWeapon != null)
            {
                if (_cachedPlayerWeapon.IsReloading)
                {
                    ammoText.text = "RELOADING...";
                }
                else
                {
                    ammoText.text = $"Ammo: {_cachedPlayerWeapon.CurrentAmmo} / {_cachedPlayerWeapon.ReserveAmmo}";
                }
                _lastAmmo = _cachedPlayerWeapon.CurrentAmmo;
                _lastReserveAmmo = _cachedPlayerWeapon.ReserveAmmo;
            }
            else
            {
                ammoText.text = "Ammo: -- / --";
            }
        }

        _lastIsDead = _cachedPlayerStats.IsDead;
    }
}