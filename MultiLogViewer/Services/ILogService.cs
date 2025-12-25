using MultiLogViewer.Models;
using System.Collections.Generic;

namespace MultiLogViewer.Services
{
    /// <summary>
    /// ログデータの取得と管理を担当するサービスのインターフェースです。
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// 指定された設定ファイルに基づいてログを一括で読み込みます。
        /// </summary>
        /// <param name="configPath">設定ファイルのパス。</param>
        /// <returns>読み込まれたログエントリ、表示列設定、ファイル状態などを含む結果オブジェクト。</returns>
        LogDataResult LoadFromConfig(string configPath);

        /// <summary>
        /// 指定された設定ファイルと現在のファイル状態に基づいて、追加分のログをインクリメンタルに読み込みます。
        /// </summary>
        /// <param name="configPath">設定ファイルのパス。</param>
        /// <param name="currentStates">前回の読み込み時のファイル状態一覧。</param>
        /// <param name="startSequenceNumber">割り当てを開始するシーケンス番号の開始値。</param>
        /// <returns>新規追加されたログエントリと更新されたファイル状態を含む結果オブジェクト。</returns>
        LogDataResult LoadIncremental(string configPath, List<FileState> currentStates, long startSequenceNumber);
    }
}
