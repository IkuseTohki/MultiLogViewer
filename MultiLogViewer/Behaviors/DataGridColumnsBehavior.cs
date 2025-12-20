using MultiLogViewer.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MultiLogViewer.Behaviors
{
    public static class DataGridColumnsBehavior
    {
        public static readonly DependencyProperty BindableColumnsProperty =
            DependencyProperty.RegisterAttached(
                "BindableColumns",
                typeof(ObservableCollection<DisplayColumnConfig>),
                typeof(DataGridColumnsBehavior),
                new PropertyMetadata(null, OnBindableColumnsChanged));

        public static ObservableCollection<DisplayColumnConfig> GetBindableColumns(DependencyObject obj)
        {
            return (ObservableCollection<DisplayColumnConfig>)obj.GetValue(BindableColumnsProperty);
        }

        public static void SetBindableColumns(DependencyObject obj, ObservableCollection<DisplayColumnConfig> value)
        {
            obj.SetValue(BindableColumnsProperty, value);
        }

        private static void OnBindableColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                if (e.OldValue is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= (sender, args) => OnCollectionChanged(sender, args, dataGrid);
                }

                if (e.NewValue is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged += (sender, args) => OnCollectionChanged(sender, args, dataGrid);
                }

                // Initial population of columns
                GenerateColumns(dataGrid, e.NewValue as ObservableCollection<DisplayColumnConfig>);
            }
        }

        private static void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e, DataGrid dataGrid)
        {
            // Regenerate columns when the collection changes
            GenerateColumns(dataGrid, sender as ObservableCollection<DisplayColumnConfig>);
        }

        private static void GenerateColumns(DataGrid dataGrid, ObservableCollection<DisplayColumnConfig>? columns)
        {
            dataGrid.Columns.Clear();
            if (columns == null)
            {
                return;
            }

            foreach (var columnConfig in columns)
            {
                DataGridColumn newColumn;

                if (columnConfig.BindingPath == "Message")
                {
                    // Message列の場合はテンプレートを使用（1行表示 + アイコン）
                    var template = dataGrid.TryFindResource("MultilineMessageTemplate") as DataTemplate;
                    if (template != null)
                    {
                        newColumn = new DataGridTemplateColumn
                        {
                            Header = columnConfig.Header,
                            Width = new DataGridLength(columnConfig.Width),
                            MinWidth = columnConfig.Width,
                            CellTemplate = template,
                            SortMemberPath = columnConfig.BindingPath
                        };
                    }
                    else
                    {
                        newColumn = CreateTextColumn(columnConfig);
                    }
                }
                else
                {
                    newColumn = CreateTextColumn(columnConfig);
                }

                // --- ヘッダーメニューの設定 (カラムフィルター) ---
                var keyName = ExtractKeyFromBindingPath(columnConfig.BindingPath);
                if (!string.IsNullOrEmpty(keyName))
                {
                    var headerMenu = new ContextMenu();
                    var headerItem = new MenuItem
                    {
                        Header = "拡張フィルターに追加",
                        Command = (dataGrid.DataContext as dynamic)?.AddExtensionFilterCommand,
                        CommandParameter = keyName
                    };
                    headerMenu.Items.Add(headerItem);

                    // 既存のヘッダースタイルを継承しつつContextMenuを追加
                    var baseHeaderStyle = Application.Current.FindResource(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader)) as Style;
                    var headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader), baseHeaderStyle);
                    headerStyle.Setters.Add(new Setter(FrameworkElement.ContextMenuProperty, headerMenu));
                    newColumn.HeaderStyle = headerStyle;
                }

                // --- セルメニューの設定 (日時フィルターなど) ---
                if (columnConfig.BindingPath == "Timestamp")
                {
                    var cellStyle = new Style(typeof(DataGridCell));
                    var contextMenu = new ContextMenu();

                    var afterItem = new MenuItem { Header = "この日時以降をフィルターに追加" };
                    // SourceにdataGridを直接指定することで確実にViewModelのコマンドにバインドする
                    afterItem.SetBinding(MenuItem.CommandProperty, new Binding("DataContext.AddDateTimeFilterCommand") { Source = dataGrid });
                    afterItem.SetBinding(MenuItem.CommandParameterProperty, new Binding(".") { Converter = new DateTimeFilterConverter(), ConverterParameter = true });
                    contextMenu.Items.Add(afterItem);

                    var beforeItem = new MenuItem { Header = "この日時以前をフィルターに追加" };
                    beforeItem.SetBinding(MenuItem.CommandProperty, new Binding("DataContext.AddDateTimeFilterCommand") { Source = dataGrid });
                    beforeItem.SetBinding(MenuItem.CommandParameterProperty, new Binding(".") { Converter = new DateTimeFilterConverter(), ConverterParameter = false });
                    contextMenu.Items.Add(beforeItem);

                    cellStyle.Setters.Add(new Setter(DataGridCell.ContextMenuProperty, contextMenu));
                    newColumn.CellStyle = cellStyle;
                }

                dataGrid.Columns.Add(newColumn);
            }
        }
        // セルのデータ（LogEntry）から特定の値（DateTime）を取り出し、フラグとセットでValueTupleにするための内部コンバーター
        private class DateTimeFilterConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value is LogEntry entry && parameter is bool isAfter)
                {
                    return (entry.Timestamp, isAfter);
                }
                return null!;
            }
            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
        }
        private static string? ExtractKeyFromBindingPath(string bindingPath)
        {
            if (string.IsNullOrEmpty(bindingPath)) return null;
            if (bindingPath == "Timestamp" || bindingPath == "Message" || bindingPath == "FileName" || bindingPath == "LineNumber") return null;

            if (bindingPath.StartsWith("AdditionalData[") && bindingPath.EndsWith("]"))
            {
                return bindingPath.Substring(15, bindingPath.Length - 16);
            }
            return null;
        }

        private static DataGridTextColumn CreateTextColumn(DisplayColumnConfig columnConfig)
        {
            var binding = new Binding(columnConfig.BindingPath)
            {
                Mode = BindingMode.OneWay
            };

            if (!string.IsNullOrEmpty(columnConfig.StringFormat))
            {
                binding.StringFormat = columnConfig.StringFormat;
            }

            return new DataGridTextColumn
            {
                Header = columnConfig.Header,
                Width = new DataGridLength(columnConfig.Width),
                MinWidth = columnConfig.Width,
                Binding = binding,
                SortMemberPath = columnConfig.BindingPath
            };
        }
    }
}
