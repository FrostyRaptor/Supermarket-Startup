using NUnit.Framework;
using Supermarket.Config;
using Supermarket.Employees;
using Supermarket.Products;
using Supermarket.Progression;
using Supermarket.Store;
using Supermarket.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Supermarket.Tests
{
    /// <summary>
    /// Guards the authored Canvas prefabs that replaced runtime-generated UI.
    /// Display wiring is verified here; gameplay text logic stays in the controllers.
    /// </summary>
    public class AuthoredUITests
    {
        [Test]
        public void HudPrefab_ExposesRequiredDisplayReferences()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/UI/TycoonHUD.prefab");
            Assert.IsNotNull(prefab, "Authored HUD prefab is missing.");

            HUDView view = prefab.GetComponent<HUDView>();
            Assert.IsNotNull(view, "HUDView is missing from the authored HUD prefab.");

            Assert.IsNotNull(view.dayText);
            Assert.IsNotNull(view.storeButton);
            Assert.IsNotNull(view.noticesContainer);
            Assert.IsNotNull(view.modalRoot);
            Assert.IsNotNull(view.modalCloseButton);
            Assert.IsNotNull(view.employeesTab);
            Assert.IsNotNull(view.upgradesTab);
            Assert.IsNotNull(view.ordersTab);
            Assert.IsNotNull(view.modalContent);
            Assert.IsNotNull(view.modalScroll);
            Assert.IsNotNull(view.toastTemplate);

            Assert.IsNotNull(view.toastTemplate.GetComponent<HudToastView>());

            Canvas canvas = prefab.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            Assert.AreEqual(90, canvas.sortingOrder);
        }

        [Test]
        public void HudPrefab_ContainsStaticTabBodies()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/UI/TycoonHUD.prefab");
            HUDView view = prefab.GetComponent<HUDView>();
            Assert.IsNotNull(view, "HUDView is missing from the authored HUD prefab.");

            Assert.IsNotNull(view.employeesPanel, "Employees body is not authored.");
            Assert.IsNotNull(view.upgradesPanel, "Upgrades body is not authored.");
            Assert.IsNotNull(view.ordersPanel, "Orders body is not authored.");
            Assert.IsTrue(view.employeesPanel.activeSelf, "Employees body should be the default visible tab.");
            Assert.IsFalse(view.upgradesPanel.activeSelf, "Upgrades body should start hidden.");
            Assert.IsFalse(view.ordersPanel.activeSelf, "Orders body should start hidden.");

            Assert.IsNotNull(view.employees);
            Assert.IsNotNull(view.upgrades);
            Assert.IsNotNull(view.orders);

            Assert.AreEqual(6, view.employees.rows.Length);
            Assert.AreEqual(5, view.employees.sectionHeadings.Length);
            Assert.IsNotNull(view.employees.wagesText);
            AssertStaticRows(view.employees.rows);
            foreach (Text heading in view.employees.sectionHeadings) Assert.IsNotNull(heading);

            Assert.AreEqual(5, view.upgrades.sectionRows.Length);
            Assert.AreEqual(5, view.upgrades.storageRows.Length);
            Assert.AreEqual(2, view.upgrades.butcherRows.Length);
            Assert.AreEqual(2, view.upgrades.bakeryRows.Length);
            Assert.AreEqual(2, view.upgrades.deliRows.Length);
            Assert.AreEqual(12, view.upgrades.alcoholFixtureRows.Length);
            Assert.IsNotNull(view.upgrades.storageInfoText);
            Assert.IsNotNull(view.upgrades.licenseStatusText);
            Assert.IsNotNull(view.upgrades.licenseStage1Row);
            Assert.IsNotNull(view.upgrades.licenseStage2Row);
            Assert.IsNotNull(view.upgrades.alcoholSectionPurchaseRow);
            Assert.IsNotNull(view.upgrades.alcoholEmptyText);
            AssertStaticRows(view.upgrades.sectionRows);
            AssertStaticRows(view.upgrades.storageRows);
            AssertStaticRows(view.upgrades.butcherRows);
            AssertStaticRows(view.upgrades.bakeryRows);
            AssertStaticRows(view.upgrades.deliRows);
            AssertStaticRows(view.upgrades.alcoholFixtureRows);
            AssertUpgradeRow(view.upgrades.licenseStage1Row);
            AssertUpgradeRow(view.upgrades.licenseStage2Row);
            AssertUpgradeRow(view.upgrades.alcoholSectionPurchaseRow);

            Transform upgrades = prefab.transform.Find("Modal/Window/Body/Content/Upgrades Panel");
            string[] expectedSections = { "Section Storage", "Section Butcher", "Section Bakery", "Section Deli", "Section Alcohol" };
            string[] expectedTitles = { "Storage", "Butcher", "Bakery", "Deli", "Alcohol" };
            for (int i = 0; i < expectedSections.Length; i++)
            {
                Assert.AreEqual(expectedSections[i], view.upgrades.sectionRows[i].name);
                AssertUpgradeRow(view.upgrades.sectionRows[i]);
                Assert.Less(view.upgrades.sectionRows[i].priceText.rectTransform.anchorMin.x,
                    view.upgrades.sectionRows[i].rightButton.GetComponent<RectTransform>().anchorMin.x,
                    "Price label should sit to the left of the buy button.");
                Assert.AreEqual(expectedTitles[i], view.upgrades.sectionRows[i].label.text.Trim());
                Assert.IsNull(upgrades.Find(expectedTitles[i] + "Heading"),
                    expectedTitles[i] + " should have only its title row, not a second heading.");
            }

            string[] excluded =
            {
                "SectionAccessHeading", "UpgradePathsHeading", "CheckoutHeading",
                "CheckoutLane", "GeneralHeading", "GeneralEmpty"
            };
            foreach (string name in excluded)
            {
                Transform child = upgrades.Find(name);
                Assert.IsNotNull(child, name + " should remain authored but hidden.");
                Assert.IsFalse(child.gameObject.activeSelf, name + " should not appear in the Upgrades tab.");
            }

            Assert.AreEqual(16, view.orders.rows.Length);
            Assert.IsNotNull(view.orders.topTotalText);
            Assert.IsNotNull(view.orders.topSubmitButton);
            Assert.IsNotNull(view.orders.bottomTotalText);
            Assert.IsNotNull(view.orders.bottomSubmitButton);
            foreach (HudOrderRowView row in view.orders.rows) AssertOrderRow(row);
        }

        [Test]
        public void BuildMenuPrefab_ExposesRequiredDisplayReferences()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/UI/BuildMenu.prefab");
            Assert.IsNotNull(prefab, "Authored Build Menu prefab is missing.");

            BuildMenuView view = prefab.GetComponent<BuildMenuView>();
            Assert.IsNotNull(view, "BuildMenuView is missing from the authored Build Menu prefab.");

            Assert.IsNotNull(view.buildBar);
            Assert.IsNotNull(view.hintsText);
            Assert.IsNotNull(view.messageText);
            Assert.IsNotNull(view.messageGroup);
            Assert.IsNotNull(view.hotbarContainer);
            Assert.IsNotNull(view.rotateButton);
            Assert.IsNotNull(view.closeButton);
            Assert.IsNotNull(view.currencyText);
            Assert.IsNotNull(view.rateText);
            Assert.IsNotNull(view.buildModeButton);
            Assert.IsNotNull(view.buildModeLabel);
            Assert.IsNotNull(view.hotbarItemTemplate);

            Assert.IsNotNull(view.hotbarItemTemplate.GetComponent<BuildMenuItemView>());

            Canvas canvas = prefab.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            Assert.AreEqual(100, canvas.sortingOrder);
        }

        [Test]
        public void LicenseStatus_MapsStateMachineToThreeLabels()
        {
            Assert.AreEqual("Incomplete", HUDController.LicenseStatus(null));

            GameObject host = new GameObject("License Test Host");
            try
            {
                LiquorLicense license = host.AddComponent<LiquorLicense>();
                Assert.AreEqual("Incomplete", HUDController.LicenseStatus(license));

                license.Restore(LicenseStage.Stage1Pending, 5, false);
                Assert.AreEqual("In Progress", HUDController.LicenseStatus(license));
                license.Restore(LicenseStage.Stage1Approved, 5, false);
                Assert.AreEqual("In Progress", HUDController.LicenseStatus(license));
                license.Restore(LicenseStage.Stage2Pending, 5, false);
                Assert.AreEqual("In Progress", HUDController.LicenseStatus(license));
                license.Restore(LicenseStage.Approved, 0, false);
                Assert.AreEqual("Complete", HUDController.LicenseStatus(license));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OrderTotals_UseGreenWhenAffordableAndRedOtherwise()
        {
            Color affordable = HUDController.TotalCostColor(true);
            Color unaffordable = HUDController.TotalCostColor(false);
            Assert.Greater(affordable.g, affordable.r, "Affordable total should be green.");
            Assert.Greater(unaffordable.r, unaffordable.g, "Unaffordable total should be red.");
        }

        [Test]
        public void EmployeeWages_UseConfiguredDailyAmounts()
        {
            EmployeeConfig config = ScriptableObject.CreateInstance<EmployeeConfig>();
            config.checkoutWage = 90;
            config.unloaderWage = 85;
            config.stockerWage = 80;
            try
            {
                Assert.AreEqual(90, HUDController.WageFor(config, EmployeeRole.Checkout));
                Assert.AreEqual(85, HUDController.WageFor(config, EmployeeRole.Unloader));
                Assert.AreEqual(80, HUDController.WageFor(config, EmployeeRole.Stocker));
                Assert.AreEqual(0, HUDController.WageFor(null, EmployeeRole.Checkout));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void OrderManager_CanGateProductsAndCalculateCartCost()
        {
            ProductCatalog catalog = ScriptableObject.CreateInstance<ProductCatalog>();
            catalog.products.Add(new ProductInfo
            {
                id = "milk", displayName = "Milk", unitCost = 12f, section = StoreSectionType.General
            });
            catalog.products.Add(new ProductInfo
            {
                id = "beer", displayName = "Beer", unitCost = 18f,
                section = StoreSectionType.Alcohol, requiresLicense = true
            });

            GameObject host = new GameObject("Order Test Host");
            GameObject progressionHost = new GameObject("Progression Test Host");
            try
            {
                OrderManager manager = host.AddComponent<OrderManager>();
                manager.catalog = catalog;
                Assert.AreEqual(36, manager.OrderCost("milk", 3));
                Assert.AreEqual(0, manager.OrderCost("milk", 0));
                Assert.IsTrue(manager.CanOrderProduct(catalog.products[0]));

                manager.ProductOrderGate = delegate { return false; };
                Assert.IsFalse(manager.CanOrderProduct(catalog.products[0]), "Storage gate should block ordering.");

                manager.ProductOrderGate = delegate { return true; };
                ProgressionManager progression = progressionHost.AddComponent<ProgressionManager>();
                manager.progression = progression;
                Assert.IsFalse(manager.CanOrderProduct(catalog.products[1]), "License gate should block alcohol orders.");
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(progressionHost);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void SectionManager_ExposesExpectedDefaultLocks()
        {
            GameObject host = new GameObject("Section Test Host");
            try
            {
                SectionManager sections = host.AddComponent<SectionManager>();
                sections.ResetToDefaults();
                Assert.IsTrue(sections.IsUnlocked(StoreSectionType.Storage));
                Assert.IsFalse(sections.IsUnlocked(StoreSectionType.Butcher));
                Assert.IsFalse(sections.IsUnlocked(StoreSectionType.Alcohol));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        static void AssertStaticRows(HudRowView[] rows)
        {
            Assert.IsNotNull(rows);
            foreach (HudRowView row in rows) Assert.IsNotNull(row);
        }

        static void AssertUpgradeRow(HudRowView row)
        {
            Assert.IsNotNull(row);
            Assert.IsNotNull(row.label);
            Assert.IsNotNull(row.priceText, "Upgrade rows need a cost label.");
            Assert.IsTrue(row.leftButton != null || row.rightButton != null, "Upgrade rows need an action button.");
        }

        static void AssertOrderRow(HudOrderRowView row)
        {
            Assert.IsNotNull(row);
            Assert.IsNotNull(row.nameText);
            Assert.IsNotNull(row.costText);
            Assert.IsNotNull(row.suggestionText);
            Assert.IsNotNull(row.quantityInput);
            Assert.IsNotNull(row.orderButton);
            Assert.IsNotNull(row.background);
        }
    }
}
