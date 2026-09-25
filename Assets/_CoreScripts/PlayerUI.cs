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
    private bool _lastIsDead = false;
    private float _lastRespawnSeconds = -1f;
    private const string HealthFillObjectName = "Image";

    public override void Spawned()
    {
        // [จุดแก้สำคัญที่ 1]: ถ้าไม่ใช่ Local Player ให้ปิดสคริปต์ PlayerUI ของตัวละครตัวนี้ทิ้งไปเลย
        // เพื่อป้องกันไม่ให้ตัวละครคนอื่นมาดึง HUD_Canvas บนหน้าจอเราไปควบคุม
        if (IsLocalPlayer == false)
        {
            enabled = false;
            return;
        }

        TryCachePlayerStats();
        TryCachePlayerPoints();
        TryCachePlayerWeapon();

        TryBindHealthFillImage();
        CreateDefaultUIIfMissing();
        BindOverlayHealthHud();

        ForceSetupPointsUI();

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

        if (_cachedPlayerStats == null)
        {
            return;
        }

        RefreshHealthUI();
        RefreshUI(force: false);
    }

    private void ForceSetupPointsUI()
    {
        if (IsLocalPlayer == false) return; // [เพิ่มการป้องกัน]

        if (pointsText == null)
        {
            Debug.LogError("[PlayerUI] ForceSetupPointsUI: pointsText is NULL!");
            return;
        }

        GameObject hudCanvas = GameObject.Find("HUD_Canvas");

        if (hudCanvas == null)
        {
            Debug.LogError("[PlayerUI] ForceSetupPointsUI: HUD_Canvas NOT FOUND!");
            return;
        }

        pointsText.transform.SetParent(hudCanvas.transform, false);

        RectTransform rt = pointsText.rectTransform;

        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);

        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(400f, 60f);

        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        pointsText.gameObject.SetActive(true);
        pointsText.enabled = true;

        pointsText.fontSize = 36f;
        pointsText.fontStyle = FontStyles.Bold;

        pointsText.color = Color.white;
        pointsText.alpha = 1f;

        pointsText.alignment = TextAlignmentOptions.TopRight;

        pointsText.text = $"Points: {GetCurrentPoints()}";

        pointsText.transform.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
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

        if (_cachedPlayerStats == null)
        {
            return;
        }

        if (HasChanged())
        {
            RefreshUI(force: false);
        }
    }

    private bool HasChanged()
    {
        if (_cachedPlayerStats == null)
        {
            return false;
        }

        bool changed = false;

        if (_cachedPlayerStats.Health != _lastHealth) changed = true;
        if (GetCurrentPoints() != _lastPoints) changed = true;
        if (_cachedPlayerStats.Kills != _lastKills) changed = true;
        if (_cachedPlayerStats.IsDead != _lastIsDead) changed = true;

        if (_cachedPlayerWeapon != null && _cachedPlayerWeapon.CurrentAmmo != _lastAmmo)
        {
            changed = true;
        }

        float currentRespawn = _cachedPlayerStats.RemainingRespawnSeconds;
        if (_cachedPlayerStats.IsDead && Mathf.Abs(currentRespawn - _lastRespawnSeconds) > 0.05f)
        {
            changed = true;
        }

        return changed;
    }

    private void TryCachePlayerStats()
    {
        if (_cachedPlayerStats == null)
        {
            _cachedPlayerStats = GetComponent<PlayerStats>();
        }
    }

    private void TryCachePlayerPoints()
    {
        if (_cachedPlayerPoints == null)
        {
            _cachedPlayerPoints = GetComponent<PlayerPoints>();
        }
    }

    private void TryCachePlayerWeapon()
    {
        if (_cachedPlayerWeapon == null)
        {
            _cachedPlayerWeapon = GetComponent<PlayerWeapon>();
        }
    }

    private bool IsLocalPlayer
    {
        get
        {
            // [จุดแก้สำคัญที่ 2]: ใน Photon Fusion โหมด Shared/Host ตัวละครของเครื่องเราเองจะมี HasInputAuthority = true
            return Object != null && Object.IsValid && HasInputAuthority;
        }
    }

    private void TryBindHealthFillImage()
    {
        if (IsLocalPlayer == false) return;

        GameObject healthFillObject = GameObject.Find(HealthFillObjectName);
        if (healthFillObject == null) healthFillObject = GameObject.Find("HealthFill");
        if (healthFillObject == null) healthFillObject = GameObject.Find("Fill");

        if (healthFillObject == null) return;

        Image foundFill = healthFillObject.GetComponent<UnityEngine.UI.Image>();
        if (foundFill == null) return;

        healthFillImage = foundFill;
        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Horizontal;
    }

    public void RefreshHealthUI()
    {
        if (IsLocalPlayer == false) return;

        if (_cachedPlayerStats == null) TryCachePlayerStats();
        if (_cachedPlayerStats == null) return;

        if (healthFillImage == null) TryBindHealthFillImage();

        BindOverlayHealthHud();

        int currentHealth = _cachedPlayerStats.Health;
        bool isDead = _cachedPlayerStats.IsDead;

        if (healthText != null)
        {
            healthText.text = isDead ? "DEAD" : $"Health: {currentHealth}";
        }

        if (healthFillImage != null)
        {
            healthFillImage.type = Image.Type.Filled;
            healthFillImage.fillMethod = Image.FillMethod.Horizontal;
            float healthPct = isDead ? 0f : Mathf.Clamp01((float)currentHealth / _cachedPlayerStats.MaxHealth);
            healthFillImage.fillAmount = healthPct;
        }

        _lastHealth = currentHealth;
        _lastIsDead = isDead;
    }

    private void BindOverlayHealthHud()
    {
        if (IsLocalPlayer == false) return; // [เพิ่มการป้องกัน]

        GameObject hudCanvasGo = GameObject.Find("HUD_Canvas");
        if (hudCanvasGo == null)
        {
            if (healthText == null || healthFillImage == null)
            {
                CreateDefaultUIIfMissing();
            }
            return;
        }

        if (healthText == null || IsWorldSpace(healthText))
        {
            Transform overlayHealth = hudCanvasGo.transform.Find("HealthText");
            if (overlayHealth != null)
            {
                TMP_Text overlayText = overlayHealth.GetComponent<TMP_Text>();
                if (overlayText != null) healthText = overlayText;
            }
        }

        if (healthFillImage == null)
        {
            Transform overlayFill = hudCanvasGo.transform.Find("HealthFill");
            if (overlayFill != null)
            {
                Image overlayImage = overlayFill.GetComponent<Image>();
                if (overlayImage != null) healthFillImage = overlayImage;
            }
        }
    }

    private static bool IsWorldSpace(TMP_Text text)
    {
        if (text == null) return false;
        Canvas canvas = text.GetComponentInParent<Canvas>();
        return canvas != null && canvas.renderMode == RenderMode.WorldSpace;
    }

    private int GetCurrentPoints()
    {
        if (_cachedPlayerPoints != null) return _cachedPlayerPoints.TotalPoints;
        return _cachedPlayerStats != null ? _cachedPlayerStats.Points : 0;
    }

    private void CreateDefaultUIIfMissing()
    {
        if (IsLocalPlayer == false) return; // [เพิ่มการป้องกัน]

        if (healthText == null || pointsText == null || respawnText == null || killsText == null || healthFillImage == null || ammoText == null || IsWorldSpace(healthText))
        {
            FindOrCreateHUD();
        }
    }

    private void FindOrCreateHUD()
    {
        if (IsLocalPlayer == false) return; // [เพิ่มการป้องกัน]

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
        else
        {
            canvas = hudCanvasGo.GetComponent<Canvas>();
        }

        Transform root = hudCanvasGo.transform;

        // --- Health Text ---
        if (healthText == null || IsWorldSpace(healthText))
        {
            Transform existingHealth = root.Find("HealthText");
            if (existingHealth != null)
            {
                TMP_Text overlayText = existingHealth.GetComponent<TMP_Text>();
                if (overlayText != null) healthText = overlayText;
            }
            else
            {
                GameObject healthGo = new GameObject("HealthText", typeof(RectTransform));
                healthGo.transform.SetParent(root, false);
                RectTransform rt = healthGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(20, -20);
                rt.sizeDelta = new Vector2(400, 60);

                healthText = healthGo.AddComponent<TextMeshProUGUI>();
                healthText.fontSize = 36;
                healthText.fontStyle = FontStyles.Bold;
                healthText.color = new Color(1f, 0.3f, 0.3f, 1f);
                healthText.alignment = TextAlignmentOptions.TopLeft;
                healthText.text = "Health: 100";
            }
        }

        // --- Points Text ---
        if (pointsText == null || pointsText.gameObject.activeInHierarchy == false)
        {
            GameObject pointsGo = new GameObject("PointsText", typeof(RectTransform));
            pointsGo.transform.SetParent(root, false);
            RectTransform rt = pointsGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-20, -20);
            rt.sizeDelta = new Vector2(400, 60);

            pointsText = pointsGo.AddComponent<TextMeshProUGUI>();
            pointsText.fontSize = 36;
            pointsText.fontStyle = FontStyles.Bold;
            pointsText.color = new Color(1f, 0.9f, 0.3f, 1f);
            pointsText.alignment = TextAlignmentOptions.TopRight;
            pointsText.text = $"Points: {GetCurrentPoints()}";
        }

        // --- Kills Text ---
        if (killsText == null)
        {
            GameObject killsGo = new GameObject("KillsText", typeof(RectTransform));
            killsGo.transform.SetParent(root, false);
            RectTransform rt = killsGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-20, -80);
            rt.sizeDelta = new Vector2(400, 40);

            killsText = killsGo.AddComponent<TextMeshProUGUI>();
            killsText.fontSize = 24;
            killsText.fontStyle = FontStyles.Bold;
            killsText.color = new Color(0.9f, 0.5f, 0.2f, 1f);
            killsText.alignment = TextAlignmentOptions.TopRight;
            killsText.text = "Kills: 0";
        }

        // --- Ammo Text ---
        if (ammoText == null)
        {
            Transform existingAmmo = root.Find("AmmoText");
            if (existingAmmo != null)
            {
                ammoText = existingAmmo.GetComponent<TMP_Text>();
            }
            else
            {
                GameObject ammoGo = new GameObject("AmmoText", typeof(RectTransform));
                ammoGo.transform.SetParent(root, false);
                RectTransform rt = ammoGo.GetComponent<RectTransform>();

                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(1, 0);
                rt.anchoredPosition = new Vector2(-20, 20);
                rt.sizeDelta = new Vector2(400, 60);

                ammoText = ammoGo.AddComponent<TextMeshProUGUI>();
                ammoText.fontSize = 36;
                ammoText.fontStyle = FontStyles.Bold;
                ammoText.color = new Color(1f, 1f, 1f, 1f);
                ammoText.alignment = TextAlignmentOptions.BottomRight;
                ammoText.text = "Ammo: -- / --";
            }
        }

        // --- Respawn Text ---
        if (respawnText == null)
        {
            GameObject respawnGo = new GameObject("RespawnText", typeof(RectTransform));
            respawnGo.transform.SetParent(root, false);
            RectTransform rt = respawnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(700, 120);

            respawnText = respawnGo.AddComponent<TextMeshProUGUI>();
            respawnText.fontSize = 56;
            respawnText.fontStyle = FontStyles.Bold;
            respawnText.color = new Color(1f, 0.25f, 0.25f, 1f);
            respawnText.alignment = TextAlignmentOptions.Center;
            respawnText.text = "";
            respawnText.enabled = false;
        }

        // --- Health Fill ---
        if (healthFillImage == null)
        {
            GameObject fillGo = new GameObject("HealthFill", typeof(RectTransform));
            fillGo.transform.SetParent(root, false);
            RectTransform rt = fillGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(20, -85);
            rt.sizeDelta = new Vector2(350, 18);

            healthFillImage = fillGo.AddComponent<Image>();
            healthFillImage.color = new Color(1f, 0.25f, 0.25f, 1f);
            healthFillImage.type = Image.Type.Filled;
            healthFillImage.fillMethod = Image.FillMethod.Horizontal;
            healthFillImage.fillAmount = 1f;

            GameObject borderGo = new GameObject("HealthBorder", typeof(RectTransform));
            borderGo.transform.SetParent(fillGo.transform.parent, true);
            borderGo.transform.SetAsFirstSibling();
            RectTransform brt = borderGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 1);
            brt.anchorMax = new Vector2(0, 1);
            brt.pivot = new Vector2(0, 1);
            brt.anchoredPosition = new Vector2(17, -82);
            brt.sizeDelta = new Vector2(356, 24);

            Image borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        }
    }

    private void RefreshUI(bool force)
    {
        if (IsLocalPlayer == false) return; // [เพิ่มการป้องกัน]
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
            pointsText.gameObject.SetActive(true);
            pointsText.enabled = true;
            pointsText.alpha = 1f;
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
                    ammoText.text = $"Ammo: {_cachedPlayerWeapon.CurrentAmmo} / {_cachedPlayerWeapon.MaxAmmo}";
                }
                _lastAmmo = _cachedPlayerWeapon.CurrentAmmo;
            }
            else
            {
                ammoText.text = "Ammo: -- / --";
            }
        }

        _lastIsDead = _cachedPlayerStats.IsDead;
    }
}