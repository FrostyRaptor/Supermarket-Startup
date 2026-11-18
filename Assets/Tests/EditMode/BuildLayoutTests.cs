using NUnit.Framework;
using Supermarket.Building;
using UnityEngine;

namespace Supermarket.Tests
{
  public class BuildLayoutTests
  {
    [Test]
    public void JsonRoundTrip_PreservesRecords()
    {
      BuildLayout layout = new BuildLayout();
      layout.objects.Add(new BuildLayout.BuildLayoutRecord
      {
        id = "shelf_2x1",
        x = 3,
        z = 4,
        rotation = 1
      });
      layout.objects.Add(new BuildLayout.BuildLayoutRecord
      {
        id = "plant_1x1",
        x = 7,
        z = 2,
        rotation = 3
      });

      string json = JsonUtility.ToJson(layout, true);

      BuildLayout restored = JsonUtility.FromJson<BuildLayout>(json);

      Assert.IsNotNull(restored);
      Assert.IsNotNull(restored.objects);
      Assert.AreEqual(layout.version, restored.version);
      Assert.AreEqual(2, restored.objects.Count);

      Assert.AreEqual("shelf_2x1", restored.objects[0].id);
      Assert.AreEqual(3, restored.objects[0].x);
      Assert.AreEqual(4, restored.objects[0].z);
      Assert.AreEqual(1, restored.objects[0].rotation);

      Assert.AreEqual("plant_1x1", restored.objects[1].id);
      Assert.AreEqual(7, restored.objects[1].x);
      Assert.AreEqual(2, restored.objects[1].z);
      Assert.AreEqual(3, restored.objects[1].rotation);
    }

    [Test]
    public void EmptyLayout_RoundTrips()
    {
      string json = JsonUtility.ToJson(new BuildLayout(), true);
      BuildLayout restored = JsonUtility.FromJson<BuildLayout>(json);

      Assert.IsNotNull(restored);
      Assert.AreEqual(1, restored.version);
      Assert.AreEqual(0, restored.objects.Count);
    }
  }
}
