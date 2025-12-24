using System.Windows;
using System.Windows.Controls;

namespace MultiLogViewer.Behaviors
{
    /// <summary>
    /// ボタンの左クリックでコンテキストメニューを開くための添付プロパティを提供します。
    /// </summary>
    public static class ButtonContextMenuBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(ButtonContextMenuBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button button)
            {
                if ((bool)e.NewValue)
                {
                    button.Click += Button_Click;
                }
                else
                {
                    button.Click -= Button_Click;
                }
            }
        }

        private static void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}
