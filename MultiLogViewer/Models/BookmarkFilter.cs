using System;

namespace MultiLogViewer.Models
{
    /// <summary>
    /// ブックマークフィルターを表すクラスです。
    /// LogFilter を継承し、WPF のテンプレート切り替えに使用します。
    /// </summary>
    public class BookmarkFilter : LogFilter
    {
        public BookmarkFilter() : base(FilterType.Bookmark, "", default, "Bookmark")
        {
        }

        // ラベル機能追加時に色などの情報をここに追加可能
    }
}
