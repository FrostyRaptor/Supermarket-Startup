using System;
using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Core
{
    public enum NoticeKind { Info, Warning, Success }

    [Serializable]
    public class Notice
    {
        public string message;
        public NoticeKind kind;
        public float time;

        public Notice(string message, NoticeKind kind)
        {
            this.message = message;
            this.kind = kind;
            time = Time.unscaledTime;
        }
    }

    /// <summary>Central place for staffing, licensing and economy notices.</summary>
    public class NotificationCenter : MonoBehaviour
    {
        public static NotificationCenter Instance { get; private set; }

        public const int MaxRecent = 20;

        readonly List<Notice> _recent = new List<Notice>();

        public event Action<string, NoticeKind> NoticeRaised;
        public IReadOnlyList<Notice> Recent { get { return _recent; } }

        void Awake()
        {
            Instance = this;
        }

        public void Notify(string message, NoticeKind kind = NoticeKind.Info)
        {
            if (string.IsNullOrEmpty(message)) return;
            _recent.Add(new Notice(message, kind));
            while (_recent.Count > MaxRecent) _recent.RemoveAt(0);
            if (NoticeRaised != null) NoticeRaised(message, kind);
        }

        public void Clear()
        {
            _recent.Clear();
        }
    }
}
