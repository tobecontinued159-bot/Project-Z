using UnityEngine;

public class PlayerNetworkData : MonoBehaviour
{
    private PlayerStats _stats;
    private PlayerPoints _playerPoints;

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

    public PlayerPoints PlayerPoints
    {
        get
        {
            if (_playerPoints == null)
            {
                _playerPoints = GetComponent<PlayerPoints>();
            }

            return _playerPoints;
        }
    }

    public int Points
    {
        get
        {
            if (PlayerPoints != null)
            {
                return PlayerPoints.TotalPoints;
            }

            return Stats != null ? Stats.Points : 0;
        }
        set
        {
            if (PlayerPoints != null && PlayerPoints.HasStateAuthority)
            {
                PlayerPoints.TotalPoints = Mathf.Max(0, value);
                return;
            }

            if (Stats != null && Stats.HasStateAuthority)
            {
                Stats.Points = value;
            }
        }
    }

    public bool TrySpendPoints(int amount)
    {
        if (PlayerPoints != null)
        {
            return PlayerPoints.TrySpendPoints(amount);
        }

        return Stats != null && Stats.TrySpendPoints(amount);
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
