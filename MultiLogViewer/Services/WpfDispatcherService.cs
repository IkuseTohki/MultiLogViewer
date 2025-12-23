using System;
using System.Windows;

namespace MultiLogViewer.Services
{
    public class WpfDispatcherService : IDispatcherService
    {
        public void Invoke(Action action)
        {
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(action);
            }
            else
            {
                // Application.Current が null の場合（テスト時など）、
                // または Dispatcher が利用できない場合は、
                // 同期的に実行するか、何もしないかを決める必要があります。
                // テスト時にモックを使わずにここを通るケースを考慮し、
                // そのまま実行してしまいます（コンソールアプリ的な挙動）。
                action();
            }
        }
    }
}
