using System;

namespace MultiLogViewer.Services
{
    /// <summary>
    /// システム時刻を提供するサービスのインターフェース。
    /// テスト時に時刻を固定するために使用します。
    /// </summary>
    public interface ITimeProvider
    {
        DateTime Now { get; }
    }

    /// <summary>
    /// 標準のシステム時刻を提供するクラス。
    /// </summary>
    public class SystemTimeProvider : ITimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}
