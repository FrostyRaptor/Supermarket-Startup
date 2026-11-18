using System.Collections.Generic;
using NUnit.Framework;
using Supermarket.Building;

namespace Supermarket.Tests
{
  public class BuildMenuUITests
  {
    [Test]
    public void FindButtonIndex_MapsCatalogIndex()
    {
      List<int> mapping = new List<int> { 0, 1, 2 };

      Assert.AreEqual(0, BuildMenuUI.FindButtonIndex(mapping, 0));
      Assert.AreEqual(1, BuildMenuUI.FindButtonIndex(mapping, 1));
      Assert.AreEqual(2, BuildMenuUI.FindButtonIndex(mapping, 2));
    }

    [Test]
    public void FindButtonIndex_SkipsNullCatalogEntries()
    {
      List<int> mapping = new List<int> { 0, 2, 3 };

      Assert.AreEqual(1, BuildMenuUI.FindButtonIndex(mapping, 2));
      Assert.AreEqual(2, BuildMenuUI.FindButtonIndex(mapping, 3));
    }

    [Test]
    public void FindButtonIndex_ReturnsMinusOneForUnknownOrEmpty()
    {
      List<int> mapping = new List<int> { 0, 2 };

      Assert.AreEqual(-1, BuildMenuUI.FindButtonIndex(mapping, 5));
      Assert.AreEqual(-1, BuildMenuUI.FindButtonIndex(mapping, -1));
      Assert.AreEqual(-1, BuildMenuUI.FindButtonIndex(null, 0));
    }
  }
}
