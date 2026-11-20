using System;
using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Config
{
    public enum StoreSectionType
    {
        General,
        Storage,
        Butcher,
        Bakery,
        Deli,
        Checkout,
        Alcohol
    }

    public enum ProductKind
    {
        /// <summary>Sold from a shelf/cooler/freezer in the general or alcohol section.</summary>
        Shelf,
        /// <summary>Made by staff from an input product, then sold from a display.</summary>
        Finished
    }

    /// <summary>One purchasable/sellable product. Costs, prices and demand are editor values.</summary>
    [Serializable]
    public class ProductInfo
    {
        public string id;
        public string displayName;
        public StoreSectionType section;
        public ProductKind kind = ProductKind.Shelf;

        [Header("Economy")]
        public float unitCost = 10f;
        public float salePrice = 20f;
        public float baseDailyDemand = 12f;

        [Header("Finished goods")]
        [Tooltip("Storage product consumed to make one unit of this finished product.")]
        public string inputProductId;
        public int inputUnits = 1;

        [Header("Rules")]
        public bool requiresLicense;
    }

    [CreateAssetMenu(menuName = "Supermarket/Product Catalog", fileName = "ProductCatalog")]
    public class ProductCatalog : ScriptableObject
    {
        public List<ProductInfo> products = new List<ProductInfo>();

        public ProductInfo Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < products.Count; i++)
            {
                ProductInfo p = products[i];
                if (p != null && p.id == id) return p;
            }
            return null;
        }

        public IEnumerable<ProductInfo> ForSection(StoreSectionType section)
        {
            for (int i = 0; i < products.Count; i++)
            {
                ProductInfo p = products[i];
                if (p != null && p.section == section) yield return p;
            }
        }
    }
}
