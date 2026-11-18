using System;
using System.Collections.Generic;

namespace Supermarket.Building
{
  [Serializable]
  public class BuildLayout
  {
    public int version = 1;
    public List<BuildLayoutRecord> objects = new List<BuildLayoutRecord>();

    [Serializable]
    public class BuildLayoutRecord
    {
      public string id;
      public int x;
      public int z;
      public int rotation;
    }
  }
}
