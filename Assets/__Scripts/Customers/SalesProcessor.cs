using System;
using System.Collections.Generic;
using UnityEngine;
using Supermarket.Config;
using Supermarket.Economy;
using Supermarket.Employees;
using Supermarket.Products;
using Supermarket.Progression;
using Supermarket.Store;

namespace Supermarket.Customers
{
    /// <summary>
    /// Abstract daily sales: demand is filled from shelf/finished stock, limited
    /// by checkout throughput, staffing, unlocks and the liquor license.
    /// </summary>
    public class SalesProcessor : MonoBehaviour
    {
        public GameConfig config;
        public ProductCatalog catalog;
        public Inventory inventory;
        public Economy.Economy economy;
        public EmployeeManager employees;
        public SectionManager sections;
        public ProgressionManager progression;
        public UpgradeSystem upgrades;

        public Dictionary<string, int> LastSales = new Dictionary<string, int>();
        public int LastUnitsSold { get; private set; }
        public float LastRevenue { get; private set; }

        /// <summary>Unlock/license availability only; demand exists before staffing is hired.</summary>
        public bool IsProductAvailable(ProductInfo product)
        {
            if (product == null || catalog == null) return false;
            if (sections != null && !sections.IsUnlocked(product.section)) return false;
            if (product.requiresLicense && progression != null && !progression.CanSellAlcohol) return false;
            return true;
        }

        public bool CanSell(ProductInfo product)
        {
            if (!IsProductAvailable(product)) return false;
            return employees != null && employees.CanSell(product.section);
        }

        public int EstimateDemand(string productId, int businessDay)
        {
            if (catalog == null || config == null) return 0;
            ProductInfo p = catalog.Get(productId);
            if (!IsProductAvailable(p)) return 0;
            if (p.baseDailyDemand <= 0f) return 0;
            float growth = 1f + Mathf.Min(2f, Mathf.Max(0, businessDay - 1) * config.demandGrowthPerDay / 10f);
            float demand = p.baseDailyDemand * growth;
            return Mathf.Max(1, Mathf.RoundToInt(demand));
        }

        public int CheckoutCapacity()
        {
            if (config == null || employees == null || sections == null) return 0;
            int staff = employees.Count(EmployeeRole.Checkout);
            int lanes = sections.CheckoutLanes;
            if (staff <= 0 || lanes <= 0) return 0;
            float efficiency = 0f;
            for (int i = 0; i < lanes; i++) efficiency += upgrades == null ? 1f : upgrades.CheckoutEfficiency(i);
            float perLane = Mathf.Max(1, config.checkoutCustomersPerEmployee);
            return Mathf.Max(0, Mathf.RoundToInt(staff * perLane * efficiency));
        }

        public void ProcessDay(int businessDay)
        {
            LastSales.Clear();
            LastUnitsSold = 0;
            LastRevenue = 0f;

            if (catalog == null || inventory == null || economy == null) return;
            int capacity = CheckoutCapacity();
            if (capacity <= 0) return;

            int totalDemand = 0;
            for (int i = 0; i < catalog.products.Count; i++)
            {
                ProductInfo p = catalog.products[i];
                if (!CanSell(p)) continue;
                totalDemand += EstimateDemand(p.id, businessDay);
            }
            if (totalDemand <= 0) return;

            float factor = Mathf.Min(1f, (float)capacity / totalDemand);
            for (int i = 0; i < catalog.products.Count && capacity > 0; i++)
            {
                ProductInfo p = catalog.products[i];
                if (!CanSell(p)) continue;

                int wanted = Mathf.FloorToInt(EstimateDemand(p.id, businessDay) * factor);
                if (wanted <= 0) continue;

                int sold = p.kind == ProductKind.Finished
                    ? inventory.TakeFromFinished(p.id, wanted)
                    : inventory.TakeFromShelf(p.id, wanted);
                if (sold <= 0) continue;

                int units = Mathf.Min(sold, capacity);
                float revenue = units * Mathf.Max(0f, p.salePrice);
                economy.RecordSale(revenue);
                LastSales[p.id] = units;
                LastUnitsSold += units;
                LastRevenue += revenue;
                capacity -= units;
            }
        }
    }
}
