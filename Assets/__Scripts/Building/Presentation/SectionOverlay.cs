using System.Collections.Generic;
using Supermarket.Config;
using UnityEngine;
using UnityEngine.Rendering;

namespace Supermarket.Building
{
  /// <summary>
  /// Build-mode-only floor overlay for the store sections.
  ///
  /// The general buildable area gets a green outline, checkout gets a purple
  /// outline, and non-modifiable sections get a gray fill, a red outline and a
  /// centered name label. Geometry is generated once from the grid's exclusion
  /// zones, so this does not change placement permissions.
  /// </summary>
  public class SectionOverlay : MonoBehaviour
  {
    [Header("References")]
    public BuildGrid grid;
    public BuildController controller;

    [Header("Materials")]
    [Tooltip("Template used to create the overlay materials at runtime. Mat_BuildGrid is a good default.")]
    public Material templateMaterial;

    [Header("Colors")]
    public Color generalOutlineColor = new Color(0.22f, 0.95f, 0.38f, 0.95f);
    public Color checkoutOutlineColor = new Color(0.62f, 0.30f, 0.98f, 0.95f);
    public Color blockedOutlineColor = new Color(1f, 0.22f, 0.18f, 0.95f);
    public Color blockedFillColor = new Color(0.16f, 0.16f, 0.19f, 0.58f);

    [Header("Look")]
    public float outlineWidth = 0.5f;
    public float fillHeight = 0.02f;
    public float outlineHeight = 0.026f;
    public float labelHeight = 0.045f;
    public int labelFontSize = 128;
    public float labelCharacterSize = 0.4f;
    public Color labelColor = new Color(1f, 0.82f, 0.80f, 1f);

    enum VisualKind
    {
      Outside,
      General,
      Checkout,
      Blocked
    }

    readonly List<Vector3> _fillVertices = new List<Vector3>();
    readonly List<int> _fillTriangles = new List<int>();
    readonly List<Vector3> _generalVertices = new List<Vector3>();
    readonly List<int> _generalTriangles = new List<int>();
    readonly List<Vector3> _checkoutVertices = new List<Vector3>();
    readonly List<int> _checkoutTriangles = new List<int>();
    readonly List<Vector3> _blockedVertices = new List<Vector3>();
    readonly List<int> _blockedTriangles = new List<int>();
    readonly List<TextMesh> _labels = new List<TextMesh>();
    readonly List<Material> _runtimeMaterials = new List<Material>();

    GameObject _visualRoot;
    Mesh _fillMesh;
    Mesh _generalMesh;
    Mesh _checkoutMesh;
    Mesh _blockedMesh;
    Material _fillMaterial;
    Material _generalMaterial;
    Material _checkoutMaterial;
    Material _blockedMaterial;
    Font _font;
    bool _built;
    bool _subscribed;

    void Awake()
    {
      ResolveReferences();
      CreateMaterials();
      Build();
    }

    void Start()
    {
      ResolveReferences();
      Build();
      Subscribe();
      ApplyVisibility();
    }

    void OnEnable()
    {
      Subscribe();
      ApplyVisibility();
    }

    void OnDisable()
    {
      Unsubscribe();
    }

    void OnDestroy()
    {
      Unsubscribe();
      DestroyRuntimeObjects();
    }

    void ResolveReferences()
    {
      if (grid == null) grid = FindFirstObjectByType<BuildGrid>();
      if (controller == null) controller = FindFirstObjectByType<BuildController>();
    }

    void Subscribe()
    {
      ResolveReferences();
      if (_subscribed || controller == null) return;

      controller.StateChanged += ApplyVisibility;
      _subscribed = true;
    }

    void Unsubscribe()
    {
      if (!_subscribed) return;
      if (controller != null) controller.StateChanged -= ApplyVisibility;
      _subscribed = false;
    }

    void ApplyVisibility()
    {
      bool visible = controller != null && controller.IsInBuildMode;
      if (_visualRoot != null) _visualRoot.SetActive(visible);
    }

    void CreateMaterials()
    {
      _fillMaterial = CreateOverlayMaterial(blockedFillColor);
      _generalMaterial = CreateOverlayMaterial(generalOutlineColor);
      _checkoutMaterial = CreateOverlayMaterial(checkoutOutlineColor);
      _blockedMaterial = CreateOverlayMaterial(blockedOutlineColor);
    }

    Material CreateOverlayMaterial(Color color)
    {
      Material material;
      if (templateMaterial != null)
      {
        material = new Material(templateMaterial);
      }
      else
      {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
        if (shader == null)
        {
          Debug.LogWarning("SectionOverlay: no transparent shader found; section overlay will be hidden.");
          return null;
        }

        material = new Material(shader);
      }

      material.name = "Section Overlay (" + color + ")";
      material.color = color;
      if (material.HasProperty("_Color")) material.SetColor("_Color", color);
      _runtimeMaterials.Add(material);
      return material;
    }

    void Build()
    {
      if (_built) return;
      if (grid == null) return;
      _built = true;

      _visualRoot = new GameObject("Section Visuals");
      _visualRoot.transform.SetParent(transform, false);
      _visualRoot.transform.SetPositionAndRotation(
        new Vector3(grid.Origin.x, grid.Origin.y, grid.Origin.z),
        Quaternion.identity);

      _fillMesh = CreateMeshObject("Blocked Fill", _fillMaterial);
      _generalMesh = CreateMeshObject("General Outline", _generalMaterial);
      _checkoutMesh = CreateMeshObject("Checkout Outline", _checkoutMaterial);
      _blockedMesh = CreateMeshObject("Blocked Outline", _blockedMaterial);

      BuildGeometry();

      _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
      BuildLabels();

      _visualRoot.SetActive(false);
    }

    Mesh CreateMeshObject(string name, Material material)
    {
      GameObject go = new GameObject(name);
      go.transform.SetParent(_visualRoot.transform, false);

      MeshFilter filter = go.AddComponent<MeshFilter>();
      MeshRenderer renderer = go.AddComponent<MeshRenderer>();
      renderer.sharedMaterial = material;
      renderer.shadowCastingMode = ShadowCastingMode.Off;
      renderer.receiveShadows = false;
      renderer.lightProbeUsage = LightProbeUsage.Off;
      renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

      Mesh mesh = new Mesh { name = name + " Mesh" };
      filter.sharedMesh = mesh;
      return mesh;
    }

    void BuildGeometry()
    {
      int width = grid.Width;
      int depth = grid.Depth;
      float cell = grid.CellSize;
      float widthLimit = Mathf.Max(0.01f, cell * 0.45f);
      float outline = Mathf.Clamp(outlineWidth, 0.01f, widthLimit);

      VisualKind[] kinds = new VisualKind[width * depth];
      StoreSectionType[] sections = new StoreSectionType[width * depth];

      for (int z = 0; z < depth; z++)
      {
        for (int x = 0; x < width; x++)
        {
          int index = z * width + x;
          StoreSectionType section;
          kinds[index] = ClassifyCell(x, z, out section);
          sections[index] = section;
        }
      }

      BuildBlockedFill(kinds, width, depth, cell);
      BuildOutlines(kinds, sections, width, depth, cell, outline);

      ApplyMesh(_fillMesh, _fillVertices, _fillTriangles);
      ApplyMesh(_generalMesh, _generalVertices, _generalTriangles);
      ApplyMesh(_checkoutMesh, _checkoutVertices, _checkoutTriangles);
      ApplyMesh(_blockedMesh, _blockedVertices, _blockedTriangles);
    }

    VisualKind ClassifyCell(int x, int z, out StoreSectionType section)
    {
      section = StoreSectionType.General;

      if (x < 0 || z < 0 || x >= grid.Width || z >= grid.Depth) return VisualKind.Outside;

      Vector3 world = grid.CoordToWorldCenter(new GridCoord(x, z));
      List<BuildExclusionZone> zones = grid.exclusionZones;

      for (int i = 0; i < zones.Count; i++)
      {
        BuildExclusionZone zone = zones[i];
        if (zone == null || !zone.ContainsWorld(world)) continue;

        section = zone.section;
        return zone.access == BuildZoneAccess.CheckoutOnly
          ? VisualKind.Checkout
          : VisualKind.Blocked;
      }

      return VisualKind.General;
    }

    void BuildBlockedFill(VisualKind[] kinds, int width, int depth, float cell)
    {
      _fillVertices.Clear();
      _fillTriangles.Clear();

      for (int z = 0; z < depth; z++)
      {
        int runStart = -1;
        for (int x = 0; x <= width; x++)
        {
          bool blocked = x < width && kinds[z * width + x] == VisualKind.Blocked;
          if (blocked && runStart < 0) runStart = x;

          if (!blocked && runStart >= 0)
          {
            AddRect(
              _fillVertices,
              _fillTriangles,
              runStart * cell,
              z * cell,
              x * cell,
              (z + 1) * cell,
              fillHeight);
            runStart = -1;
          }
        }
      }
    }

    void BuildOutlines(
      VisualKind[] kinds,
      StoreSectionType[] sections,
      int width,
      int depth,
      float cell,
      float outline)
    {
      _generalVertices.Clear();
      _generalTriangles.Clear();
      _checkoutVertices.Clear();
      _checkoutTriangles.Clear();
      _blockedVertices.Clear();
      _blockedTriangles.Clear();

      for (int z = 0; z < depth; z++)
      {
        for (int x = 0; x < width; x++)
        {
          int index = z * width + x;
          VisualKind kind = kinds[index];
          if (kind == VisualKind.Outside) continue;

          List<Vector3> vertices = OutlineVertices(kind);
          List<int> triangles = OutlineTriangles(kind);
          StoreSectionType section = sections[index];

          float x0 = x * cell;
          float x1 = (x + 1) * cell;
          float z0 = z * cell;
          float z1 = (z + 1) * cell;

          if (NeedsEdge(kinds, sections, width, depth, x - 1, z, kind, section))
          {
            AddRect(vertices, triangles, x0, z0, x0 + outline, z1, outlineHeight);
          }

          if (NeedsEdge(kinds, sections, width, depth, x + 1, z, kind, section))
          {
            AddRect(vertices, triangles, x1 - outline, z0, x1, z1, outlineHeight);
          }

          if (NeedsEdge(kinds, sections, width, depth, x, z - 1, kind, section))
          {
            AddRect(vertices, triangles, x0, z0, x1, z0 + outline, outlineHeight);
          }

          if (NeedsEdge(kinds, sections, width, depth, x, z + 1, kind, section))
          {
            AddRect(vertices, triangles, x0, z1 - outline, x1, z1, outlineHeight);
          }
        }
      }
    }

    static bool NeedsEdge(
      VisualKind[] kinds,
      StoreSectionType[] sections,
      int width,
      int depth,
      int x,
      int z,
      VisualKind kind,
      StoreSectionType section)
    {
      if (x < 0 || z < 0 || x >= width || z >= depth) return true;

      int index = z * width + x;
      VisualKind other = kinds[index];
      if (other != kind) return true;

      return kind == VisualKind.Blocked && sections[index] != section;
    }

    List<Vector3> OutlineVertices(VisualKind kind)
    {
      return kind == VisualKind.General ? _generalVertices
        : kind == VisualKind.Checkout ? _checkoutVertices
        : _blockedVertices;
    }

    List<int> OutlineTriangles(VisualKind kind)
    {
      return kind == VisualKind.General ? _generalTriangles
        : kind == VisualKind.Checkout ? _checkoutTriangles
        : _blockedTriangles;
    }

    void BuildLabels()
    {
      if (_font == null)
      {
        Debug.LogWarning("SectionOverlay: built-in font missing; section labels are disabled.");
        return;
      }

      int sectionCount = System.Enum.GetValues(typeof(StoreSectionType)).Length;
      float[] bestArea = new float[sectionCount];
      Vector2[] centers = new Vector2[sectionCount];
      List<BuildExclusionZone> zones = grid.exclusionZones;

      for (int i = 0; i < zones.Count; i++)
      {
        BuildExclusionZone zone = zones[i];
        if (zone == null || !zone.IsBlocked) continue;

        float area = (zone.max.x - zone.min.x) * (zone.max.y - zone.min.y);
        int index = (int)zone.section;
        if (area <= bestArea[index]) continue;

        bestArea[index] = area;
        centers[index] = (zone.min + zone.max) * 0.5f;
      }

      for (int i = 0; i < sectionCount; i++)
      {
        if (bestArea[i] <= 0f) continue;

        StoreSectionType section = (StoreSectionType)i;
        Vector3 local = new Vector3(
          centers[i].x - grid.Origin.x,
          labelHeight,
          centers[i].y - grid.Origin.z);

        CreateLabel(section.ToString().ToUpperInvariant(), local);
      }
    }

    void CreateLabel(string text, Vector3 localPosition)
    {
      GameObject go = new GameObject("Label " + text);
      go.transform.SetParent(_visualRoot.transform, false);
      go.transform.localPosition = localPosition;
      // Flat text viewed from above is mirrored by Unity's left-handed basis;
      // flipping X keeps the label upright and readable while it lies on the floor.
      go.transform.localScale = new Vector3(-1f, 1f, 1f);

      TextMesh textMesh = go.AddComponent<TextMesh>();
      textMesh.font = _font;
      textMesh.fontSize = labelFontSize;
      textMesh.characterSize = labelCharacterSize;
      textMesh.anchor = TextAnchor.MiddleCenter;
      textMesh.alignment = TextAlignment.Center;
      textMesh.color = labelColor;
      textMesh.text = text;

      MeshRenderer renderer = go.GetComponent<MeshRenderer>();
      renderer.sharedMaterial = _font.material;
      renderer.shadowCastingMode = ShadowCastingMode.Off;
      renderer.receiveShadows = false;
      renderer.lightProbeUsage = LightProbeUsage.Off;
      renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

      _labels.Add(textMesh);
    }

    void LateUpdate()
    {
      if (_labels.Count == 0 || _visualRoot == null || !_visualRoot.activeSelf) return;

      // Lay every label along the store's long axis instead of following the
      // camera. TextMesh faces its local +Z, so pointing that up puts the text
      // flat on the floor; the up hint decides which way the letters run. The
      // negative X scale in CreateLabel keeps them readable from above.
      Vector3 upHint = grid != null && grid.Depth > grid.Width
        ? Vector3.right
        : Vector3.forward;
      Quaternion rotation = Quaternion.LookRotation(Vector3.up, upHint);
      for (int i = 0; i < _labels.Count; i++)
      {
        if (_labels[i] != null) _labels[i].transform.rotation = rotation;
      }
    }

    static void AddRect(
      List<Vector3> vertices,
      List<int> triangles,
      float x0,
      float z0,
      float x1,
      float z1,
      float y)
    {
      int start = vertices.Count;
      vertices.Add(new Vector3(x0, y, z0));
      vertices.Add(new Vector3(x0, y, z1));
      vertices.Add(new Vector3(x1, y, z1));
      vertices.Add(new Vector3(x1, y, z0));

      triangles.Add(start);
      triangles.Add(start + 1);
      triangles.Add(start + 2);
      triangles.Add(start);
      triangles.Add(start + 2);
      triangles.Add(start + 3);
    }

    static void ApplyMesh(Mesh mesh, List<Vector3> vertices, List<int> triangles)
    {
      if (mesh == null) return;

      mesh.Clear();
      if (vertices.Count == 0) return;

      mesh.SetVertices(vertices);
      mesh.SetTriangles(triangles, 0);
      mesh.RecalculateBounds();
    }

    void DestroyRuntimeObjects()
    {
      DestroyMesh(_fillMesh);
      DestroyMesh(_generalMesh);
      DestroyMesh(_checkoutMesh);
      DestroyMesh(_blockedMesh);

      for (int i = 0; i < _runtimeMaterials.Count; i++)
      {
        Material material = _runtimeMaterials[i];
        if (material == null) continue;
        if (Application.isPlaying) Destroy(material);
        else DestroyImmediate(material);
      }
      _runtimeMaterials.Clear();
    }

    static void DestroyMesh(Mesh mesh)
    {
      if (mesh == null) return;
      if (Application.isPlaying) Destroy(mesh);
      else DestroyImmediate(mesh);
    }
  }
}
