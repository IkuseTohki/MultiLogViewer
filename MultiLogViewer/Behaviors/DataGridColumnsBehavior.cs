using MultiLogViewer.Models;
using MultiLogViewer.ViewModels;
using MultiLogViewer.ViewModels.Converters;
using MultiLogViewer.Utils;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace MultiLogViewer.Behaviors
{
    /// <summary>
    /// DataGrid の列を動的に生成するための添付プロパティを提供する挙動クラスです。
    /// </summary>
    public static class DataGridColumnsBehavior
    {
        private const string AdditionalDataPrefix = "AdditionalData[";

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

                GenerateColumns(dataGrid, e.NewValue as ObservableCollection<DisplayColumnConfig>);
            }
        }

        private static void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e, DataGrid dataGrid)
        {
            GenerateColumns(dataGrid, sender as ObservableCollection<DisplayColumnConfig>);
        }

        /// <summary>
        /// 設定に基づいて DataGrid の列を再生成します。
        /// </summary>
        private static void GenerateColumns(DataGrid dataGrid, ObservableCollection<DisplayColumnConfig>? columns)
        {
            dataGrid.Columns.Clear();
            if (columns == null) return;

            foreach (var config in columns)
            {
                DataGridColumn column = CreateColumn(dataGrid, config);

                // 共通のスタイル適用 (背景色、文字色、太字)
                ApplyColumnStyle(column, config);

                // ヘッダーのコンテキストメニュー (拡張フィルター用)
                ApplyHeaderContextMenu(dataGrid, column, config);

                dataGrid.Columns.Add(column);
            }
        }

        /// <summary>
        /// 列の種類に応じた DataGridColumn を生成します。
        /// </summary>
        private static DataGridColumn CreateColumn(DataGrid dataGrid, DisplayColumnConfig config)
        {
            if (config.IsBookmark)
            {
                return CreateBookmarkColumn(dataGrid, config);
            }

            if (string.Equals(config.BindingPath, "Message", StringComparison.OrdinalIgnoreCase))
            {
                return CreateMessageColumn(dataGrid, config);
            }

            if (string.Equals(config.BindingPath, "Timestamp", StringComparison.OrdinalIgnoreCase))
            {
                return CreateTimestampColumn(dataGrid, config);
            }

            return CreateStandardTextColumn(config);
        }

        private static DataGridColumn CreateBookmarkColumn(DataGrid dataGrid, DisplayColumnConfig config)
        {
            var template = dataGrid.TryFindResource("BookmarkIconTemplate") as DataTemplate;
            var column = new DataGridTemplateColumn
            {
                Header = config.Header,
                Width = new DataGridLength(config.Width),
                MinWidth = config.Width,
                CellTemplate = template,
                CanUserResize = false
            };

            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new EventSetter(Control.MouseDoubleClickEvent, new MouseButtonEventHandler(OnCellDoubleClick)));
            cellStyle.Setters.Add(new Setter(DataGridCell.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));

            // ブックマーク専用コンテキストメニュー
            var contextMenu = new ContextMenu();
            var boolToVis = Application.Current.FindResource("BooleanToVisibilityConverter") as IValueConverter;
            var boolInvert = Application.Current.FindResource("BooleanInvertConverter") as IValueConverter;

            var filterItem = new MenuItem { Header = "拡張フィルターに追加" };
            filterItem.SetBinding(MenuItem.CommandProperty, new Binding("DataContext.AddBookmarkFilterCommand") { Source = dataGrid });
            filterItem.SetBinding(MenuItem.CommandParameterProperty, new Binding("BookmarkColor"));
            filterItem.SetBinding(MenuItem.VisibilityProperty, new Binding("IsBookmarked") { Converter = boolToVis });
            filterItem.SetBinding(MenuItem.IsEnabledProperty, new Binding("DataContext.IsTailEnabled") { Source = dataGrid, Converter = boolInvert });
            contextMenu.Items.Add(filterItem);

            var clearItem = new MenuItem { Header = "ブックマークをすべて解除" };
            clearItem.SetBinding(MenuItem.CommandProperty, new Binding("DataContext.ClearBookmarksCommand") { Source = dataGrid });
            contextMenu.Items.Add(clearItem);

            var separator = new Separator();
            separator.SetBinding(Separator.VisibilityProperty, new Binding("IsBookmarked") { Converter = boolToVis });
            contextMenu.Items.Add(separator);

            var colorMenu = new MenuItem { Header = "色を変更" };
            colorMenu.SetBinding(MenuItem.VisibilityProperty, new Binding("IsBookmarked") { Converter = boolToVis });
            AddColorItems(colorMenu, dataGrid);
            contextMenu.Items.Add(colorMenu);

            cellStyle.Setters.Add(new Setter(DataGridCell.ContextMenuProperty, contextMenu));
            column.CellStyle = cellStyle;

            return column;
        }

        private static void AddColorItems(MenuItem parent, DataGrid dataGrid)
        {
            void Add(string name, BookmarkColor color, string hex)
            {
                var item = new MenuItem { Header = name };
                item.SetBinding(MenuItem.CommandProperty, new Binding("DataContext.SetBookmarkColorCommand") { Source = dataGrid });
                item.CommandParameter = color;
                item.Icon = new System.Windows.Shapes.Rectangle { Width = 12, Height = 12, Fill = (Brush)new BrushConverter().ConvertFromString(hex)! };
                parent.Items.Add(item);
            }
            Add("Red", BookmarkColor.Red, "#E57373");
            Add("Blue", BookmarkColor.Blue, "#64B5F6");
            Add("Green", BookmarkColor.Green, "#81C784");
            Add("Yellow", BookmarkColor.Yellow, "#FFF176");
        }

        private static DataGridColumn CreateMessageColumn(DataGrid dataGrid, DisplayColumnConfig config)
        {
            var template = dataGrid.TryFindResource("MultilineMessageTemplate") as DataTemplate;
            if (template == null) return CreateStandardTextColumn(config);

            return new DataGridTemplateColumn
            {
                Header = config.Header,
                Width = new DataGridLength(config.Width),
                MinWidth = config.Width,
                CellTemplate = template,
                SortMemberPath = config.BindingPath
            };
        }

        private static DataGridColumn CreateTimestampColumn(DataGrid dataGrid, DisplayColumnConfig config)
        {
            var column = CreateStandardTextColumn(config);
            var cellStyle = new Style(typeof(DataGridCell));
            var contextMenu = new ContextMenu();
            var dtConverter = Application.Current.FindResource("DateTimeFilterConverter") as IValueConverter;
            var boolInvert = Application.Current.FindResource("BooleanInvertConverter") as IValueConverter;

            var afterItem = new MenuItem { Header = "この日時以降をフィルターに追加" };
            afterItem.SetBinding(MenuItem.CommandProperty, new Binding("DataContext.AddDateTimeFilterCommand") { Source = dataGrid });
            afterItem.SetBinding(MenuItem.CommandParameterProperty, new Binding(".") { Converter = dtConverter, ConverterParameter = true });
            afterItem.SetBinding(MenuItem.IsEnabledProperty, new Binding("DataContext.IsTailEnabled") { Source = dataGrid, Converter = boolInvert });
            contextMenu.Items.Add(afterItem);

            var beforeItem = new MenuItem { Header = "この日時以前をフィルターに追加" };
            beforeItem.SetBinding(MenuItem.CommandProperty, new Binding("DataContext.AddDateTimeFilterCommand") { Source = dataGrid });
            beforeItem.SetBinding(MenuItem.CommandParameterProperty, new Binding(".") { Converter = dtConverter, ConverterParameter = false });
            beforeItem.SetBinding(MenuItem.IsEnabledProperty, new Binding("DataContext.IsTailEnabled") { Source = dataGrid, Converter = boolInvert });
            contextMenu.Items.Add(beforeItem);

            cellStyle.Setters.Add(new Setter(DataGridCell.ContextMenuProperty, contextMenu));
            column.CellStyle = cellStyle;
            return column;
        }

        private static DataGridTextColumn CreateStandardTextColumn(DisplayColumnConfig config)
        {
            // AdditionalData[key] は LogEntry 自身のインデクサ [key] を使用するように変換する
            string path = config.BindingPath;
            if (path.StartsWith(AdditionalDataPrefix, StringComparison.OrdinalIgnoreCase) && path.EndsWith("]"))
            {
                // "AdditionalData" 部分を削って "[key]" にする (インデックス14の '[' から開始)
                path = path.Substring(AdditionalDataPrefix.Length - 1);
            }

            var binding = new Binding(path) { Mode = BindingMode.OneWay };
            if (!string.IsNullOrEmpty(config.StringFormat)) binding.StringFormat = config.StringFormat;

            return new DataGridTextColumn
            {
                Header = config.Header,
                Width = new DataGridLength(config.Width),
                MinWidth = config.Width,
                Binding = binding,
                SortMemberPath = config.BindingPath
            };
        }

        private static void ApplyColumnStyle(DataGridColumn column, DisplayColumnConfig config)
        {
            if (config.StyleConfig == null) return;

            var cellStyle = column.CellStyle ?? new Style(typeof(DataGridCell));

            // AdditionalData[key] は LogEntry 自身のインデクサ [key] を使用するように変換する
            string path = config.BindingPath;
            if (path.StartsWith(AdditionalDataPrefix, StringComparison.OrdinalIgnoreCase) && path.EndsWith("]"))
            {
                path = path.Substring(AdditionalDataPrefix.Length - 1);
            }

            void AddBindingSetter(DependencyProperty prop, string bindingPath, string param)
            {
                var binding = new Binding(bindingPath)
                {
                    Converter = new CellStyleConverter { StyleConfig = config.StyleConfig },
                    ConverterParameter = param
                };
                cellStyle.Setters.Add(new Setter(prop, binding));
            }

            AddBindingSetter(DataGridCell.BackgroundProperty, path, "Background");
            AddBindingSetter(DataGridCell.ForegroundProperty, path, "Foreground");
            AddBindingSetter(DataGridCell.FontWeightProperty, path, "FontWeight");

            column.CellStyle = cellStyle;
        }

        private static void ApplyHeaderContextMenu(DataGrid dataGrid, DataGridColumn column, DisplayColumnConfig config)
        {
            var keyName = LogEntryValueConverter.ExtractAdditionalDataKey(config.BindingPath);
            if (string.IsNullOrEmpty(keyName)) return;

            var headerMenu = new ContextMenu();
            headerMenu.Items.Add(new MenuItem
            {
                Header = "拡張フィルターに追加",
                Command = (dataGrid.DataContext as MainViewModel)?.AddExtensionFilterCommand,
                CommandParameter = keyName
            });

            var baseHeaderStyle = Application.Current.FindResource(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader)) as Style;
            var headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader), baseHeaderStyle);
            headerStyle.Setters.Add(new Setter(FrameworkElement.ContextMenuProperty, headerMenu));
            column.HeaderStyle = headerStyle;
        }

        private static void OnCellDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridCell cell && cell.DataContext is LogEntry)
            {
                var dataGrid = FindAncestor<DataGrid>(cell);
                if (dataGrid?.DataContext is MainViewModel vm)
                {
                    vm.ToggleBookmarkCommand?.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T ancestor) return ancestor;
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }
    }
}
