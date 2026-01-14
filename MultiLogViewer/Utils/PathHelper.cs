using System;
using System.IO;
using System.Diagnostics;

namespace MultiLogViewer.Utils
{
    /// <summary>
    /// パス関連のユーティリティクラス
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// アプリケーションのベースディレクトリを取得します。
        /// PublishSingleFile=true の環境下でも、一時展開ディレクトリではなく
        /// 実行ファイル(.exe)が存在する正しいディレクトリを返します。
        /// </summary>
        /// <returns>ベースディレクトリのパス</returns>
        public static string GetBaseDirectory()
        {
            // .NET Core 3.1 互換対応: Environment.ProcessPath は .NET 6 以降のため使用できない。
            // 代わりに Process.GetCurrentProcess().MainModule.FileName を使用する。
            // これにより PublishSingleFile=true の環境でも正しいパスが取得できる。
            string? processPath = null;
            try
            {
                using var process = Process.GetCurrentProcess();
                processPath = process.MainModule?.FileName;
            }
            catch
            {
                // アクセス権限等で取得できない場合は無視してBaseDirectoryへのフォールバックに任せる
            }

            if (!string.IsNullOrEmpty(processPath))
            {
                return Path.GetDirectoryName(processPath) ?? AppDomain.CurrentDomain.BaseDirectory;
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
