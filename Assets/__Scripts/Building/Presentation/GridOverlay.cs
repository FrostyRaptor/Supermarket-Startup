using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Supermarket.Building
{
  /// <summary>
  /// Runtime-generated grid lines and footprint highlight. Both meshes live
  /// under this object's transform and are toggled with build mode.
  /// </summary>
  public class GridOverlay : MonoBehaviour
  {
    [Header("References")]
    public BuildGrid grid;

    /// <summary>Grid abstraction for overlay queries (falls back to <see cref="grid"/>).</summary>
    public IGridOccupancy Grid
    {
      get { return grid; }
    }

    [Header("Materials")]
    public Material lineMaterial;
    public Material validMaterial;
    public Material invalidMaterial;

    [Header("Look")]
    public float lineWidth = 0.016f;
    public float heightOffset = 0.01f;
    public float cellInset = 0.03f;

    GameObject _gridObject;
    GameObject _highlightObject;
    MeshFilter _gridFilter;
    MeshFilter _highlightFilter;
    MeshRenderer _gridRenderer;
    MeshRenderer _highlightRenderer;
    Mesh _gridMesh;
    Mesh _highlightMesh;

    readonly List<Vector3> _vertices = new List<Vector3>();
    readonly List<int> _triangles = new List<int>();
    readonly List<GridCoord> _cells = new List<GridCoord>();

    bool _visible;
    bool _highlightVisible;
    PlaceableDefinition _lastDefinition;
    GridCoord _lastOrigin;
    int _lastTurns = -1;
    bool _lastValid;

    void Start()
    {
      CreateRenderers();
      RebuildGrid();
      SetVisible(_visible);
    }

    void CreateRenderers()
    {
      if (_gridObject != null) return;

      _gridObject = CreateMeshObject("Build Grid Lines", lineMaterial, out _gridFilter, out _gridRenderer);
      _gridMesh = new Mesh { name = "Build Grid Mesh" };
      _gridFilter.sharedMesh = _gridMesh;

      _highlightObject = CreateMeshObject("Build Grid Highlight", validMaterial, out _highlightFilter, out _highlightRenderer);
      _highlightMesh = new Mesh { name = "Build Grid Highlight Mesh" };
      _highlightFilter.sharedMesh = _highlightMesh;
      _highlightObject.SetActive(false);
    }

    GameObject CreateMeshObject(string name, Material material, out MeshFilter filter, out MeshRenderer renderer)
    {
      GameObject go = new GameObject(name);
      go.transform.SetParent(transform, false);

      filter = go.AddComponent<MeshFilter>();
      renderer = go.AddComponent<MeshRenderer>();
      renderer.sharedMaterial = material;
      renderer.shadowCastingMode = ShadowCastingMode.Off;
      renderer.receiveShadows = false;
      renderer.lightProbeUsage = LightProbeUsage.Off;
      renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
      return go;
    }

    public void RebuildGrid()
    {
      IGridOccupancy g = Grid;
      if (g == null || _gridMesh == null) return;

      transform.SetPositionAndRotation(
        new Vector3(g.Origin.x, g.Origin.y + heightOffset, g.Origin.z),
        Quaternion.identity);

      _vertices.Clear();
      _triangles.Clear();

      float width = g.Width * g.CellSize;
      float depth = g.Depth * g.CellSize;
      float half = Mathf.Max(0.002f, lineWidth) * 0.5f;

      for (int x = 0; x <= g.Width; x++)
      {
        float px = x * g.CellSize;
        AddQuad(
          new Vector3(px - half, 0f, 0f),
          new Vector3(px - half, 0f, depth),
          new Vector3(px + half, 0f, depth),
          new Vector3(px + half, 0f, 0f));
      }

      for (int z = 0; z <= g.Depth; z++)
      {
        float pz = z * g.CellSize;
        AddQuad(
          new Vector3(0f, 0f, pz - half),
          new Vector3(0f, 0f, pz + half),
          new Vector3(width, 0f, pz + half),
          new Vector3(width, 0f, pz - half));
      }

      ApplyMesh(_gridMesh, _vertices, _triangles);
    }

    public void SetHighlight(PlaceableDefinition definition, GridCoord origin, int turns, bool valid)
    {
      if (_highlightMesh == null) return;

      turns = FootprintUtility.NormalizeTurns(turns);

      if (!_highlightVisible ||
          _lastDefinition != definition ||
          _lastOrigin != origin ||
          _lastTurns != turns ||
          _lastValid != valid)
      {
        _lastDefinition = definition;
        _lastOrigin = origin;
        _lastTurns = turns;
        _lastValid = valid;

        _vertices.Clear();
        _triangles.Clear();
        _cells.Clear();

        if (definition != null)
        {
          FootprintUtility.GetCellOffsets(definition.Footprint, turns, _cells);

          IGridOccupancy g = Grid;
          float size = g != null ? g.CellSize : 1f;
          float inset = Mathf.Min(cellInset, size * 0.15f);

          for (int i = 0; i < _cells.Count; i++)
          {
            GridCoord cell = origin + _cells[i];
            float x0 = cell.X * size + inset;
            float x1 = (cell.X + 1) * size - inset;
            float z0 = cell.Z * size + inset;
            float z1 = (cell.Z + 1) * size - inset;

            AddQuad(
              new Vector3(x0, 0.002f, z0),
              new Vector3(x0, 0.002f, z1),
              new Vector3(x1, 0.002f, z1),
              new Vector3(x1, 0.002f, z0));
          }
        }

        ApplyMesh(_highlightMesh, _vertices, _triangles);
        _highlightVisible = definition != null && _cells.Count > 0;
      }

      if (_highlightRenderer != null)
      {
        _highlightRenderer.sharedMaterial = valid ? validMaterial : invalidMaterial;
      }

      if (_highlightObject != null)
      {
        _highlightObject.SetActive(_visible && _highlightVisible);
      }
    }

    public void ClearHighlight()
    {
      _highlightVisible = false;
      _lastDefinition = null;
      _lastOrigin = new GridCoord(int.MinValue, int.MinValue);
      _lastTurns = -1;
      _lastValid = false;

      _cells.Clear();
      if (_highlightObject != null) _highlightObject.SetActive(false);
    }

    public void SetVisible(bool visible)
    {
      _visible = visible;
      if (_gridObject != null) _gridObject.SetActive(visible);
      if (_highlightObject != null) _highlightObject.SetActive(visible && _highlightVisible);
    }

    void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
      int start = _vertices.Count;
      _vertices.Add(a);
      _vertices.Add(b);
      _vertices.Add(c);
      _vertices.Add(d);

      _triangles.Add(start);
      _triangles.Add(start + 1);
      _triangles.Add(start + 2);
      _triangles.Add(start);
      _triangles.Add(start + 2);
      _triangles.Add(start + 3);
    }

    static void ApplyMesh(Mesh mesh, List<Vector3> vertices, List<int> triangles)
    {
      mesh.Clear();
      if (vertices.Count == 0) return;

      mesh.SetVertices(vertices);
      mesh.SetTriangles(triangles, 0);
      mesh.RecalculateBounds();
    }

    void OnDestroy()
    {
      if (_gridMesh != null)
      {
        if (Application.isPlaying) Destroy(_gridMesh);
        else DestroyImmediate(_gridMesh);
      }

      if (_highlightMesh != null)
      {
        if (Application.isPlaying) Destroy(_highlightMesh);
        else DestroyImmediate(_highlightMesh);
      }
    }
  }
}
