using UnityEngine;

public class PlayerNetworkData : MonoBehaviour
{
    private PlayerStats _stats;

    public PlayerStats Stats
    {
        get
        {
            if (_stats == null)
            {
                _stats = GetComponent<PlayerStats>();
            }

            return _stats;
        }
    }

    public int Points
    {
        get
        {
            return Stats != null ? Stats.Points : 0;
        }
        set
        {
            if (Stats != null && Stats.HasStateAuthority)
            {
                Stats.Points = value;
            }
        }
    }

    public bool HasStateAuthority
    {
        get
        {
            return Stats != null && Stats.HasStateAuthority;
        }
    }

    public bool HasInputAuthority
    {
        get
        {
            return Stats != null && Stats.HasInputAuthority;
        }
    }
}
