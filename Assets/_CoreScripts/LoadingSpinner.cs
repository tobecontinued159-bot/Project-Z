using TMPro;
using UnityEngine;

/// <summary>
/// Spins the existing LoadingPanel spinner and updates its two labels.
/// Attach this to the Spinner object. It does not create any UI.
/// </summary>
public class LoadingSpinner : MonoBehaviour
{
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private RectTransform spinner;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private TMP_Text loadingSubText;
    [SerializeField] private float degreesPerSecond = 240f;

    private bool _resolved;

    private void Awake()
    {
        Resolve();
    }

    private void Update()
    {
        if (spinner == null)
        {
            return;
        }

        spinner.Rotate(0f, 0f, -degreesPerSecond * Time.unscaledDeltaTime);
    }

    public void Show(string title, string subtitle)
    {
        Resolve();
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            loadingPanel.transform.SetAsLastSibling();
        }

        if (spinner != null)
        {
            spinner.gameObject.SetActive(true);
        }

        SetMessages(title, subtitle);
    }

    public void Hide()
    {
        Resolve();
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    public void SetMessages(string title, string subtitle)
    {
        Resolve();
        if (loadingText != null)
        {
            loadingText.text = title;
        }

        if (loadingSubText != null)
        {
            loadingSubText.text = subtitle;
        }
    }

    private void Resolve()
    {
        if (_resolved)
        {
            return;
        }

        if (spinner == null)
        {
            if (gameObject.name == "Spinner")
            {
                spinner = transform as RectTransform;
            }
            else
            {
                Transform foundSpinner = FindDeep(transform, "Spinner");
                if (foundSpinner != null)
                {
                    spinner = foundSpinner as RectTransform;
                }
            }
        }

        if (loadingPanel == null)
        {
            Transform panel = transform;
            while (panel != null && panel.name != "LoadingPanel")
            {
                panel = panel.parent;
            }

            if (panel != null)
            {
                loadingPanel = panel.gameObject;
            }
        }

        Transform searchRoot = loadingPanel != null ? loadingPanel.transform : transform;
        if (loadingText == null)
        {
            loadingText = FindText(searchRoot, "LoadingText");
        }

        if (loadingSubText == null)
        {
            loadingSubText = FindText(searchRoot, "LoadingSubText");
        }

        _resolved = loadingPanel != null && spinner != null;
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
}
