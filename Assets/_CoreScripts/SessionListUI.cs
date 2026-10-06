using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct SessionRow
{
    public string RoomName;
    public string InfoLabel;
    public int PlayerCount;
    public int MaxPlayers;
    public bool IsPrivate;
    public bool IsFull;
}

/// <summary>
/// Builds JoinPanel rows from the Fusion session list.
/// Attach this to JoinPanel and assign Assets/RoomItem.prefab.
/// </summary>
public class SessionListUI : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject roomItemPrefab;
    [SerializeField] private NetworkMenuManager menu;

    private bool _templateIsSceneObject;

    public void Initialize()
    {
        Resolve();
    }

    public void ShowRooms(IReadOnlyList<SessionRow> rooms)
    {
        Resolve();
        if (content == null || roomItemPrefab == null)
        {
            Debug.LogWarning("SessionListUI: assign Content and the RoomItem prefab.");
            return;
        }

        ClearRows();
        if (rooms == null)
        {
            return;
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            CreateRow(rooms[i]);
        }
    }

    private void Resolve()
    {
        if (menu == null)
        {
            menu = FindFirstObjectByType<NetworkMenuManager>();
        }

        if (content == null)
        {
            Transform found = FindDeep(transform, "Content");
            if (found != null)
            {
                content = found as RectTransform;
            }
        }

        if (roomItemPrefab == null)
        {
            Transform template = content != null ? FindDeep(content, "RoomItem") : null;
            if (template == null)
            {
                template = FindDeep(transform, "RoomItem");
            }

            if (template != null)
            {
                roomItemPrefab = template.gameObject;
            }
        }

        if (roomItemPrefab != null && roomItemPrefab.scene.IsValid())
        {
            _templateIsSceneObject = true;
            roomItemPrefab.SetActive(false);
        }
    }

    private void ClearRows()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform child = content.GetChild(i);
            if (_templateIsSceneObject && child.gameObject == roomItemPrefab)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    private void CreateRow(SessionRow room)
    {
        GameObject row = Instantiate(roomItemPrefab, content, false);
        row.SetActive(true);
        row.name = string.IsNullOrEmpty(room.RoomName) ? "RoomItem" : room.RoomName;

        Transform root = row.transform;
        SetActive(FindDeep(root, "LockIcon"), room.IsPrivate);
        SetText(FindDeep(root, "NameText"), room.RoomName);
        SetText(FindDeep(root, "InfoText"), room.InfoLabel);

        string count = room.MaxPlayers > 0
            ? room.PlayerCount + "/" + room.MaxPlayers
            : room.PlayerCount.ToString();
        SetText(FindDeep(root, "CountText"), count);

        Transform join = FindDeep(root, "JoinButton");
        if (join == null)
        {
            return;
        }

        Button joinButton = join.GetComponent<Button>();
        TMP_Text joinLabel = join.GetComponentInChildren<TMP_Text>(true);
        if (joinLabel != null)
        {
            joinLabel.text = room.IsFull ? "FULL" : "JOIN";
        }

        if (joinButton == null)
        {
            return;
        }

        joinButton.interactable = room.IsFull == false;
        string sessionName = room.RoomName;
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() =>
        {
            if (menu != null)
            {
                menu.JoinListedSession(sessionName);
            }
        });
    }

    private static void SetText(Transform target, string value)
    {
        if (target == null)
        {
            return;
        }

        TMP_Text label = target.GetComponent<TMP_Text>();
        if (label != null)
        {
            label.text = value;
        }
    }

    private static void SetActive(Transform target, bool active)
    {
        if (target != null)
        {
            target.gameObject.SetActive(active);
        }
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
