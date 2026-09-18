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

    private PlayerStats _cachedPlayerStats;
    private PlayerPoints _cachedPlayerPoints;
    private int _lastHealth = -1;
    private int _lastPoints = -1;
    private int _lastKills = -1;
    private bool _lastIsDead = false;
    private float _lastRespawnSeconds = -1f;
    private const string HealthFillObjectName = "Image";

    public override void Spawned()
    {
        TryCachePlayerStats();
        TryCachePlayerPoints();

        if (IsLocalPlayer == false)
        {
            return;
        }

        TryBindHealthFillImage();
        CreateDefaultUIIfMissing();
        BindOverlayHealthHud();
        RefreshHealthUI();
        RefreshUI(force: true);
    }

    public override void Render()
    {
        if (Object == null || Object.IsValid == false || IsLocalPlayer == false)
        {
            return;
        }

        if (_cachedPlayerStats == null)
        {
            TryCachePlayerStats();
        }

        if (_cachedPlayerPoints == null)
        {
            TryCachePlayerPoints();
        }

        if (_cachedPlayerStats == null)
        {
            return;
        }

        RefreshHealthUI();
        RefreshUI(force: false);
    }

    private void LateUpdate()
    {
        if (Object == null || Object.IsValid == false || IsLocalPlayer == false)
        {
            return;
        }

        if (_cachedPlayerStats == null)
        {
            TryCachePlayerStats();
        }

        if (_cachedPlayerPoints == null)
        {
            TryCachePlayerPoints();
        }

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

        if (_cachedPlayerStats.Health != _lastHealth)
        {
            changed = true;
        }

        if (GetCurrentPoints() != _lastPoints)
        {
            changed = true;
        }

        if (_cachedPlayerStats.Kills != _lastKills)
        {
            changed = true;
        }

        if (_cachedPlayerStats.IsDead != _lastIsDead)
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

    private bool IsLocalPlayer
    {
        get
        {
            return Object != null && Object.IsValid && (HasInputAuthority || HasStateAuthority);
        }
    }

    private void TryBindHealthFillImage()
    {
        GameObject healthFillObject = GameObject.Find(HealthFillObjectName);
        if (healthFillObject == null)
        {
            healthFillObject = GameObject.Find("HealthFill");
        }

        if (healthFillObject == null)
        {
            healthFillObject = GameObject.Find("Fill");
        }

        if (healthFillObject == null)
        {
            Debug.LogWarning($"PlayerUI: Could not find health fill object '{HealthFillObjectName}'.");
            return;
        }

        Image foundFill = healthFillObject.GetComponent<UnityEngine.UI.Image>();
        if (foundFill == null)
        {
            Debug.LogWarning($"PlayerUI: '{healthFillObject.name}' has no UnityEngine.UI.Image component.");
            return;
        }

        healthFillImage = foundFill;
        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Horizontal;
    }

    public void RefreshHealthUI()
    {
        if (IsLocalPlayer == false)
        {
            return;
        }

        if (_cachedPlayerStats == null)
        {
            TryCachePlayerStats();
        }

        if (_cachedPlayerStats == null)
        {
            return;
        }

        if (healthFillImage == null)
        {
            TryBindHealthFillImage();
        }

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
                if (overlayText != null)
                {
                    healthText = overlayText;
                }
            }
        }

        if (healthFillImage == null)
        {
            Transform overlayFill = hudCanvasGo.transform.Find("HealthFill");
            if (overlayFill != null)
            {
                Image overlayImage = overlayFill.GetComponent<Image>();
                if (overlayImage != null)
                {
                    healthFillImage = overlayImage;
                }
            }
        }
    }

    private static bool IsWorldSpace(TMP_Text text)
    {
        if (text == null)
        {
            return false;
        }

        Canvas canvas = text.GetComponentInParent<Canvas>();
        return canvas != null && canvas.renderMode == RenderMode.WorldSpace;
    }

    private int GetCurrentPoints()
    {
        if (_cachedPlayerPoints != null)
        {
            return _cachedPlayerPoints.TotalPoints;
        }

        return _cachedPlayerStats != null ? _cachedPlayerStats.Points : 0;
    }

    private void CreateDefaultUIIfMissing()
    {
        if (healthText == null || pointsText == null || respawnText == null || killsText == null || healthFillImage == null || IsWorldSpace(healthText))
        {
            FindOrCreateHUD();
        }
    }

    private void FindOrCreateHUD()
    {
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

        if (healthText == null || IsWorldSpace(healthText))
        {
            Transform existingHealth = root.Find("HealthText");
            if (existingHealth != null)
            {
                TMP_Text overlayText = existingHealth.GetComponent<TMP_Text>();
                if (overlayText != null)
                {
                    healthText = overlayText;
                }
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

        if (pointsText == null)
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
            pointsText.text = "Points: 500";
        }

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
        if (_cachedPlayerStats == null)
        {
            return;
        }

        RefreshHealthUI();

        if (respawnText != null)
        {
            if (_cachedPlayerStats.IsDead)
            {
                respawnText.enabled = true;
                float remain = Mathf.Ceil(_cachedPlayerStats.RemainingRespawnSeconds);
                if (remain < 0.5f)
                {
                    remain = 0f;
                }
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
            if (force || currentPoints != _lastPoints)
            {
                pointsText.text = $"Points: {currentPoints}";
                _lastPoints = currentPoints;
            }
        }

        if (killsText != null)
        {
            if (force || _cachedPlayerStats.Kills != _lastKills)
            {
                killsText.text = $"Kills: {_cachedPlayerStats.Kills}";
                _lastKills = _cachedPlayerStats.Kills;
            }
        }

        _lastIsDead = _cachedPlayerStats.IsDead;
    }
}
