using Fusion;
using TMPro;
using UnityEngine;

public class PlayerPoints : NetworkBehaviour
{
    private const int StartingPoints = 500;

    [Networked]
    public int TotalPoints { get; set; } = StartingPoints;

    public TMP_Text pointsUIText;

    private int _lastDisplayedPoints = int.MinValue;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            TotalPoints = StartingPoints;
        }

        TryBindPointsUI();

        if (IsLocalPlayer == false && pointsUIText != null)
        {
            pointsUIText.enabled = false;
        }

        RefreshPointsUI(force: true);
    }

    public override void Render()
    {
        if (IsLocalPlayer == false)
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

    private bool IsLocalPlayer
    {
        get
        {
            return Object != null && Object.IsValid && HasInputAuthority;
        }
    }

    private void TryBindPointsUI()
    {
        if (pointsUIText != null)
        {
            return;
        }

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == "PointsText")
            {
                pointsUIText = texts[i];
                return;
            }
        }
    }

    private void RefreshPointsUI(bool force)
    {
        if (IsLocalPlayer == false || pointsUIText == null)
        {
            return;
        }

        if (force == false && TotalPoints == _lastDisplayedPoints)
        {
            return;
        }

        pointsUIText.text = $"Points: {TotalPoints}";
        _lastDisplayedPoints = TotalPoints;
    }
}
