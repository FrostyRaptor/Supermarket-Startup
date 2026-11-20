using System;
using UnityEngine;
using Supermarket.Config;
using Supermarket.Core;

namespace Supermarket.Progression
{
    public enum LicenseStage
    {
        NotStarted,
        Stage1Pending,
        Stage2Pending,
        Approved,
        /// <summary>Background check passed; the final paperwork can now be filed.</summary>
        Stage1Approved
    }

    /// <summary>
    /// Two-stage liquor license application. Stage 1 failure repeats stage 1;
    /// stage 2 failure sends the player back to the beginning and both fees are lost.
    /// </summary>
    public class LiquorLicense : MonoBehaviour
    {
        public GameConfig config;
        public NotificationCenter notices;

        /// <summary>Injectable RNG so EditMode tests can be deterministic.</summary>
        public Func<float> RandomValue = delegate { return UnityEngine.Random.value; };

        public LicenseStage Stage { get; private set; }
        public int DueDay { get { return _dueDay; } private set { _dueDay = value; } }
        public bool WillFail { get { return _willFail; } }
        public bool IsApproved { get { return Stage == LicenseStage.Approved; } }

        int _dueDay;

        bool _willFail;

        public bool CanFileStage1 { get { return Stage == LicenseStage.NotStarted; } }
        public bool CanFileStage2 { get { return Stage == LicenseStage.Stage1Approved; } }

        public int Stage1Fee { get { return config == null ? 0 : config.licenseStage1Fee; } }
        public int Stage2Fee { get { return config == null ? 0 : config.licenseStage2Fee; } }

        public string StatusText
        {
            get
            {
                switch (Stage)
                {
                    case LicenseStage.Approved: return "Approved";
                    case LicenseStage.Stage1Pending: return "Background check - due day " + DueDay;
                    case LicenseStage.Stage1Approved: return "Background check approved - file final paperwork";
                    case LicenseStage.Stage2Pending: return "Final paperwork - due day " + DueDay;
                    default: return "Not applied";
                }
            }
        }

        public bool TryFileStage1(Economy.Economy wallet, int currentBusinessDay)
        {
            if (!CanFileStage1 || wallet == null || !wallet.CanAfford(Stage1Fee)) return false;
            wallet.TrySpend(Stage1Fee);
            Stage = LicenseStage.Stage1Pending;
            DueDay = currentBusinessDay + RollDays(config.licenseStage1MinDays, config.licenseStage1MaxDays, 3, 10);
            _willFail = RandomValue() < (config == null ? 0.2f : config.licenseStage1FailChance);
            Notify("Liquor license background check filed. Response expected around day " + DueDay + ".", NoticeKind.Info);
            return true;
        }

        public bool TryFileStage2(Economy.Economy wallet, int currentBusinessDay)
        {
            if (!CanFileStage2 || wallet == null || !wallet.CanAfford(Stage2Fee)) return false;
            wallet.TrySpend(Stage2Fee);
            Stage = LicenseStage.Stage2Pending;
            DueDay = currentBusinessDay + RollDays(config.licenseStage2MinDays, config.licenseStage2MaxDays, 5, 20);
            _willFail = RandomValue() < (config == null ? 0.25f : config.licenseStage2FailChance);
            Notify("Liquor license paperwork filed. Response expected around day " + DueDay + ".", NoticeKind.Info);
            return true;
        }

        public void OnBusinessDay(int businessDay)
        {
            if (Stage != LicenseStage.Stage1Pending && Stage != LicenseStage.Stage2Pending) return;
            if (businessDay < DueDay) return;

            if (Stage == LicenseStage.Stage1Pending)
            {
                if (_willFail)
                {
                    Stage = LicenseStage.NotStarted;
                    _willFail = false;
                    Notify("Liquor license background check failed. The fee was lost; you can refile.", NoticeKind.Warning);
                }
                else
                {
                    Stage = LicenseStage.Stage1Approved;
                    Notify("Background check approved. File the final paperwork to continue.", NoticeKind.Success);
                }
                return;
            }

            if (_willFail)
            {
                Stage = LicenseStage.NotStarted;
                _willFail = false;
                Notify("Liquor license application failed. Both fees were lost; restart the process.", NoticeKind.Warning);
            }
            else
            {
                Stage = LicenseStage.Approved;
                Notify("Liquor license approved! You can now purchase the alcohol section.", NoticeKind.Success);
            }
        }

        int RollDays(int min, int max, int fallbackMin, int fallbackMax)
        {
            if (max < min) { int t = min; min = max; max = t; }
            if (max <= 0) { min = fallbackMin; max = fallbackMax; }
            float roll = Mathf.Clamp01(RandomValue());
            return min + Mathf.RoundToInt(roll * (max - min));
        }

        public void Restore(LicenseStage stage, int dueDay, bool willFail)
        {
            Stage = stage;
            DueDay = dueDay;
            _willFail = willFail;
        }

        void Notify(string message, NoticeKind kind)
        {
            if (notices != null) notices.Notify(message, kind);
        }
    }
}
