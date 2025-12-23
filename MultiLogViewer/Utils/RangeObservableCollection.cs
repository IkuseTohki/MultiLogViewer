using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace MultiLogViewer.Utils
{
    /// <summary>
    /// 範囲操作（AddRange）をサポートする ObservableCollection の拡張クラス。
    /// 大量のデータを追加する際のパフォーマンスを向上させます。
    /// </summary>
    /// <typeparam name="T">コレクション内の要素の型。</typeparam>
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification = false;

        /// <summary>
        /// 指定されたコレクションの要素を末尾に追加します。
        /// </summary>
        /// <param name="collection">追加する要素のコレクション。</param>
        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            try
            {
                _suppressNotification = true;

                foreach (var item in collection)
                {
                    Add(item);
                }
            }
            finally
            {
                _suppressNotification = false;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnCollectionChanged(e);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnPropertyChanged(e);
        }
    }
}
