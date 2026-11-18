namespace Supermarket.Building
{
  public interface IBuildEconomy
  {
    int Balance { get; }

    /// <summary>Checks affordability without changing the balance.</summary>
    bool CanAfford(int amount);

    bool TrySpend(int amount);
  }
}
