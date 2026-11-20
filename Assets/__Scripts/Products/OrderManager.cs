using UnityEngine;
using Supermarket.Config;
using Supermarket.Core;
using Supermarket.Economy;
using Supermarket.Progression;

namespace Supermarket.Products
{
    /// <summary>
    /// Purchase orders. Suggestions are slightly above projected demand; the
    /// player always has final say over the ordered quantity.
    /// </summary>
    public class OrderManager : MonoBehaviour
    {
        public GameConfig config;
        public ProductCatalog catalog;
        public Inventory inventory;
        public Economy.Economy economy;
        public Customers.SalesProcessor sales;
        public ProgressionManager progression;
        public GameClock clock;
        public NotificationCenter notices;

        public int SuggestQuantity(string productId)
        {
            if (catalog == null) return 0;
            ProductInfo p = catalog.Get(productId);
            if (p == null || inventory == null) return 0;
            if (p.requiresLicense && progression != null && !progression.CanSellAlcohol) return 0;

            int nextDay = clock == null ? 1 : clock.BusinessDay + 1;
            int demand = sales == null ? Mathf.RoundToInt(p.baseDailyDemand) : sales.EstimateDemand(productId, nextDay);
            float factor = config == null ? 1.12f : Mathf.Max(1f, config.suggestionFactor);
            int target = Mathf.CeilToInt(demand * factor);
            return Mathf.Max(0, target - inventory.TotalOnHand(productId));
        }

        public int OrderCost(string productId, int quantity)
        {
            ProductInfo p = catalog == null ? null : catalog.Get(productId);
            if (p == null || quantity <= 0) return 0;
            return Mathf.CeilToInt(p.unitCost * quantity);
        }

        public bool TryPlaceOrder(string productId, int quantity)
        {
            ProductInfo p = catalog == null ? null : catalog.Get(productId);
            if (p == null || quantity <= 0 || inventory == null || economy == null) return false;
            int cost = OrderCost(productId, quantity);
            if (!economy.CanAfford(cost))
            {
                if (notices != null) notices.Notify("Not enough money for that order.", NoticeKind.Warning);
                return false;
            }
            economy.TrySpend(cost);
            int arrival = clock == null ? 1 : clock.BusinessDay + 1;
            inventory.ScheduleDelivery(productId, quantity, arrival);
            if (notices != null) notices.Notify("Ordered " + quantity + " x " + p.displayName + " for delivery tomorrow.", NoticeKind.Success);
            return true;
        }
    }
}
