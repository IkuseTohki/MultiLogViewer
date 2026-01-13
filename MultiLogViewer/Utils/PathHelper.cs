using System;
using System.IO;

namespace MultiLogViewer.Utils
{
    /// <summary>
    /// パス関連のユーティリティクラス
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// アプリケーションのベースディレクトリを取得します。
        /// PublishSingleFile=true の環境下でも、一時ディレクトリではなく
        /// 実行ファイル(.exe)が存在する正しいディレクトリを返します。
        /// </summary>
        /// <returns>ベースディレクトリのパス</returns>
        public static string GetBaseDirectory()
        {
            // .NET 6以降、PublishSingleFile環境下では AppDomain.CurrentDomain.BaseDirectory が
            // 一時展開ディレクトリを指す可能性があるため、Environment.ProcessPath を優先して使用する。
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath))
            {
                return Path.GetDirectoryName(processPath) ?? AppDomain.CurrentDomain.BaseDirectory;
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
