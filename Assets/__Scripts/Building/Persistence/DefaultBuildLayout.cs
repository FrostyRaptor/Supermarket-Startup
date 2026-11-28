using System;
using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Building
{
  /// <summary>
  /// Wire format for the shipped default store layout JSON. The layout uses
  /// prefab IDs and degree rotations so it can be authored outside Unity;
  /// <see cref="BuildLayoutStore"/> converts it to runtime grid placements.
  /// </summary>
  [Serializable]
  public class DefaultBuildLayoutFile
  {
    public int version = 1;
    public string scene;
    public DefaultBuildGridSize gridSize;
    public List<DefaultBuildLayoutItem> items = new List<DefaultBuildLayoutItem>();
  }

  [Serializable]
  public class DefaultBuildGridSize
  {
    public int width;
    public int height;
  }

  [Serializable]
  public class DefaultBuildLayoutItem
  {
    public string id;
    public string prefabId;
    public string category;
    public DefaultBuildGridPoint gridOrigin;
    public DefaultBuildItemSize size;
    public int rotation;
  }

  [Serializable]
  public class DefaultBuildGridPoint
  {
    public int x;
    public int z;
  }

  [Serializable]
  public class DefaultBuildItemSize
  {
    public int width;
    public int depth;
  }
}
