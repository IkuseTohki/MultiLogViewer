using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MultiLogViewer.Behaviors
{
    /// <summary>
    /// DataGrid の標準コピーコマンドを任意の ICommand へリダイレクトする挙動を提供します。
    /// </summary>
    public static class DataGridCopyBehavior
    {
        public static readonly DependencyProperty CopyCommandProperty =
            DependencyProperty.RegisterAttached(
                "CopyCommand",
                typeof(ICommand),
                typeof(DataGridCopyBehavior),
                new PropertyMetadata(null, OnCopyCommandChanged));

        public static ICommand GetCopyCommand(DependencyObject obj) => (ICommand)obj.GetValue(CopyCommandProperty);
        public static void SetCopyCommand(DependencyObject obj, ICommand value) => obj.SetValue(CopyCommandProperty, value);

        private static void OnCopyCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                // 既存のバインディングをクリアして新しく追加
                dataGrid.CommandBindings.Clear();
                if (e.NewValue is ICommand command)
                {
                    var binding = new CommandBinding(ApplicationCommands.Copy, (s, args) =>
                    {
                        if (command.CanExecute(args.Parameter))
                        {
                            command.Execute(args.Parameter);
                            args.Handled = true;
                        }
                    });
                    dataGrid.CommandBindings.Add(binding);
                }
            }
        }
    }
}
