using MultiLogViewer.Models;
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

        private static void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e, DataGrid dataGrid)
        {
            // Regenerate columns when the collection changes
            GenerateColumns(dataGrid, sender as ObservableCollection<DisplayColumnConfig>);
        }

        private static void GenerateColumns(DataGrid dataGrid, ObservableCollection<DisplayColumnConfig> columns)
        {
            dataGrid.Columns.Clear();
            if (columns == null)
            {
                return;
            }

            foreach (var columnConfig in columns)
            {
                var newColumn = new DataGridTextColumn
                {
                    Header = columnConfig.Header,
                    Width = new DataGridLength(columnConfig.Width),
                    Binding = new Binding(columnConfig.BindingPath)
                    {
                        Mode = BindingMode.OneWay
                    },
                    SortMemberPath = columnConfig.BindingPath
                };
                dataGrid.Columns.Add(newColumn);
            }
        }
    }
}
