using Fusion;
using TMPro;
using UnityEngine;

public class PlayerPoints : NetworkBehaviour
{
    private const int StartingPoints = 500;
    private const string PointsUiObjectName = "Text (TMP)";

    [Networked]
    public int TotalPoints { get; set; } = StartingPoints;

    private TextMeshProUGUI _pointsUIText;
    private int _lastDisplayedPoints = int.MinValue;

    public override void Spawned()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        TotalPoints = StartingPoints;

        GameObject pointsObject = GameObject.Find(PointsUiObjectName);
        if (pointsObject != null)
        {
            _pointsUIText = pointsObject.GetComponent<TextMeshProUGUI>();
        }

        if (_pointsUIText == null)
        {
            Debug.LogWarning($"PlayerPoints: Could not find UI object '{PointsUiObjectName}'.");
            return;
        }

        RefreshPointsUI(force: true);
    }

    public override void Render()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        RefreshPointsUI(force: false);
    }

    public void AddPoints(int amount)
    {
        if (Object == null || Object.IsValid == false || amount <= 0)
        {
            return;
        }

        if (HasStateAuthority == false)
        {
            RPC_AddPoints(amount);
            return;
        }

        TotalPoints += amount;
    }

    public bool TrySpendPoints(int amount)
    {
        if (Object == null || Object.IsValid == false || amount < 0)
        {
            return false;
        }

        if (amount == 0)
        {
            return true;
        }

        if (HasStateAuthority == false)
        {
            return false;
        }

        if (TotalPoints < amount)
        {
            return false;
        }

        TotalPoints -= amount;
        return true;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_AddPoints(int amount)
    {
        if (HasStateAuthority == false || amount <= 0)
        {
            return;
        }

        TotalPoints += amount;
    }

    private void RefreshPointsUI(bool force)
    {
        if (HasStateAuthority == false || _pointsUIText == null)
        {
            return;
        }

        if (force == false && TotalPoints == _lastDisplayedPoints)
        {
            return;
        }

        _pointsUIText.text = $"Points: {TotalPoints}";
        _lastDisplayedPoints = TotalPoints;
    }
}
