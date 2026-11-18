using System;
using UnityEngine;

namespace Supermarket.Building
{
  /// <summary>
  /// Minimal economy hook so costs on definitions have an effect.
  /// Replace or extend for the real economy later.
  /// </summary>
  public class SimpleWallet : MonoBehaviour, IBuildEconomy
  {
    [SerializeField] int startingBalance = 1000;

    public int Balance { get; private set; }

    /// <summary>Raised whenever the balance changes.</summary>
    public event Action<int> BalanceChanged;

    void Awake()
    {
      Initialize(startingBalance);
    }

    /// <summary>Sets the balance, e.g. for tests or a new game.</summary>
    public void Initialize(int balance)
    {
      Balance = Mathf.Max(0, balance);
      RaiseBalanceChanged();
    }

    public bool CanAfford(int amount)
    {
      return amount <= 0 || Balance >= amount;
    }

    public bool TrySpend(int amount)
    {
      if (amount <= 0) return true;
      if (Balance < amount) return false;

      Balance -= amount;
      RaiseBalanceChanged();
      return true;
    }

    public void Add(int amount)
    {
      Balance += Mathf.Max(0, amount);
      RaiseBalanceChanged();
    }

    void RaiseBalanceChanged()
    {
      if (BalanceChanged != null) BalanceChanged(Balance);
    }
  }
}
