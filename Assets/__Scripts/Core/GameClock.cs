using System;
using UnityEngine;
using Supermarket.Config;

namespace Supermarket.Core
{
    /// <summary>
    /// Fixed, unstoppable day clock. One in-game day is a configurable number of
    /// real minutes (10 by default). Every day counts as a business day.
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        public GameConfig config;

        float _elapsedSeconds;
        int _day = 1;

        public event Action<int> DayChanged;

        public int Day { get { return _day; } }
        public int BusinessDay { get { return _day; } }
        public float ElapsedSeconds { get { return _elapsedSeconds; } }
        public float DayProgress
        {
            get { return config == null ? 0f : Mathf.Clamp01(_elapsedSeconds / config.DayLengthSeconds); }
        }

        public DateTime CurrentDate
        {
            get
            {
                if (config == null) return new DateTime(2026, 1, 1);
                return new DateTime(config.startYear, config.startMonth, config.startDay).AddDays(_day - 1);
            }
        }

        public string DateText { get { return CurrentDate.ToString("MM/dd/yyyy"); } }

        void Update()
        {
            if (config == null) return;
            _elapsedSeconds += Time.unscaledDeltaTime;

            while (_elapsedSeconds >= config.DayLengthSeconds)
            {
                _elapsedSeconds -= config.DayLengthSeconds;
                _day++;
                if (DayChanged != null) DayChanged(_day);
            }
        }

        public void AdvanceDay()
        {
            _day++;
            _elapsedSeconds = 0f;
            if (DayChanged != null) DayChanged(_day);
        }

        public void Restore(int day, float elapsedSeconds)
        {
            _day = Mathf.Max(1, day);
            _elapsedSeconds = Mathf.Max(0f, elapsedSeconds);
        }
    }
}
