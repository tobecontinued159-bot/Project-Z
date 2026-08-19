using Fusion;

public interface IPlayerWallet
{
    int Balance { get; }
    bool CanAfford(int cost);
    bool TrySpend(int cost);
    void AddMoney(int amount);
}
