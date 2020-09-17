using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace HandleManager {
    public interface IMyWindowCollection {
        object this[int index] { get; set; }

        bool CanAddNew { get; }
        bool CanAddNewItem { get; }
        bool CanCancelEdit { get; }
        bool CanChangeLiveFiltering { get; }
        bool CanChangeLiveGrouping { get; }
        bool CanChangeLiveSorting { get; }
        bool CanRemove { get; }
        object CurrentAddItem { get; }
        object CurrentEditItem { get; }
        bool IsAddingNew { get; }
        bool IsEditingItem { get; }
        bool IsFixedSize { get; }
        bool? IsLiveFiltering { get; set; }
        bool? IsLiveGrouping { get; set; }
        bool? IsLiveSorting { get; set; }
        bool IsReadOnly { get; }
        bool IsSynchronized { get; }
        ReadOnlyCollection<ItemPropertyInfo> ItemProperties { get; }
        ObservableCollection<string> LiveFilteringProperties { get; }
        ObservableCollection<string> LiveGroupingProperties { get; }
        ObservableCollection<string> LiveSortingProperties { get; }
        NewItemPlaceholderPosition NewItemPlaceholderPosition { get; set; }
        object SyncRoot { get; }

        int Add(object value);
        object AddNew();
        object AddNewItem(object newItem);
        void CancelEdit();
        void CancelNew();
        void Clear();
        void CommitEdit();
        void CommitNew();
        void CopyTo(Array array, int index);
        void EditItem(object item);
        void Insert(int index, object value);
        bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e);
        void Remove(object value);
        void RemoveAt(int index);
    }
}