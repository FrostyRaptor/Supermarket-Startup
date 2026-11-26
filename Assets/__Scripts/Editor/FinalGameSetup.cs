using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Supermarket.Building;
using Supermarket.Config;
using Supermarket.Core;
using Supermarket.Customers;
using GameWallet = Supermarket.Economy.Economy;
using Supermarket.Employees;
using Supermarket.Products;
using Supermarket.Progression;
using Supermarket.Store;
using Supermarket.UI;

namespace Supermarket.Editor
{
    /// <summary>
    /// One-click bootstrap for the finished tycoon game: creates all config
    /// assets and placeables, then wires the systems into __Scene_0.
    /// Safe to rerun; existing assets are patched rather than duplicated.
    /// </summary>
    public static class FinalGameSetup
    {
        const string ScenePath = "Assets/_Scenes/__Scene_0.unity";
        const string ConfigFolder = "Assets/_ScriptableObjects/Game";
        const string UpgradeFolder = "Assets/_ScriptableObjects/Game/Upgrades";
        const string PlaceableFolder = "Assets/_Prefabs/Placeables";
        const string LevelMaterialFolder = "Assets/_Materials/UpgradeLevels";

        [MenuItem("Tools/Supermarket/Final Game/Setup Final Game")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit play mode before setting up the final game.");
                return;
            }

            EnsureFolder("Assets/_ScriptableObjects");
            EnsureFolder(LevelMaterialFolder);
            EnsureFolder(ConfigFolder);
            EnsureFolder(UpgradeFolder);
            EnsureFolder(PlaceableFolder);

            GameConfig gameConfig = EnsureAsset<GameConfig>(ConfigFolder + "/GameConfig.asset");
            EmployeeConfig employeeConfig = EnsureAsset<EmployeeConfig>(ConfigFolder + "/EmployeeConfig.asset");
            SectionConfig sectionConfig = EnsureAsset<SectionConfig>(ConfigFolder + "/SectionConfig.asset");
            ProductCatalog catalog = EnsureAsset<ProductCatalog>(ConfigFolder + "/ProductCatalog.asset");

            BuildProductCatalog(catalog);
            BuildSectionConfig(sectionConfig);

            UpgradeProfile shelf = Upgrade("Shelf", "Shelf", new[] { 8, 12, 18, 26, 36 }, new[] { 0, 180, 340, 620, 1000 });
            UpgradeProfile cooler = Upgrade("Cooler", "Cooler", new[] { 12, 18, 26, 36, 50 }, new[] { 0, 240, 450, 800, 1300 });
            UpgradeProfile freezer = Upgrade("Freezer", "Freezer", new[] { 10, 15, 22, 32, 44 }, new[] { 0, 220, 420, 760, 1250 });
            UpgradeProfile alcoholShelf = Upgrade("AlcoholShelf", "Alcohol Shelf", new[] { 8, 12, 18, 26, 36 }, new[] { 0, 220, 400, 700, 1100 });
            UpgradeProfile lane = Upgrade("CheckoutLane", "Checkout Lane", new[] { 1, 1, 1, 1, 1 }, new[] { 0, 300, 550, 900, 1500 });
            lane.efficiencyByLevel = new[] { 1f, 1.35f, 1.8f, 2.4f, 3.2f };
            UpgradeProfile loading = Upgrade("LoadingZone", "Loading Zone", new[] { 1, 1, 1, 1, 1 }, new[] { 0, 280, 500, 850, 1400 });
            loading.efficiencyByLevel = new[] { 1f, 1.4f, 1.9f, 2.5f, 3.3f };
            UpgradeProfile storageShelf = Upgrade("StorageShelf", "Storage Shelf", new[] { 40, 60, 90, 130, 180 }, new[] { 0, 260, 480, 820, 1300 });
            UpgradeProfile butcher = Upgrade("Butcher", "Butcher Equipment", new[] { 1, 1, 1, 1, 1 }, new[] { 0, 350, 650, 1050, 1700 });
            butcher.efficiencyByLevel = new[] { 1f, 1.3f, 1.7f, 2.2f, 3f };
            butcher.yieldByLevel = new[] { 1, 2, 3, 4, 5 };
            UpgradeProfile bakery = Upgrade("Bakery", "Bakery Equipment", new[] { 1, 1, 1, 1, 1 }, new[] { 0, 350, 650, 1050, 1700 });
            bakery.efficiencyByLevel = new[] { 1f, 1.3f, 1.7f, 2.2f, 3f };
            bakery.yieldByLevel = new[] { 1, 2, 3, 4, 5 };
            UpgradeProfile deli = Upgrade("Deli", "Deli Equipment", new[] { 1, 1, 1, 1, 1 }, new[] { 0, 350, 650, 1050, 1700 });
            deli.efficiencyByLevel = new[] { 1f, 1.3f, 1.7f, 2.2f, 3f };
            deli.yieldByLevel = new[] { 1, 2, 3, 4, 5 };
            EditorUtility.SetDirty(lane);
            EditorUtility.SetDirty(loading);
            EditorUtility.SetDirty(butcher);
            EditorUtility.SetDirty(bakery);
            EditorUtility.SetDirty(deli);

            Material wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Materials/Mat_Shelf.mat");
            Material frame = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Materials/Mat_ShelfFrame.mat");
            GameObject shelfPrefab = EnsureBoxPrefab("Shelf_1x1", new Vector3(0.9f, 1.8f, 0.9f), wood, frame);
            GameObject coolerPrefab = EnsureBoxPrefab("Cooler_2x1", new Vector3(1.9f, 2f, 0.9f), frame, wood);
            GameObject freezerPrefab = EnsureBoxPrefab("Freezer_2x1", new Vector3(1.9f, 2f, 0.9f), frame, wood);
            GameObject alcoholPrefab = EnsureBoxPrefab("AlcoholShelf_1x1", new Vector3(0.9f, 1.8f, 0.9f), wood, frame);
            GameObject checkoutLanePrefab = EnsureBoxPrefab(
                "CheckoutLane_4x3", new Vector3(3.9f, 0.9f, 2.9f), frame, wood);

            PlaceableCatalog placeables = AssetDatabase.LoadAssetAtPath<PlaceableCatalog>("Assets/_ScriptableObjects/Building/PlaceableCatalog.asset");
            if (placeables == null)
            {
                Debug.LogError("PlaceableCatalog asset is missing. Run Tools > Supermarket > Build System > Setup Scene first.");
                return;
            }
            EnsureDefinition("Def_Shelf_1x1", "shelf_1x1", "Shelf 1x1", shelfPrefab, new Vector2Int(1, 1), 180, shelf, placeables);
            EnsureDefinition("Def_Cooler_2x1", "cooler_2x1", "Cooler 2x1", coolerPrefab, new Vector2Int(2, 1), 260, cooler, placeables);
            EnsureDefinition("Def_Freezer_2x1", "freezer_2x1", "Freezer 2x1", freezerPrefab, new Vector2Int(2, 1), 240, freezer, placeables);
            EnsureDefinition("Def_AlcoholShelf_1x1", "alcohol_shelf_1x1", "Alcohol Shelf 1x1", alcoholPrefab, new Vector2Int(1, 1), 220, alcoholShelf, placeables);
            EnsureDefinition(
                "Def_CheckoutLane_4x3", "checkout_lane_4x3", "Checkout Lane 4x3",
                checkoutLanePrefab, new Vector2Int(4, 3), sectionConfig.checkoutLaneCost,
                lane, placeables, PlaceableArea.Checkout);
            EditorUtility.SetDirty(placeables);

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            EnsureSceneSection("StorageWalls", StoreSectionType.Storage, true);
            EnsureSceneSection("CheckoutWalls", StoreSectionType.Checkout, true);
            EnsureSceneSection("ButcherWalls", StoreSectionType.Butcher, false);
            EnsureSceneSection("BakeryWalls", StoreSectionType.Bakery, false);
            EnsureSceneSection("DeliWalls", StoreSectionType.Deli, false);
            EnsureSceneSection("AlcoholWalls", StoreSectionType.Alcohol, false);
            GameObject store = GameObject.Find("Store") ?? new GameObject("Store");
            EnsureSceneSectionObject(store, StoreSectionType.General, true);

            BuildGrid grid = FindScene<BuildGrid>();
            BuildController controller = FindScene<BuildController>();
            if (grid == null || controller == null)
            {
                Debug.LogError("Build system not found in the scene. Run the Build System setup first.");
                return;
            }

            // Keep the full-store build area and section mask in sync with the floor.
            Supermarket.EditorTools.BuildSystemSetup.EnsureBuildArea(grid);
            Supermarket.EditorTools.BuildSystemSetup.EnsureSectionVisuals();

            GameObject host = GameObject.Find("GameSystems") ?? new GameObject("GameSystems");
            SimpleWallet oldWallet = FindScene<SimpleWallet>();
            GameWallet economy;
            if (oldWallet != null)
            {
                GameObject walletHost = oldWallet.gameObject;
                Object.DestroyImmediate(oldWallet);
                economy = walletHost.AddComponent<GameWallet>();
            }
            else
            {
                economy = GetOrAdd<GameWallet>(host);
            }

            GameClock clock = GetOrAdd<GameClock>(host);
            NotificationCenter notices = GetOrAdd<NotificationCenter>(host);
            SectionManager sections = GetOrAdd<SectionManager>(host);
            EmployeeManager employeeManager = GetOrAdd<EmployeeManager>(host);
            UpgradeSystem upgradeSystem = GetOrAdd<UpgradeSystem>(host);
            Inventory inventory = GetOrAdd<Inventory>(host);
            SalesProcessor sales = GetOrAdd<SalesProcessor>(host);
            OrderManager orderManager = GetOrAdd<OrderManager>(host);
            ProgressionManager progression = GetOrAdd<ProgressionManager>(host);
            LiquorLicense license = GetOrAdd<LiquorLicense>(host);
            GameSave save = GetOrAdd<GameSave>(host);
            GameManager manager = GetOrAdd<GameManager>(host);
            HUDController hud = GetOrAdd<HUDController>(host);

            SetObj(clock, "config", gameConfig);
            SetObj(economy, "config", gameConfig);
            SetObj(sections, "config", sectionConfig);
            SetObj(sections, "economy", economy);
            SetObj(sections, "notices", notices);
            SetObj(sections, "_buildGrid", grid);
            SetObj(employeeManager, "config", employeeConfig);
            SetObj(employeeManager, "sections", sections);
            SetObj(employeeManager, "notices", notices);

            SetObj(upgradeSystem, "economy", economy);
            SetObj(upgradeSystem, "notices", notices);
            SetObj(upgradeSystem, "grid", grid);
            SetObj(upgradeSystem, "LaneProfile", lane);
            SetObj(upgradeSystem, "LoadingZoneProfile", loading);
            SetObj(upgradeSystem, "StorageShelfProfile", storageShelf);
            SetObj(upgradeSystem, "ButcherProfile", butcher);
            SetObj(upgradeSystem, "BakeryProfile", bakery);
            SetObj(upgradeSystem, "DeliProfile", deli);

            SetObj(inventory, "catalog", catalog);
            SetObj(inventory, "sections", sections);
            SetObj(inventory, "employees", employeeManager);
            SetObj(inventory, "upgrades", upgradeSystem);
            SetObj(inventory, "notices", notices);

            SetObj(license, "config", gameConfig);
            SetObj(license, "notices", notices);
            SetObj(progression, "sections", sections);
            SetObj(progression, "license", license);
            SetObj(progression, "notices", notices);

            SetObj(sales, "config", gameConfig);
            SetObj(sales, "catalog", catalog);
            SetObj(sales, "inventory", inventory);
            SetObj(sales, "economy", economy);
            SetObj(sales, "employees", employeeManager);
            SetObj(sales, "sections", sections);
            SetObj(sales, "progression", progression);
            SetObj(sales, "upgrades", upgradeSystem);

            SetObj(orderManager, "config", gameConfig);
            SetObj(orderManager, "catalog", catalog);
            SetObj(orderManager, "inventory", inventory);
            SetObj(orderManager, "economy", economy);
            SetObj(orderManager, "sales", sales);
            SetObj(orderManager, "progression", progression);
            SetObj(orderManager, "clock", clock);
            SetObj(orderManager, "notices", notices);

            SetObj(manager, "config", gameConfig);
            SetObj(manager, "clock", clock);
            SetObj(manager, "economy", economy);
            SetObj(manager, "notices", notices);
            SetObj(manager, "save", save);
            SetObj(manager, "sections", sections);
            SetObj(manager, "employees", employeeManager);
            SetObj(manager, "upgrades", upgradeSystem);
            SetObj(manager, "inventory", inventory);
            SetObj(manager, "sales", sales);
            SetObj(manager, "orders", orderManager);
            SetObj(manager, "progression", progression);
            SetObj(manager, "license", license);
            SetObj(manager, "grid", grid);

            SetObj(save, "game", manager);
            SetObj(hud, "game", manager);
            SetObj(hud, "clock", clock);
            SetObj(hud, "economy", economy);
            SetObj(hud, "notices", notices);
            SetObj(hud, "sections", sections);
            SetObj(hud, "employees", employeeManager);
            SetObj(hud, "upgrades", upgradeSystem);
            SetObj(hud, "inventory", inventory);
            SetObj(hud, "orders", orderManager);
            SetObj(hud, "sales", sales);
            SetObj(hud, "progression", progression);
            SetObj(hud, "license", license);
            SetObj(hud, "catalog", catalog);

            SetObj(controller, "economySource", economy);
            BuildMenuUI buildMenu = FindScene<BuildMenuUI>();
            if (buildMenu != null) SetObj(buildMenu, "wallet", economy);

            EditorUtility.SetDirty(host);
            EditorUtility.SetDirty(grid);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Final game setup complete. Config assets created and scene wired.");
        }

        static void BuildProductCatalog(ProductCatalog catalog)
        {
            catalog.products = new List<ProductInfo>
            {
                P("milk", "Milk", StoreSectionType.General, ProductKind.Shelf, 12f, 22f, 16f),
                P("soda", "Soda", StoreSectionType.General, ProductKind.Shelf, 8f, 16f, 14f),
                P("cereal", "Cereal", StoreSectionType.General, ProductKind.Shelf, 14f, 26f, 10f),
                P("beer", "Beer", StoreSectionType.Alcohol, ProductKind.Shelf, 18f, 38f, 12f, null, 1, true),
                P("wine", "Wine", StoreSectionType.Alcohol, ProductKind.Shelf, 40f, 80f, 8f, null, 1, true),
                P("raw_meat", "Raw Meat", StoreSectionType.Butcher, ProductKind.Shelf, 20f, 0f, 0f),
                P("steak", "Steak", StoreSectionType.Butcher, ProductKind.Finished, 0f, 65f, 12f, "raw_meat", 2),
                P("raw_fish", "Raw Fish", StoreSectionType.Butcher, ProductKind.Shelf, 24f, 0f, 0f),
                P("salmon", "Salmon", StoreSectionType.Butcher, ProductKind.Finished, 0f, 75f, 8f, "raw_fish", 2),
                P("flour", "Flour", StoreSectionType.Bakery, ProductKind.Shelf, 8f, 0f, 0f),
                P("bread", "Bread", StoreSectionType.Bakery, ProductKind.Finished, 0f, 14f, 18f, "flour", 1),
                P("cake", "Cake", StoreSectionType.Bakery, ProductKind.Finished, 0f, 30f, 8f, "flour", 3),
                P("deli_meat", "Deli Meat", StoreSectionType.Deli, ProductKind.Shelf, 18f, 0f, 0f),
                P("sandwich", "Sandwich", StoreSectionType.Deli, ProductKind.Finished, 0f, 28f, 14f, "deli_meat", 1)
            };
            EditorUtility.SetDirty(catalog);
        }

        static ProductInfo P(string id, string name, StoreSectionType section, ProductKind kind,
            float cost, float price, float demand, string input = null, int inputUnits = 1, bool license = false)
        {
            return new ProductInfo
            {
                id = id,
                displayName = name,
                section = section,
                kind = kind,
                unitCost = cost,
                salePrice = price,
                baseDailyDemand = demand,
                inputProductId = input,
                inputUnits = inputUnits,
                requiresLicense = license
            };
        }

        static void BuildSectionConfig(SectionConfig config)
        {
            config.sections = new List<SectionLayoutInfo>
            {
                S(StoreSectionType.General, "General", 0, new Vector3(0f, 0f, 0f)),
                S(StoreSectionType.Storage, "Storage", 0, new Vector3(50f, 0f, 220f),
                    new[] { new Vector3(-8f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(8f, 0f, 0f) }),
                S(StoreSectionType.Checkout, "Checkout", 0, new Vector3(8f, 0f, 8f),
                    new[] { new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), new Vector3(8f, 0f, 0f), new Vector3(12f, 0f, 0f) }),
                S(StoreSectionType.Butcher, "Butcher", 900, new Vector3(56f, 0f, 24f),
                    new[] { new Vector3(-4f, 0f, 0f), new Vector3(4f, 0f, 0f) }),
                S(StoreSectionType.Bakery, "Bakery", 800, new Vector3(24f, 0f, 56f),
                    new[] { new Vector3(-4f, 0f, 0f), new Vector3(4f, 0f, 0f) }),
                S(StoreSectionType.Deli, "Deli", 850, new Vector3(24f, 0f, -16f),
                    new[] { new Vector3(-4f, 0f, 0f), new Vector3(4f, 0f, 0f) }),
                S(StoreSectionType.Alcohol, "Alcohol", 1500, new Vector3(8f, 0f, -16f),
                    new[] { new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f) })
            };
            config.checkoutLaneCost = 250;
            config.loadingZoneCost = 300;
            config.maxCheckoutLanes = 4;
            config.maxLoadingZones = 3;
            EditorUtility.SetDirty(config);
        }

        static SectionLayoutInfo S(StoreSectionType type, string name, int cost, Vector3 center, Vector3[] slots = null)
        {
            return new SectionLayoutInfo
            {
                type = type,
                displayName = name,
                unlockCost = cost,
                center = center,
                slots = slots ?? new Vector3[0]
            };
        }

        static UpgradeProfile Upgrade(string file, string name, int[] capacity, int[] costs)
        {
            string path = UpgradeFolder + "/UP_" + file + ".asset";
            UpgradeProfile profile = EnsureAsset<UpgradeProfile>(path);
            profile.id = file.ToLowerInvariant();
            profile.displayName = name;
            profile.capacityByLevel = capacity;
            profile.upgradeCostByLevel = costs;
            if (profile.efficiencyByLevel == null || profile.efficiencyByLevel.Length != 5)
                profile.efficiencyByLevel = new[] { 1f, 1.3f, 1.7f, 2.2f, 3f };
            if (profile.yieldByLevel == null || profile.yieldByLevel.Length != 5)
                profile.yieldByLevel = new[] { 1, 2, 3, 4, 5 };
            profile.levelMaterials = EnsureLevelMaterials(file);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static Material[] EnsureLevelMaterials(string file)
        {
            Color[] palette =
            {
                new Color(0.52f, 0.55f, 0.59f, 1f),
                new Color(0.30f, 0.65f, 0.40f, 1f),
                new Color(0.24f, 0.53f, 0.82f, 1f),
                new Color(0.60f, 0.39f, 0.77f, 1f),
                new Color(0.96f, 0.73f, 0.19f, 1f)
            };

            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");

            Material[] materials = new Material[palette.Length];
            for (int i = 0; i < palette.Length; i++)
            {
                string path = LevelMaterialFolder + "/MAT_" + file + "_Lv" + (i + 1) + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }
                material.name = "MAT_" + file + "_Lv" + (i + 1);
                material.color = palette[i];
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", palette[i]);
                EditorUtility.SetDirty(material);
                materials[i] = material;
            }
            AssetDatabase.SaveAssets();
            return materials;
        }

        static GameObject EnsureBoxPrefab(string name, Vector3 size, Material body, Material frame)
        {
            string path = PlaceableFolder + "/" + name + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject root = new GameObject(name);
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Body";
            box.transform.SetParent(root.transform, false);
            box.transform.localScale = size;
            box.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.GetComponent<MeshRenderer>().sharedMaterial = body != null ? body : frame;

            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Top";
            top.transform.SetParent(root.transform, false);
            top.transform.localScale = new Vector3(size.x, 0.12f, size.z);
            top.transform.localPosition = new Vector3(0f, size.y + 0.06f, 0f);
            Object.DestroyImmediate(top.GetComponent<Collider>());
            top.GetComponent<MeshRenderer>().sharedMaterial = frame != null ? frame : body;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = size;
            collider.center = new Vector3(0f, size.y * 0.5f, 0f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void EnsureDefinition(string file, string id, string displayName, GameObject prefab,
            Vector2Int footprint, int cost, UpgradeProfile profile, PlaceableCatalog catalog,
            PlaceableArea allowedArea = PlaceableArea.General)
        {
            string path = "Assets/_ScriptableObjects/Building/" + file + ".asset";
            PlaceableDefinition def = EnsureAsset<PlaceableDefinition>(path);
            def.id = id;
            def.displayName = displayName;
            def.prefab = prefab;
            def.footprint = footprint;
            def.cost = cost;
            def.category = PlaceableCategory.Furniture;
            def.allowedArea = allowedArea;
            def.upgradeProfile = profile;
            EditorUtility.SetDirty(def);

            if (!catalog.definitions.Contains(def))
            {
                List<PlaceableDefinition> list = new List<PlaceableDefinition>(catalog.Definitions);
                list.Add(def);
                SerializedObject so = new SerializedObject(catalog);
                SerializedProperty prop = so.FindProperty("definitions");
                if (prop != null)
                {
                    prop.arraySize = list.Count;
                    for (int i = 0; i < list.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        static void EnsureSceneSection(string objectName, StoreSectionType type, bool unlocked)
        {
            GameObject go = GameObject.Find(objectName);
            if (go == null)
            {
                Debug.LogWarning("Section wall object not found: " + objectName);
                return;
            }
            EnsureSceneSectionObject(go, type, unlocked);
        }

        static void EnsureSceneSectionObject(GameObject go, StoreSectionType type, bool unlocked)
        {
            StoreSection section = go.GetComponent<StoreSection>() ?? go.AddComponent<StoreSection>();
            section.type = type;
            section.displayName = type.ToString();
            section.startsUnlocked = unlocked;
            EditorUtility.SetDirty(section);
        }

        static T FindScene<T>() where T : Component
        {
            T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return found != null && found.Length > 0 ? found[0] : null;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            if (slash <= 0) return;
            string parent = path.Substring(0, slash);
            string child = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, child);
        }

        static void SetObj(Component component, string property, Object value)
        {
            if (component == null) return;
            SerializedObject so = new SerializedObject(component);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogWarning("Missing serialized property " + property + " on " + component.GetType().Name);
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
