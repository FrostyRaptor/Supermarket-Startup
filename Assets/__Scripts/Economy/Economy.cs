using System;
using UnityEngine;
using Supermarket.Building;
using Supermarket.Config;

namespace Supermarket.Economy
{
    /// <summary>
    /// Real wallet for the tycoon game. Extends SimpleWallet so all existing
    /// build-system plumbing keeps working, and adds sales/wage tracking plus
    /// the "$ per real minute" figure shown in the top-left HUD tab.
    /// </summary>
    public class Economy : SimpleWallet
    {
        public GameConfig config;

        public float LastDayIncome { get; private set; }
        public float LastDayWages { get; private set; }
        public float TotalIncome { get; private set; }
        public float TotalWages { get; private set; }

        public event Action StatsChanged;

        protected override void Awake()
        {
            base.Awake();
            if (config != null) Initialize(config.startingMoney);
            RaiseStats();
        }

        public void RecordSale(float amount)
        {
            int value = Mathf.Max(0, Mathf.RoundToInt(amount));
            if (value <= 0) return;
            Add(value);
            LastDayIncome += value;
            TotalIncome += value;
            RaiseStats();
        }

        /// <summary>Debits daily wages. Insufficient funds simply drain the wallet to zero.</summary>
        public void ChargeWages(int amount)
        {
            int value = Mathf.Max(0, amount);
            if (value > Balance) value = Balance;
            if (value > 0) TrySpend(value);
            LastDayWages += value;
            TotalWages += value;
            RaiseStats();
        }

        public void BeginNewDay()
        {
            LastDayIncome = 0f;
            LastDayWages = 0f;
            RaiseStats();
        }

        public float NetPerRealMinute
        {
            get
            {
                float minutes = config == null ? 10f : Mathf.Max(1f, config.realMinutesPerDay);
                return (LastDayIncome - LastDayWages) / minutes;
            }
        }

        public void Restore(int money, float income, float wages, float totalIncome, float totalWages)
        {
            Initialize(money);
            LastDayIncome = Mathf.Max(0f, income);
            LastDayWages = Mathf.Max(0f, wages);
            TotalIncome = Mathf.Max(0f, totalIncome);
            TotalWages = Mathf.Max(0f, totalWages);
            RaiseStats();
        }

        void RaiseStats()
        {
            if (StatsChanged != null) StatsChanged();
        }
    }
}
