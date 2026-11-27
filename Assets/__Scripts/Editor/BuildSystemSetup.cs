using System;
using System.IO;
using Supermarket.Building;
using Supermarket.Config;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Supermarket.EditorTools
{
  /// <summary>
  /// One-click bootstrap for the build system: creates materials, placeholder
  /// placeables, definitions, the catalog, and wires a BuildSystem object into
  /// the scene. Safe to run more than once; existing assets are not overwritten.
  /// </summary>
  public static class BuildSystemSetup
  {
    const string ScenePath = "Assets/_Scenes/__Scene_0.unity";
    const string MaterialFolder = "Assets/_Materials";
    const string PrefabFolder = "Assets/_Prefabs/Placeables";
    const string DataFolder = "Assets/_ScriptableObjects/Building";
    const float CellSize = 1f;

    [MenuItem("Tools/Supermarket/Build System/Setup Scene")]
    public static void SetupScene()
    {
      if (EditorApplication.isPlayingOrWillChangePlaymode)
      {
        Debug.LogWarning("BuildSystemSetup: exit play mode before running scene setup.");
        return;
      }

      EnsureFolder(MaterialFolder);
      EnsureFolder(PrefabFolder);
      EnsureFolder(DataFolder);

      Material gridLine = EnsureMaterial(
        MaterialFolder + "/Mat_BuildGrid.mat",
        "Sprites/Default",
        new Color(0.82f, 0.94f, 1f, 0.30f));

      Material highlightValid = EnsureMaterial(
        MaterialFolder + "/Mat_HighlightValid.mat",
        "Sprites/Default",
        new Color(0.25f, 0.95f, 0.45f, 0.35f));

      Material highlightInvalid = EnsureMaterial(
        MaterialFolder + "/Mat_HighlightInvalid.mat",
        "Sprites/Default",
        new Color(1f, 0.25f, 0.2f, 0.40f));

      Material ghostValid = EnsureMaterial(
        MaterialFolder + "/Mat_GhostValid.mat",
        "Legacy Shaders/Transparent/Diffuse",
        new Color(0.25f, 1f, 0.4f, 0.45f));

      Material ghostInvalid = EnsureMaterial(
        MaterialFolder + "/Mat_GhostInvalid.mat",
        "Legacy Shaders/Transparent/Diffuse",
        new Color(1f, 0.25f, 0.2f, 0.45f));

      Material shelf = EnsureMaterial(
        MaterialFolder + "/Mat_Shelf.mat",
        "Standard",
        new Color(0.52f, 0.63f, 0.76f));

      Material shelfFrame = EnsureMaterial(
        MaterialFolder + "/Mat_ShelfFrame.mat",
        "Standard",
        new Color(0.24f, 0.27f, 0.32f));

      Material counter = EnsureMaterial(
        MaterialFolder + "/Mat_Counter.mat",
        "Standard",
        new Color(0.76f, 0.62f, 0.44f));

      Material counterTop = EnsureMaterial(
        MaterialFolder + "/Mat_CounterTop.mat",
        "Standard",
        new Color(0.30f, 0.31f, 0.34f));

      Material plantPot = EnsureMaterial(
        MaterialFolder + "/Mat_PlantPot.mat",
        "Standard",
        new Color(0.48f, 0.30f, 0.22f));

      Material plantLeaf = EnsureMaterial(
        MaterialFolder + "/Mat_PlantLeaf.mat",
        "Standard",
        new Color(0.24f, 0.60f, 0.30f));

      GameObject shelfPrefab = EnsureShelfPrefab(shelf, shelfFrame);
      GameObject counterPrefab = EnsureCounterPrefab(counter, counterTop);
      GameObject plantPrefab = EnsurePlantPrefab(plantPot, plantLeaf);
      GameObject checkoutLanePrefab = EnsureCheckoutLanePrefab(counter, counterTop);

      UpgradeProfile checkoutLaneProfile = AssetDatabase.LoadAssetAtPath<UpgradeProfile>(
        "Assets/_ScriptableObjects/Game/Upgrades/UP_CheckoutLane.asset");

      PlaceableDefinition shelfDefinition = EnsureDefinition(
        DataFolder + "/Def_Shelf_2x1.asset",
        "shelf_2x1",
        "Shelf 2x1",
        shelfPrefab,
        new Vector2Int(2, 1),
        120,
        PlaceableArea.General,
        null);

      PlaceableDefinition counterDefinition = EnsureDefinition(
        DataFolder + "/Def_Counter_2x1.asset",
        "counter_2x1",
        "Counter 2x1",
        counterPrefab,
        new Vector2Int(2, 1),
        200,
        PlaceableArea.General,
        null);

      PlaceableDefinition plantDefinition = EnsureDefinition(
        DataFolder + "/Def_Plant_1x1.asset",
        "plant_1x1",
        "Plant 1x1",
        plantPrefab,
        Vector2Int.one,
        40,
        PlaceableArea.General,
        null);

      // Plan: checkout lanes are 4x3 and have five upgrade levels.
      PlaceableDefinition checkoutLaneDefinition = EnsureDefinition(
        DataFolder + "/Def_CheckoutLane_4x3.asset",
        "checkout_lane_4x3",
        "Checkout Lane 4x3",
        checkoutLanePrefab,
        new Vector2Int(4, 3),
        250,
        PlaceableArea.Checkout,
        checkoutLaneProfile);

      PlaceableCatalog catalog = EnsureCatalog(
        shelfDefinition, counterDefinition, plantDefinition, checkoutLaneDefinition);

      Scene scene = OpenTargetScene();
      if (!scene.IsValid())
      {
        Debug.LogError("BuildSystemSetup: could not open " + ScenePath);
        return;
      }

      GameObject root = GameObject.Find("BuildSystem");
      if (root == null)
      {
        root = new GameObject("BuildSystem");
        Undo.RegisterCreatedObjectUndo(root, "Create BuildSystem");
      }

      root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

      BuildGrid grid = GetOrAdd<BuildGrid>(root);
      EnsureBuildArea(grid);

      GameObject placedRoot = GetOrCreateChild(root.transform, "PlacedObjects");
      placedRoot.transform.localPosition = Vector3.zero;
      grid.placedRoot = placedRoot.transform;

      GameObject overlayObject = GetOrCreateChild(root.transform, "GridOverlay", typeof(GridOverlay));
      GridOverlay overlay = overlayObject.GetComponent<GridOverlay>();
      overlay.grid = grid;
      overlay.lineMaterial = gridLine;
      overlay.validMaterial = highlightValid;
      overlay.invalidMaterial = highlightInvalid;

      GameObject ghostObject = GetOrCreateChild(root.transform, "GhostPreview", typeof(BuildGhost));
      BuildGhost ghost = ghostObject.GetComponent<BuildGhost>();
      ghost.validMaterial = ghostValid;
      ghost.invalidMaterial = ghostInvalid;

      SimpleWallet wallet = GetOrAdd<SimpleWallet>(root);

      BuildLayoutStore store = GetOrAdd<BuildLayoutStore>(root);
      store.grid = grid;
      store.catalog = catalog;
      store.loadOnStart = true;
      store.saveOnChange = true;

      BuildController controller = GetOrAdd<BuildController>(root);
      controller.grid = grid;
      controller.catalog = catalog;
      controller.buildCamera = Camera.main;
      controller.ghost = ghost;
      controller.overlay = overlay;
      controller.economySource = wallet;

      GameObject uiObject = GetOrCreateChild(root.transform, "BuildUI", typeof(BuildMenuUI));
      BuildMenuUI menu = uiObject.GetComponent<BuildMenuUI>();
      menu.catalog = catalog;
      menu.controller = controller;
      menu.wallet = wallet;
      AuthoredUICreator.CreatePrefabs(false);
      menu.buildMenuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AuthoredUICreator.BuildMenuPrefabPath);
      controller.menu = menu;

      EnsureSectionVisuals();

      EditorUtility.SetDirty(grid);
      EditorUtility.SetDirty(overlay);
      EditorUtility.SetDirty(ghost);
      EditorUtility.SetDirty(store);
      EditorUtility.SetDirty(controller);
      EditorUtility.SetDirty(menu);
      EditorUtility.SetDirty(wallet);

      AssetDatabase.SaveAssets();
      EditorSceneManager.MarkSceneDirty(scene);
      EditorSceneManager.SaveScene(scene);
      AssetDatabase.Refresh();

      Selection.activeGameObject = root;

      Debug.Log(
        "Build system setup complete.\n" +
        "Play __Scene_0 and press B. LMB places, RMB removes, R rotates, Esc cancels.\n" +
        "Layout saves to PlayerPrefs key: " + store.StorageKey);
    }

    /// <summary>
    /// Fits the grid to the whole ShopCell floor and installs the default
    /// non-buildable section mask. Shared with final-game setup so rerunning
    /// either bootstrap keeps the build area in sync with the scene.
    /// </summary>
    public static void EnsureBuildArea(BuildGrid grid)
    {
      if (grid == null) return;

      grid.cellSize = CellSize;
      Bounds floor = ShopFloorBounds();

      if (floor.size.x <= 0f || floor.size.z <= 0f)
      {
        grid.size = new Vector2Int(16, 16);
        grid.origin = new Vector3(-8f, 0.75f, -8f);
      }
      else
      {
        grid.size = new Vector2Int(
          Mathf.Max(1, Mathf.RoundToInt(floor.size.x / CellSize)),
          Mathf.Max(1, Mathf.RoundToInt(floor.size.z / CellSize)));
        grid.origin = new Vector3(floor.min.x, floor.max.y, floor.min.z);
      }

      StoreBuildZones.ApplyTo(grid);
      EditorUtility.SetDirty(grid);
    }

    /// <summary>
    /// Applies the build-mode section treatment: colored floor overlays plus
    /// build-only barrier walls. Storage and deli keep their physical walls.
    /// Safe to rerun.
    /// </summary>
    [MenuItem("Tools/Supermarket/Build System/Apply Section Visuals")]
    public static void EnsureSectionVisuals()
    {
      if (EditorApplication.isPlayingOrWillChangePlaymode)
      {
        Debug.LogWarning("BuildSystemSetup: exit play mode before applying section visuals.");
        return;
      }

      Scene scene = SceneManager.GetActiveScene();
      if (scene.path != ScenePath)
      {
        scene = OpenTargetScene();
        if (!scene.IsValid())
        {
          Debug.LogError("BuildSystemSetup: could not open " + ScenePath);
          return;
        }
      }

      BuildGrid grid = UnityEngine.Object.FindFirstObjectByType<BuildGrid>();
      BuildController controller = UnityEngine.Object.FindFirstObjectByType<BuildController>();
      if (grid == null || controller == null)
      {
        Debug.LogError("BuildSystemSetup: build grid/controller not found; cannot apply section visuals.");
        return;
      }

      Material template = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Mat_BuildGrid.mat");
      Material barrierMaterial = EnsureMaterial(
        MaterialFolder + "/Mat_SectionBarrier.mat",
        "Legacy Shaders/Transparent/Diffuse",
        new Color(0.78f, 0.80f, 0.84f, 0.30f));

      GameObject root = GameObject.Find("BuildSystem");
      if (root == null) root = new GameObject("BuildSystem");

      GameObject overlayObject = GetOrCreateChild(root.transform, "SectionOverlay", typeof(SectionOverlay));
      SectionOverlay overlay = overlayObject.GetComponent<SectionOverlay>();
      overlay.grid = grid;
      overlay.controller = controller;
      overlay.templateMaterial = template;

      EnsureBarrier("Store/SectionWalls/ButcherWalls", controller, barrierMaterial);
      EnsureBarrier("Store/SectionWalls/BakeryWalls", controller, barrierMaterial);
      EnsureBarrier("Store/SectionWalls/AlcoholWalls", controller, barrierMaterial);
      EnsureBarrier("Store/SectionWalls/CheckoutWalls", controller, barrierMaterial);

      EditorUtility.SetDirty(overlay);
      EditorSceneManager.MarkSceneDirty(scene);
      EditorSceneManager.SaveScene(scene);
      AssetDatabase.SaveAssets();
      Debug.Log("Section visuals applied. Storage and deli keep physical walls; other sections use build-mode barriers.");
    }

    static void EnsureBarrier(string path, BuildController controller, Material barrierMaterial)
    {
      GameObject group = GameObject.Find(path);
      if (group == null)
      {
        Debug.LogWarning("BuildSystemSetup: section wall group not found: " + path);
        return;
      }

      BuildModeBarrier barrier = group.GetComponent<BuildModeBarrier>();
      if (barrier == null) barrier = group.AddComponent<BuildModeBarrier>();
      barrier.controller = controller;

      Collider[] colliders = group.GetComponentsInChildren<Collider>(true);
      for (int i = 0; i < colliders.Length; i++)
      {
        if (colliders[i] != null) UnityEngine.Object.DestroyImmediate(colliders[i]);
      }

      Renderer[] renderers = group.GetComponentsInChildren<Renderer>(true);
      for (int i = 0; i < renderers.Length; i++)
      {
        if (renderers[i] == null) continue;
        if (barrierMaterial != null) renderers[i].sharedMaterial = barrierMaterial;
        renderers[i].enabled = false;
      }

      EditorUtility.SetDirty(barrier);
      EditorUtility.SetDirty(group);
    }

    static Bounds ShopFloorBounds()
    {
      Bounds bounds = new Bounds();
      bool found = false;

      Collider[] colliders = UnityEngine.Object.FindObjectsByType<Collider>(
        FindObjectsInactive.Include, FindObjectsSortMode.None);

      for (int i = 0; i < colliders.Length; i++)
      {
        Collider collider = colliders[i];
        if (collider == null || !collider.gameObject.name.StartsWith("ShopCell_", StringComparison.Ordinal)) continue;

        if (!found)
        {
          bounds = collider.bounds;
          found = true;
        }
        else
        {
          bounds.Encapsulate(collider.bounds);
        }
      }

      return bounds;
    }

    [MenuItem("Tools/Supermarket/Build System/Clear Saved Layout")]
    public static void ClearSavedLayout()
    {
      BuildLayoutStore store = UnityEngine.Object.FindFirstObjectByType<BuildLayoutStore>();
      if (store == null)
      {
        Debug.Log("No BuildLayoutStore found in the open scene.");
        return;
      }

      string key = store.StorageKey;
      bool cleared = false;

      if (PlayerPrefs.HasKey(key))
      {
        PlayerPrefs.DeleteKey(key);
        cleared = true;
      }

      if (PlayerPrefs.HasKey(key + ".corrupt"))
      {
        PlayerPrefs.DeleteKey(key + ".corrupt");
        cleared = true;
      }

      if (cleared) PlayerPrefs.Save();

      string legacyPath = store.LegacyFilePath;
      if (File.Exists(legacyPath))
      {
        File.Delete(legacyPath);
        cleared = true;
      }

      Debug.Log(cleared
        ? "Cleared saved layout (key: " + key + ")."
        : "No saved layout found (key: " + key + ").");
    }

    [MenuItem("Tools/Supermarket/Build System/Reset Scene To Defaults")]
    public static void ResetSceneToDefaults()
    {
      if (EditorApplication.isPlayingOrWillChangePlaymode)
      {
        Debug.LogWarning("BuildSystemSetup: exit play mode before resetting the scene.");
        return;
      }

      Scene scene = SceneManager.GetActiveScene();
      if (scene.path != ScenePath)
      {
        scene = OpenTargetScene();
        if (!scene.IsValid())
        {
          Debug.LogError("BuildSystemSetup: could not open " + ScenePath);
          return;
        }
      }

      // Clear persisted build layout (Editor/PlayerPrefs). WebGL build save settings are unchanged.
      BuildLayoutStore store = UnityEngine.Object.FindFirstObjectByType<BuildLayoutStore>();
      if (store != null)
      {
        string key = store.StorageKey;
        if (PlayerPrefs.HasKey(key)) PlayerPrefs.DeleteKey(key);
        if (PlayerPrefs.HasKey(key + ".corrupt")) PlayerPrefs.DeleteKey(key + ".corrupt");
        string legacyPath = store.LegacyFilePath;
        if (File.Exists(legacyPath)) File.Delete(legacyPath);
      }
      else
      {
        string defaultKey = "supermarket.build-layout." + BuildLayoutStore.DefaultFileName;
        if (PlayerPrefs.HasKey(defaultKey)) PlayerPrefs.DeleteKey(defaultKey);
        if (PlayerPrefs.HasKey(defaultKey + ".corrupt")) PlayerPrefs.DeleteKey(defaultKey + ".corrupt");
      }

      // Clear persisted game state (Editor/PlayerPrefs). WebGL build save settings are unchanged.
      if (PlayerPrefs.HasKey(Supermarket.Core.GameSave.SaveKey)) PlayerPrefs.DeleteKey(Supermarket.Core.GameSave.SaveKey);
      PlayerPrefs.Save();

      Supermarket.Core.GameManager game = UnityEngine.Object.FindFirstObjectByType<Supermarket.Core.GameManager>();
      Supermarket.Economy.Economy economy = UnityEngine.Object.FindFirstObjectByType<Supermarket.Economy.Economy>();
      GameConfig config = ResolveGameConfig(economy);

      if (game != null)
      {
        if (game.config == null && config != null)
        {
          SerializedObject gameSo = new SerializedObject(game);
          SerializedProperty cfgProp = gameSo.FindProperty("config");
          if (cfgProp != null) cfgProp.objectReferenceValue = config;
          gameSo.ApplyModifiedPropertiesWithoutUndo();
        }
        game.ResetAll();
        EditorUtility.SetDirty(game);
      }

      // Ensure economy is reset even if GameManager didn't handle it as expected.
      if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<Supermarket.Economy.Economy>();
      if (economy != null)
      {
        if (economy.config == null && config != null) economy.config = config;
        int startingMoney = 10000;
        if (config != null && config.editorStartingMoney > 0) startingMoney = config.editorStartingMoney;
        economy.Restore(startingMoney, 0f, 0f, 0f, 0f);

        SerializedObject ecoSo = new SerializedObject(economy);
        SerializedProperty sb = ecoSo.FindProperty("startingBalance");
        if (sb != null) sb.intValue = startingMoney;
        ecoSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(economy);
      }

      if (game == null)
      {
        BuildGrid grid = UnityEngine.Object.FindFirstObjectByType<BuildGrid>();
        if (grid != null)
        {
          grid.ClearAll();
          EditorUtility.SetDirty(grid);
        }
      }

      Supermarket.Core.GameSave save = UnityEngine.Object.FindFirstObjectByType<Supermarket.Core.GameSave>();
      if (save != null)
      {
        // ResetAll() may have written a fresh default save; remove it so Editor starts clean.
        if (PlayerPrefs.HasKey(Supermarket.Core.GameSave.SaveKey)) PlayerPrefs.DeleteKey(Supermarket.Core.GameSave.SaveKey);
        PlayerPrefs.Save();
        EditorUtility.SetDirty(save);
      }

      SimpleWallet[] wallets = UnityEngine.Object.FindObjectsByType<SimpleWallet>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      for (int i = 0; i < wallets.Length; i++)
      {
        SimpleWallet w = wallets[i];
        if (w == null) continue;
        if (w is Supermarket.Economy.Economy) continue; // already handled
        SerializedObject wSo = new SerializedObject(w);
        SerializedProperty sb = wSo.FindProperty("startingBalance");
        if (sb != null) sb.intValue = 10000;
        wSo.ApplyModifiedPropertiesWithoutUndo();
        w.Initialize(10000);
        EditorUtility.SetDirty(w);
      }

      EditorSceneManager.MarkSceneDirty(scene);
      EditorSceneManager.SaveScene(scene);
      Debug.Log("Scene reset to defaults: bank amount and store layout cleared.");
    }

    static GameConfig ResolveGameConfig(Supermarket.Economy.Economy economy)
    {
      if (economy != null && economy.config != null) return economy.config;

      Supermarket.Core.GameManager game = UnityEngine.Object.FindFirstObjectByType<Supermarket.Core.GameManager>();
      if (game != null && game.config != null) return game.config;

      string[] guids = AssetDatabase.FindAssets("t:Supermarket.Config.GameConfig");
      if (guids != null && guids.Length > 0)
      {
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<GameConfig>(path);
      }

      return null;
    }

    [MenuItem("Tools/Supermarket/Build System/Open Saved Layout Folder")]
    public static void OpenSavedLayoutFolder()
    {
      string folder = Application.persistentDataPath;
      if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
      EditorUtility.RevealInFinder(folder);
    }

    static Scene OpenTargetScene()
    {
      if (!File.Exists(ScenePath))
      {
        Debug.LogError("BuildSystemSetup: scene not found: " + ScenePath);
        return default(Scene);
      }

      Scene active = EditorSceneManager.GetActiveScene();
      if (active.path == ScenePath) return active;

      if (active.isDirty) EditorSceneManager.SaveOpenScenes();
      return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    static void EnsureFolder(string path)
    {
      if (AssetDatabase.IsValidFolder(path)) return;

      string parent = Path.GetDirectoryName(path);
      string name = Path.GetFileName(path);
      if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;

      parent = parent.Replace('\\', '/');
      if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
      AssetDatabase.CreateFolder(parent, name);
    }

    static Material EnsureMaterial(string path, string shaderName, Color color)
    {
      Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
      if (existing != null) return existing;

      Shader shader = Shader.Find(shaderName);
      if (shader == null)
      {
        Debug.LogError("BuildSystemSetup: shader not found: " + shaderName);
        return null;
      }

      Material material = new Material(shader);
      material.color = color;
      AssetDatabase.CreateAsset(material, path);
      return material;
    }

    static GameObject EnsureShelfPrefab(Material body, Material frame)
    {
      string path = PrefabFolder + "/Shelf_2x1.prefab";
      GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
      if (existing != null) return existing;

      GameObject root = new GameObject("Shelf_2x1");
      root.AddComponent<PlacedObject>();
      BoxCollider box = root.AddComponent<BoxCollider>();
      box.center = new Vector3(0f, 0.9f, 0f);
      box.size = new Vector3(2f, 1.8f, 1f);

      Cube(root.transform, "Back", new Vector3(0f, 0.9f, -0.46f), new Vector3(1.96f, 1.72f, 0.06f), body);

      for (int ix = -1; ix <= 1; ix += 2)
      {
        for (int iz = -1; iz <= 1; iz += 2)
        {
          Cube(
            root.transform,
            "Post",
            new Vector3(ix * 0.94f, 0.9f, iz * 0.42f),
            new Vector3(0.08f, 1.8f, 0.08f),
            frame);
        }
      }

      for (int i = 0; i < 3; i++)
      {
        Cube(
          root.transform,
          "Board",
          new Vector3(0f, 0.35f + i * 0.55f, 0f),
          new Vector3(1.9f, 0.06f, 0.9f),
          body);
      }

      return SavePrefab(root, path);
    }

    static GameObject EnsureCounterPrefab(Material body, Material top)
    {
      string path = PrefabFolder + "/Counter_2x1.prefab";
      GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
      if (existing != null) return existing;

      GameObject root = new GameObject("Counter_2x1");
      root.AddComponent<PlacedObject>();
      BoxCollider box = root.AddComponent<BoxCollider>();
      box.center = new Vector3(0f, 0.52f, 0f);
      box.size = new Vector3(2f, 1.04f, 1f);

      Cube(root.transform, "Body", new Vector3(0f, 0.45f, 0f), new Vector3(1.9f, 0.9f, 0.9f), body);
      Cube(root.transform, "Top", new Vector3(0f, 0.95f, 0f), new Vector3(2f, 0.1f, 1f), top);
      Cube(root.transform, "Register", new Vector3(0.55f, 1.2f, 0.05f), new Vector3(0.4f, 0.3f, 0.35f), top);

      return SavePrefab(root, path);
    }

    static GameObject EnsureCheckoutLanePrefab(Material body, Material top)
    {
      string path = PrefabFolder + "/CheckoutLane_4x3.prefab";
      GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
      if (existing != null) return existing;

      GameObject root = new GameObject("CheckoutLane_4x3");
      root.AddComponent<PlacedObject>();
      BoxCollider box = root.AddComponent<BoxCollider>();
      box.center = new Vector3(0f, 0.52f, 0f);
      box.size = new Vector3(4f, 1.04f, 3f);

      Cube(root.transform, "Body", new Vector3(0f, 0.45f, 0f), new Vector3(3.9f, 0.9f, 2.9f), body);
      Cube(root.transform, "Top", new Vector3(0f, 0.95f, 0f), new Vector3(4f, 0.1f, 3f), top);
      Cube(root.transform, "Register", new Vector3(1.35f, 1.2f, 0f), new Vector3(0.5f, 0.35f, 0.45f), top);

      return SavePrefab(root, path);
    }

    static GameObject EnsurePlantPrefab(Material pot, Material leaves)
    {
      string path = PrefabFolder + "/Plant_1x1.prefab";
      GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
      if (existing != null) return existing;

      GameObject root = new GameObject("Plant_1x1");
      root.AddComponent<PlacedObject>();
      BoxCollider box = root.AddComponent<BoxCollider>();
      box.center = new Vector3(0f, 0.6f, 0f);
      box.size = new Vector3(0.7f, 1.2f, 0.7f);

      GameObject potObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
      potObject.name = "Pot";
      potObject.transform.SetParent(root.transform, false);
      potObject.transform.localPosition = new Vector3(0f, 0.2f, 0f);
      potObject.transform.localScale = new Vector3(0.45f, 0.2f, 0.45f);
      StripColliderAndSetMaterial(potObject, pot);

      GameObject leafObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
      leafObject.name = "Leaves";
      leafObject.transform.SetParent(root.transform, false);
      leafObject.transform.localPosition = new Vector3(0f, 0.75f, 0f);
      leafObject.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
      StripColliderAndSetMaterial(leafObject, leaves);

      return SavePrefab(root, path);
    }

    static GameObject SavePrefab(GameObject root, string path)
    {
      GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
      UnityEngine.Object.DestroyImmediate(root);
      return prefab;
    }

    static GameObject Cube(
      Transform parent,
      string name,
      Vector3 localPosition,
      Vector3 localScale,
      Material material)
    {
      GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
      go.name = name;
      go.transform.SetParent(parent, false);
      go.transform.localPosition = localPosition;
      go.transform.localScale = localScale;
      StripColliderAndSetMaterial(go, material);
      return go;
    }

    static void StripColliderAndSetMaterial(GameObject go, Material material)
    {
      Collider collider = go.GetComponent<Collider>();
      if (collider != null) UnityEngine.Object.DestroyImmediate(collider);

      Renderer renderer = go.GetComponent<Renderer>();
      if (renderer != null && material != null) renderer.sharedMaterial = material;
    }

    static PlaceableDefinition EnsureDefinition(
      string path,
      string id,
      string displayName,
      GameObject prefab,
      Vector2Int footprint,
      int cost,
      PlaceableArea allowedArea,
      UpgradeProfile upgradeProfile)
    {
      PlaceableDefinition definition = AssetDatabase.LoadAssetAtPath<PlaceableDefinition>(path);
      if (definition == null)
      {
        definition = ScriptableObject.CreateInstance<PlaceableDefinition>();
        AssetDatabase.CreateAsset(definition, path);
        definition.id = id;
        definition.displayName = displayName;
        definition.prefab = prefab;
        definition.footprint = footprint;
        definition.cost = cost;
        definition.rotatable = true;
      }
      else
      {
        if (definition.prefab == null) definition.prefab = prefab;
        if (string.IsNullOrEmpty(definition.id)) definition.id = id;
      }

      definition.allowedArea = allowedArea;
      if (upgradeProfile != null) definition.upgradeProfile = upgradeProfile;

      EditorUtility.SetDirty(definition);
      return definition;
    }

    static PlaceableCatalog EnsureCatalog(params PlaceableDefinition[] definitions)
    {
      string path = DataFolder + "/PlaceableCatalog.asset";
      PlaceableCatalog catalog = AssetDatabase.LoadAssetAtPath<PlaceableCatalog>(path);
      if (catalog == null)
      {
        catalog = ScriptableObject.CreateInstance<PlaceableCatalog>();
        AssetDatabase.CreateAsset(catalog, path);
      }

      for (int i = 0; i < definitions.Length; i++)
      {
        PlaceableDefinition definition = definitions[i];
        if (definition != null && !catalog.definitions.Contains(definition))
        {
          catalog.definitions.Add(definition);
        }
      }

      EditorUtility.SetDirty(catalog);
      catalog.RebuildLookup();
      return catalog;
    }

    static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
      T component = gameObject.GetComponent<T>();
      return component != null ? component : gameObject.AddComponent<T>();
    }

    static GameObject GetOrCreateChild(Transform parent, string name, params Type[] components)
    {
      Transform existing = parent.Find(name);
      GameObject go = existing != null ? existing.gameObject : new GameObject(name);
      if (existing == null) go.transform.SetParent(parent, false);

      for (int i = 0; i < components.Length; i++)
      {
        if (go.GetComponent(components[i]) == null) go.AddComponent(components[i]);
      }

      return go;
    }
  }
}
