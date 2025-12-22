using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace MultiLogViewer.Behaviors
{
    public static class DataGridScrollToSelectionBehavior
    {
        #region SelectedItem (Existing)

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItem",
                typeof(object),
                typeof(DataGridScrollToSelectionBehavior),
                new PropertyMetadata(null, OnSelectedItemChanged));

        public static object GetSelectedItem(DependencyObject obj)
        {
            return obj.GetValue(SelectedItemProperty);
        }

        public static void SetSelectedItem(DependencyObject obj, object value)
        {
            obj.SetValue(SelectedItemProperty, value);
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid && e.NewValue != null)
            {
                dataGrid.ScrollIntoView(e.NewValue);
            }
        }

        #endregion

        #region AutoScrollOnCollectionChanged (New)

        public static readonly DependencyProperty AutoScrollOnCollectionChangedProperty =
            DependencyProperty.RegisterAttached(
                "AutoScrollOnCollectionChanged",
                typeof(bool),
                typeof(DataGridScrollToSelectionBehavior),
                new PropertyMetadata(false, OnAutoScrollOnCollectionChangedChanged));

        public static bool GetAutoScrollOnCollectionChanged(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollOnCollectionChangedProperty);
        }

        public static void SetAutoScrollOnCollectionChanged(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollOnCollectionChangedProperty, value);
        }

        // Private attached property to store the handler delegate to avoid leaks and allow unsubscription
        private static readonly DependencyProperty CollectionChangedHandlerProperty =
            DependencyProperty.RegisterAttached(
                "CollectionChangedHandler",
                typeof(NotifyCollectionChangedEventHandler),
                typeof(DataGridScrollToSelectionBehavior),
                new PropertyMetadata(null));

        private static void OnAutoScrollOnCollectionChangedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                if ((bool)e.NewValue)
                {
                    dataGrid.Loaded += DataGrid_Loaded;
                    dataGrid.Unloaded += DataGrid_Unloaded;
                }
                else
                {
                    dataGrid.Loaded -= DataGrid_Loaded;
                    dataGrid.Unloaded -= DataGrid_Unloaded;
                    UnsubscribeFromCollection(dataGrid);
                }
            }
        }

        private static void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                SubscribeToCollection(dataGrid);
            }
        }

        private static void DataGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                UnsubscribeFromCollection(dataGrid);
            }
        }

        private static void SubscribeToCollection(DataGrid dataGrid)
        {
            // Ensure we don't subscribe twice
            UnsubscribeFromCollection(dataGrid);

            if (dataGrid.Items is INotifyCollectionChanged collection)
            {
                // Create a handler that captures the dataGrid instance
                NotifyCollectionChangedEventHandler handler = (s, args) =>
                {
                    // Action needs to be deferred to let the grid update its internal state
                    dataGrid.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (dataGrid.SelectedItem != null)
                        {
                            try
                            {
                                dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                            }
                            catch
                            {
                                // Ignore errors if item is not in view or grid is in invalid state
                            }
                        }
                    }), System.Windows.Threading.DispatcherPriority.ContextIdle);
                };

                collection.CollectionChanged += handler;
                dataGrid.SetValue(CollectionChangedHandlerProperty, handler);
            }
        }

        private static void UnsubscribeFromCollection(DataGrid dataGrid)
        {
            var handler = (NotifyCollectionChangedEventHandler)dataGrid.GetValue(CollectionChangedHandlerProperty);
            if (handler != null && dataGrid.Items is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged -= handler;
                dataGrid.ClearValue(CollectionChangedHandlerProperty);
            }
        }

        #endregion
    }
}
