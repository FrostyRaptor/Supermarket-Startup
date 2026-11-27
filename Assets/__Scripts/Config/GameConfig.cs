using UnityEngine;

namespace Supermarket.Config
{
  /// <summary>
  /// Global, editor-tunable numbers for the supermarket simulation.
  /// All balance values live here or in the other config ScriptableObjects.
  /// </summary>
  [CreateAssetMenu(menuName = "Supermarket/Game Config", fileName = "GameConfig")]
  public class GameConfig : ScriptableObject
  {
    [Header("Time")]
    [Tooltip("Real minutes for one in-game business day.")]
    public float realMinutesPerDay = 0.1f;
    public int startMonth = 1;
    public int startDay = 1;
    public int startYear = 2026;

    [Header("Economy")]
    [Tooltip("Starting money for new saves in player builds (WebGL).")]
    public int startingMoney = 1000;
    [Tooltip("Starting money when running/resetting in the Unity Editor.")]
    public int editorStartingMoney = 10000;

    [Header("Demand")]
    [Tooltip("Units sold per product per day at level 1 with average staffing.")]
    public float baseDailyDemand = 18f;
    public float demandGrowthPerDay = 0.35f;
    [Tooltip("Order suggestions are this multiple of projected demand.")]
    public float suggestionFactor = 1.12f;
    [Tooltip("Customers a single checkout employee can serve per day per lane level.")]
    public int checkoutCustomersPerEmployee = 14;

    [Header("Liquor License")]
    public int licenseStage1Fee = 400;
    public int licenseStage1MinDays = 3;
    public int licenseStage1MaxDays = 10;
    [Range(0f, 1f)] public float licenseStage1FailChance = 0.2f;
    public int licenseStage2Fee = 1200;
    public int licenseStage2MinDays = 5;
    public int licenseStage2MaxDays = 20;
    [Range(0f, 1f)] public float licenseStage2FailChance = 0.25f;

    public float DayLengthSeconds
    {
      get { return Mathf.Max(1f, realMinutesPerDay * 60f); }
    }
  }
}
