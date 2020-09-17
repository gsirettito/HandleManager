// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  ArrayList 
**
** <owner>[....]</owner> 
**
**
** Purpose: Implements a dynamically sized List as an array,
**          and provides many convenience methods for treating 
**          an array as an IList.
** 
** 
===========================================================*/
namespace System.Collections {
    using System;
    using System.Runtime;
    using System.Security;
    using System.Security.Permissions;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Diagnostics.Contracts;

    // Implements a variable-size List that uses an array of objects to store the 
    // elements. A ArrayList has a capacity, which is the allocated length
    // of the internal array. As elements are added to a ArrayList, the capacity
    // of the ArrayList is automatically increased as required by reallocating the
    // internal array. 
    //
    [DebuggerTypeProxy(typeof(System.Collections.ArrayList.ArrayListDebugView))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class ArrayList : IList, ICloneable {
        private Object[] _items;
        [ContractPublicPropertyName("Count")]
        private int _size;
        private int _version;
        [NonSerialized]
        private Object _syncRoot;

        private const int _defaultCapacity = 4;
        private static readonly Object[] emptyArray = new Object[0];

        // Note: this constructor is a bogus constructor that does nothing 
        // and is for use only with SyncArrayList.
        internal ArrayList(bool trash) {
        }

        // Constructs a ArrayList. The list is initially empty and has a capacity
        // of zero. Upon adding the first element to the list the capacity is
        // increased to _defaultCapacity, and then increased in multiples of two as required.
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif 
        public ArrayList() {
            _items = emptyArray;
        }

        // Constructs a ArrayList with a given initial capacity. The list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required. 
        //
        public ArrayList(int capacity) {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", "capacity"));
            Contract.EndContractBlock();
            _items = new Object[capacity];
        }

        // Constructs a ArrayList, copying the contents of the given collection. The
        // size and capacity of the new list will both be equal to the size of the 
        // given collection.
        // 
        public ArrayList(ICollection c) {
            if (c == null)
                throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
            Contract.EndContractBlock();
            _items = new Object[c.Count];
            AddRange(c);
        }

        // Gets and sets the capacity of this list.  The capacity is the size of 
        // the internal array used to hold items.  When set, the internal 
        // array of the list is reallocated to the given capacity.
        // 
        public virtual int Capacity {
            get {
                Contract.Ensures(Contract.Result<int>() >= Count);
                return _items.Length;
            }
            set {
                if (value < _size) {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                }
                Contract.Ensures(Capacity >= 0);
                Contract.EndContractBlock();
                // We don't want to update the version number when we change the capacity.
                // Some existing applications have dependency on this. 
                if (value != _items.Length) {
                    if (value > 0) {
                        Object[] newItems = new Object[value];
                        if (_size > 0) {
                            Array.Copy(_items, 0, newItems, 0, _size);
                        }
                        _items = newItems;
                    } else {
                        _items = new Object[_defaultCapacity];
                    }
                }
            }
        }

        // Read-only property describing how many elements are in the List.
        public virtual int Count {
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return _size;
            }
        }

        public virtual bool IsFixedSize {
            get { return false; }
        }


        // Is this ArrayList read-only? 
        public virtual bool IsReadOnly {
            get { return false; }
        }

        // Is this ArrayList synchronized (thread-safe)?
        public virtual bool IsSynchronized {
            get { return false; }
        }

        // Synchronization root for this object. 
        public virtual Object SyncRoot {
            get {
                if (_syncRoot == null) {
                    System.Threading.Interlocked.CompareExchange<object>(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        // Sets or Gets the element at the given index.
        // 
        public virtual Object this[int index] {
            get {
                if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                Contract.EndContractBlock();
                return _items[index];
            }
            set {
                if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                Contract.EndContractBlock();
                _items[index] = value;
                _version++;
            }
        }

        // Creates a ArrayList wrapper for a particular IList.  This does not 
        // copy the contents of the IList, but only wraps the ILIst.  So any 
        // changes to the underlying list will affect the ArrayList.  This would
        // be useful if you want to Reverse a subrange of an IList, or want to 
        // use a generic BinarySearch or Sort method without implementing one yourself.
        // However, since these methods are generic, the performance may not be
        // nearly as good for some operations as they would be on the IList itself.
        // 
        public static ArrayList Adapter(IList list) {
            if (list == null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<arraylist>() != null);
            Contract.EndContractBlock();
            return new IListWrapper(list);
        }

        // Adds the given object to the end of this list. The size of the list is 
        // increased by one. If required, the capacity of the list is doubled
        // before adding the new element. 
        // 
        public virtual int Add(Object value) {
            Contract.Ensures(Contract.Result<int>() >= 0);
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            _items[_size] = value;
            _version++;
            return _size++;
        }

        // Adds the elements of the given collection to the end of this list. If 
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger. 
        //
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif 
        public virtual void AddRange(ICollection c) {
            InsertRange(_size, c);
        }

        // Searches a section of the list for a given element using a binary search 
        // algorithm. Elements of the list are compared to the search value using
        // the given IComparer interface. If comparer is null, elements of
        // the list are compared to the search value using the IComparable
        // interface, which in that case must be implemented by all elements of the 
        // list and the given search value. This method assumes that the given
        // section of the list is already sorted; if this is not the case, the 
        // result will be incorrect. 
        //
        // The method returns the index of the given value in the list. If the 
        // list does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value. This is also the index at which 
        // the search value should be inserted into the list in order for the list
        // to remain sorted. 
        // 
        // The method uses the Array.BinarySearch method to perform the
        // search. 
        //
        public virtual int BinarySearch(int index, int count, Object value, IComparer comparer) {
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.Ensures(Contract.Result<int>() < index + count);
            Contract.EndContractBlock();

            return Array.BinarySearch((Array)_items, index, count, value, comparer);
        }

        public virtual int BinarySearch(Object value) {
            Contract.Ensures(Contract.Result<int>() < Count);
            return BinarySearch(0, Count, value, null);
        }

        public virtual int BinarySearch(Object value, IComparer comparer) {
            Contract.Ensures(Contract.Result<int>() < Count);
            return BinarySearch(0, Count, value, comparer);
        }


        // Clears the contents of ArrayList. 
        public virtual void Clear() {
            if (_size > 0) {
                Array.Clear(_items, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references. 
                _size = 0;
            }
            _version++;
        }

        // Clones this ArrayList, doing a shallow copy.  (A copy is made of all
        // Object references in the ArrayList, but the Objects pointed to
        // are not cloned).
        public virtual Object Clone() {
            Contract.Ensures(Contract.Result<object>() != null);
            ArrayList la = new ArrayList(_size);
            la._size = _size;
            la._version = _version;
            Array.Copy(_items, 0, la._items, 0, _size);
            return la;
        }


        // Contains returns true if the specified element is in the ArrayList. 
        // It does a linear, O(n) search.  Equality is determined by calling 
        // item.Equals().
        // 
        public virtual bool Contains(Object item) {
            if (item == null) {
                for (int i = 0; i < _size; i++)
                    if (_items[i] == null)
                        return true;
                return false;
            } else {
                for (int i = 0; i < _size; i++)
                    if ((_items[i] != null) && (_items[i].Equals(item)))
                        return true;
                return false;
            }
        }

        // Copies this ArrayList into array, which must be of a 
        // compatible array type.
        // 
        public virtual void CopyTo(Array array) {
            CopyTo(array, 0);
        }

        // Copies this ArrayList into array, which must be of a
        // compatible array type. 
        // 
        public virtual void CopyTo(Array array, int arrayIndex) {
            if ((array != null) && (array.Rank != 1))
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
            Contract.EndContractBlock();
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        // Copies a section of this list to the given array at the given index. 
        //
        // The method uses the Array.Copy method to copy the elements. 
        //
        public virtual void CopyTo(int index, Array array, int arrayIndex, int count) {
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            if ((array != null) && (array.Rank != 1))
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
            Contract.EndContractBlock();
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, index, array, arrayIndex, count);
        }

        // Ensures that the capacity of this list is at least the given minimum
        // value. If the currect capacity of the list is less than min, the 
        // capacity is increased to twice the current capacity or to min,
        // whichever is larger. 
        private void EnsureCapacity(int min) {
            if (_items.Length < min) {
                int newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length * 2;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }

        // Returns a list wrapper that is fixed at the current size.  Operations 
        // that add or remove items will fail, however, replacing items is allowed. 
        //
        public static IList FixedSize(IList list) {
            if (list == null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<ilist>() != null);
            Contract.EndContractBlock();
            return new FixedSizeList(list);
        }

        // Returns a list wrapper that is fixed at the current size.  Operations
        // that add or remove items will fail, however, replacing items is allowed. 
        //
        public static ArrayList FixedSize(ArrayList list) {
            if (list == null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<arraylist>() != null);
            Contract.EndContractBlock();
            return new FixedSizeArrayList(list);
        }

        // Returns an enumerator for this list with the given
        // permission for removal of elements. If modifications made to the list
        // while an enumeration is in progress, the MoveNext and
        // GetObject methods of the enumerator will throw an exception. 
        //
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public virtual IEnumerator GetEnumerator() {
            Contract.Ensures(Contract.Result<ienumerator>() != null);
            return new ArrayListEnumeratorSimple(this);
        }

        // Returns an enumerator for a section of this list with the given
        // permission for removal of elements. If modifications made to the list 
        // while an enumeration is in progress, the MoveNext and 
        // GetObject methods of the enumerator will throw an exception.
        // 
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
#if false 
        private void TrimSrcHack() {}    // workaround to avoid unclosed "#if !FEATURE_CORECLR"
#endif 
        public virtual IEnumerator GetEnumerator(int index, int count) {
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.Ensures(Contract.Result<ienumerator>() != null);
            Contract.EndContractBlock();

            return new ArrayListEnumerator(this, index, count);
        }

        // Returns the index of the first occurrence of a given value in a range of 
        // this list. The list is searched forwards from beginning to end.
        // The elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search. 
        // 
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public virtual int IndexOf(Object value) {
            Contract.Ensures(Contract.Result<int>() < Count);
            return Array.IndexOf((Array)_items, value, 0, _size);
        }

        // Returns the index of the first occurrence of a given value in a range of 
        // this list. The list is searched forwards, starting at index
        // startIndex and ending at count number of elements. The 
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        //
        // This method uses the Array.IndexOf method to perform the 
        // search.
        // 
        public virtual int IndexOf(Object value, int startIndex) {
            if (startIndex > _size)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();
            return Array.IndexOf((Array)_items, value, startIndex, _size - startIndex);
        }

        // Returns the index of the first occurrence of a given value in a range of 
        // this list. The list is searched forwards, starting at index 
        // startIndex and upto count number of elements. The
        // elements of the list are compared to the given value using the 
        // Object.Equals method.
        //
        // This method uses the Array.IndexOf method to perform the
        // search. 
        //
        public virtual int IndexOf(Object value, int startIndex, int count) {
            if (startIndex > _size)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (count < 0 || startIndex > _size - count) throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();
            return Array.IndexOf((Array)_items, value, startIndex, count);
        }

        // Inserts an element into this list at a given index. The size of the list 
        // is increased by one. If required, the capacity of the list is doubled 
        // before inserting the new element.
        // 
        public virtual void Insert(int index, Object value) {
            // Note that insertions at the end are legal.
            if (index < 0 || index > _size) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_ArrayListInsert"));
            //Contract.Ensures(Count == Contract.OldValue(Count) + 1); 
            Contract.EndContractBlock();

            if (_size == _items.Length) EnsureCapacity(_size + 1);
            if (index < _size) {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = value;
            _size++;
            _version++;
        }

        // Inserts the elements of the given collection at a given index. If 
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.  Ranges may be added 
        // to the end of the list by setting index to the ArrayList's size.
        //
        public virtual void InsertRange(int index, ICollection c) {
            if (c == null)
                throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
            if (index < 0 || index > _size) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            //Contract.Ensures(Count == Contract.OldValue(Count) + c.Count); 
            Contract.EndContractBlock();

            int count = c.Count;
            if (count > 0) {
                EnsureCapacity(_size + count);
                // shift existing items 
                if (index < _size) {
                    Array.Copy(_items, index, _items, index + count, _size - index);
                }

                Object[] itemsToInsert = new Object[count];
                c.CopyTo(itemsToInsert, 0);
                itemsToInsert.CopyTo(_items, index);
                _size += count;
                _version++;
            }
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at the end 
        // and ending at the first element in the list. The elements of the list
        // are compared to the given value using the Object.Equals method.
        //
        // This method uses the Array.LastIndexOf method to perform the 
        // search.
        // 
        public virtual int LastIndexOf(Object value) {
            Contract.Ensures(Contract.Result<int>() < _size);
            return LastIndexOf(value, _size - 1, _size);
        }

        // Returns the index of the last occurrence of a given value in a range of 
        // this list. The list is searched backwards, starting at index
        // startIndex and ending at the first element in the list. The 
        // elements of the list are compared to the given value using the 
        // Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        //
        public virtual int LastIndexOf(Object value, int startIndex) {
            if (startIndex >= _size)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();
            return LastIndexOf(value, startIndex, startIndex + 1);
        }

        // Returns the index of the last occurrence of a given value in a range of 
        // this list. The list is searched backwards, starting at index
        // startIndex and upto count elements. The elements of 
        // the list are compared to the given value using the Object.Equals 
        // method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        //
        public virtual int LastIndexOf(Object value, int startIndex, int count) {
            if (Count != 0 && (startIndex < 0 || count < 0))
                throw new ArgumentOutOfRangeException((startIndex < 0 ? "startIndex" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();

            if (_size == 0)  // Special case for an empty list
                return -1;

            if (startIndex >= _size || count > startIndex + 1)
                throw new ArgumentOutOfRangeException((startIndex >= _size ? "startIndex" : "count"), Environment.GetResourceString("ArgumentOutOfRange_BiggerThanCollection"));

            return Array.LastIndexOf((Array)_items, value, startIndex, count);
        }

        // Returns a read-only IList wrapper for the given IList.
        //
        public static IList ReadOnly(IList list) {
            if (list == null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<ilist>() != null);
            Contract.EndContractBlock();
            return new ReadOnlyList(list);
        }

        // Returns a read-only ArrayList wrapper for the given ArrayList.
        //
        public static ArrayList ReadOnly(ArrayList list) {
            if (list == null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<arraylist>() != null);
            Contract.EndContractBlock();
            return new ReadOnlyArrayList(list);
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one. 
        //
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public virtual void Remove(Object obj) {
            Contract.Ensures(Count >= 0);

            int index = IndexOf(obj);
            BCLDebug.Correctness(index >= 0 || !(obj is Int32), "You passed an Int32 to Remove that wasn't in the ArrayList." + Environment.NewLine + "Did you mean RemoveAt?  int: " + obj + "  Count: " + Count);
            if (index >= 0)
                RemoveAt(index);
        }

        // Removes the element at the given index. The size of the list is 
        // decreased by one.
        //
        public virtual void RemoveAt(int index) {
            if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.Ensures(Count >= 0);
            //Contract.Ensures(Count == Contract.OldValue(Count) - 1); 
            Contract.EndContractBlock();

            _size--;
            if (index < _size) {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = null;
            _version++;
        }

        // Removes a range of elements from this list.
        // 
        public virtual void RemoveRange(int index, int count) {
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.Ensures(Count >= 0);
            //Contract.Ensures(Count == Contract.OldValue(Count) - count); 
            Contract.EndContractBlock();

            if (count > 0) {
                int i = _size;
                _size -= count;
                if (index < _size) {
                    Array.Copy(_items, index + count, _items, index, _size - index);
                }
                while (i > _size) _items[--i] = null;
                _version++;
            }
        }

        // Returns an IList that contains count copies of value.
        // 
        public static ArrayList Repeat(Object value, int count) {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.Ensures(Contract.Result<arraylist>() != null);
            Contract.EndContractBlock();

            ArrayList list = new ArrayList((count > _defaultCapacity) ? count : _defaultCapacity);
            for (int i = 0; i < count; i++) {
                list.add(value);
                return list;
            }
        }
            // reverses the elements in this list.
            //
            public virtual void reverse(){
                reverse(0, count);
            //a range of following call to method, an element given by index and count which was previously located at i will now be + (index - 1).
            //method uses array.reverse reverse elements.
            private reverse(int index, int count) {
                if < 0 || 0) 
                throw new ArgumentOutOfRangeException((index<0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum")); 
                (_size argumentexception(environment.getresourcestring("argument_invalidofflen"));
                contract.endcontractblock();
                array.reverse(_items, _version++; sets starting collection. setrange(int icollection c) (c="=null)" argumentnullexception("c", environment.getresourcestring("argumentnull_collection"));=""> _size - count) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
 
            if (count > 0) { 
                c.CopyTo(_items, index);
                _version++; 
            }
}

public virtual ArrayList GetRange(int index, int count) {
    if (index < 0 || count < 0)
        throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
    if (_size - index < count)
        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
    Contract.Ensures(Contract.Result<arraylist>() != null);
    Contract.EndContractBlock();
    return new Range(this, index, count);
}

// Sorts the elements in this list.  Uses the default comparer and
// Array.Sort.
public virtual void Sort() {
    Sort(0, Count, Comparer.Default);
}

// Sorts the elements in this list.  Uses Array.Sort with the 
// provided comparer.
public virtual void Sort(IComparer comparer) {
    Sort(0, Count, comparer);
}

// Sorts the elements in a section of this list. The sort compares the 
// elements to each other using the given IComparer interface. If
// comparer is null, the elements are compared to each other using 
// the IComparable interface, which in that case must be implemented by all
// elements of the list.
//
// This method uses the Array.Sort method to sort the elements. 
//
public virtual void Sort(int index, int count, IComparer comparer) {
    if (index < 0 || count < 0)
        throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
    if (_size - index < count)
        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
    Contract.EndContractBlock();

    Array.Sort(_items, index, count, comparer);
    _version++;
}

// Returns a thread-safe wrapper around an IList.
// 
[HostProtection(Synchronization = true)]
public static IList Synchronized(IList list) {
    if (list == null)
        throw new ArgumentNullException("list");
    Contract.Ensures(Contract.Result<ilist>() != null);
    Contract.EndContractBlock();
    return new SyncIList(list);
}

// Returns a thread-safe wrapper around a ArrayList.
//
[HostProtection(Synchronization = true)]
public static ArrayList Synchronized(ArrayList list) {
    if (list == null)
        throw new ArgumentNullException("list");
    Contract.Ensures(Contract.Result<arraylist>() != null);
    Contract.EndContractBlock();
    return new SyncArrayList(list);
}

// ToArray returns a new Object array containing the contents of the ArrayList.
// This requires copying the ArrayList, which is an O(n) operation. 
public virtual Object[] ToArray() {
    Contract.Ensures(Contract.Result<object[]>() != null);

    Object[] array = new Object[_size];
    Array.Copy(_items, 0, array, 0, _size);
    return array;
}

// ToArray returns a new array of a particular type containing the contents 
// of the ArrayList.  This requires copying the ArrayList and potentially
// downcasting all elements.  This copy may fail and is an O(n) operation. 
// Internally, this implementation calls Array.Copy. 
//
[SecuritySafeCritical]
public virtual Array ToArray(Type type) {
    if (type == null)
        throw new ArgumentNullException("type");
    Contract.Ensures(Contract.Result<array>() != null);
    Contract.EndContractBlock();
    Array array = Array.UnsafeCreateInstance(type, _size);
    Array.Copy(_items, 0, array, 0, _size);
    return array;
}

// Sets the capacity of this list to the size of the list. This method can
// be used to minimize a list's memory overhead once it is known that no
// new elements will be added to the list. To completely clear a list and 
// release all memory referenced by the list, execute the following
// statements: 
// 
// list.Clear();
// list.TrimToSize(); 
//
public virtual void TrimToSize() {
    Capacity = _size;
}


// This class wraps an IList, exposing it as a ArrayList 
// Note this requires reimplementing half of ArrayList...
[Serializable]
private class IListWrapper : ArrayList {
    private IList _list;

    internal IListWrapper(IList list) {
        _list = list;
        _version = 0; // list doesn't not contain a version number 
    }

    public override int Capacity {
        get { return _list.Count; }
        set {
            if (value < _list.Count) throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
        }
    }

    public override int Count {
        get { return _list.Count; }
    }

    public override bool IsReadOnly {
        get { return _list.IsReadOnly; }
    }

    public override bool IsFixedSize {
        get { return _list.IsFixedSize; }
    }


    public override bool IsSynchronized {
        get { return _list.IsSynchronized; }
    }

    public override Object this[int index] {
        get {
            return _list[index];
        }
        set {
            _list[index] = value;
            _version++;
        }
    }

    public override Object SyncRoot {
        get { return _list.SyncRoot; }
    }

    public override int Add(Object obj) {
        int i = _list.Add(obj);
        _version++;
        return i;
    }

    public override void AddRange(ICollection c) {
        InsertRange(Count, c);
    }

    // Other overloads with automatically work 
    public override int BinarySearch(int index, int count, Object value, IComparer comparer) {
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (_list.Count - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        if (comparer == null)
            comparer = Comparer.Default;

        int lo = index;
        int hi = index + count - 1;
        int mid;
        while (lo <= hi) {
            mid = (lo + hi) / 2;
            int r = comparer.Compare(value, _list[mid]);
            if (r == 0)
                return mid;
            if (r < 0)
                hi = mid - 1;
            else
                lo = mid + 1;
        }
        // return bitwise complement of the first element greater than value. 
        // Since hi is less than lo now, ~lo is the correct item.
        return ~lo;
    }

    public override void Clear() {
        // If _list is an array, it will support Clear method. 
        // We shouldn't allow clear operation on a FixedSized ArrayList 
        if (_list.IsFixedSize) {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
        }

        _list.Clear();
        _version++;
    }

    public override Object Clone() {
        // This does not do a shallow copy of _list into a ArrayList!
        // This clones the IListWrapper, creating another wrapper class! 
        return new IListWrapper(_list);
    }

    public override bool Contains(Object obj) {
        return _list.Contains(obj);
    }

    public override void CopyTo(Array array, int index) {
        _list.CopyTo(array, index);
    }

    public override void CopyTo(int index, Array array, int arrayIndex, int count) {
        if (array == null)
            throw new ArgumentNullException("array");
        if (index < 0 || arrayIndex < 0)
            throw new ArgumentOutOfRangeException((index < 0) ? "index" : "arrayIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (count < 0)
            throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (array.Length - arrayIndex < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        if (array.Rank != 1)
            throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
        Contract.EndContractBlock();

        if (_list.Count - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));

        for (int i = index; i < index + count; i++) array.setvalue(_list[i], arrayindex++); } public override ienumerator getenumerator() { return _list.getenumerator(); getenumerator(int index, int count) if (index < 0 || count 0) throw new argumentoutofrangeexception((index<0 ? "index" : "count"), environment.getresourcestring("argumentoutofrange_neednonnegnum")); contract.endcontractblock(); (_list.count - index argumentexception(environment.getresourcestring("argument_invalidofflen")); ilistwrapperenumwrapper(this, count); indexof(object value) _list.indexof(value); value, startindex) indexof(value, startindex, _list.count startindex); (startindex startindex=""> _list.Count) throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                if (count< 0 || startIndex> _list.Count - count) throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
 
                int endIndex = startIndex + count; 
                if (value == null) {
                    for(int i=startIndex; i<endindex; i++) if (_list[i]="=" null) return i; -1; } else { for(int i="startIndex;" i<endindex; !="null" && _list[i].equals(value)) public override void insert(int index, object obj) _list.insert(index, obj); _version++; insertrange(int icollection c) (c="=null)" throw new argumentnullexception("c", environment.getresourcestring("argumentnull_collection")); (index < 0 || index=""> _list.Count) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
  
                if(c.Count > 0) {
                    ArrayList al = _list as ArrayList;
                    if(al != null) {
                        // We need to special case ArrayList. 
                        // When c is a range of _list, we need to handle this in a special way.
                        // See ArrayList.InsertRange for details. 
                        al.InsertRange(index, c); 
                    }
                    else { 
                        IEnumerator en = c.GetEnumerator();
                        while(en.MoveNext()) {
                            _list.Insert(index++, en.Current);
                        } 
                    }
                    _version++; 
                } 
            }
  
            public override int LastIndexOf(Object value) {
    return LastIndexOf(value, _list.Count - 1, _list.Count);
}

public override int LastIndexOf(Object value, int startIndex) {
    return LastIndexOf(value, startIndex, startIndex + 1);
}

public override int LastIndexOf(Object value, int startIndex, int count) {
    if (_list.Count == 0)
        return -1;

    if (startIndex < 0 || startIndex >= _list.Count) throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
    if (count < 0 || count > startIndex + 1) throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));

    int endIndex = startIndex - count + 1;
    if (value == null) {
        for (int i = startIndex; i >= endIndex; i--)
            if (_list[i] == null)
                return i;
        return -1;
    } else {
        for (int i = startIndex; i >= endIndex; i--)
            if (_list[i] != null && _list[i].Equals(value))
                return i;
        return -1;
    }
}

public override void Remove(Object value) {
    int index = IndexOf(value);
    if (index >= 0)
        RemoveAt(index);
}

public override void RemoveAt(int index) {
    _list.RemoveAt(index);
    _version++;
}

public override void RemoveRange(int index, int count) {
    if (index < 0 || count < 0)
        throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
    Contract.EndContractBlock();
    if (_list.Count - index < count)
        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));

    if (count > 0)    // be consistent with ArrayList
        _version++;

    while (count > 0) {
        _list.RemoveAt(index);
        count--;
    }
}

public override void Reverse(int index, int count) {
    if (index < 0 || count < 0)
        throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
    Contract.EndContractBlock();
    if (_list.Count - index < count)
        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));

    int i = index;
    int j = index + count - 1;
    while (i < j) {
        Object tmp = _list[i];
        _list[i++] = _list[j];
        _list[j--] = tmp;
    }
    _version++;
}

public override void SetRange(int index, ICollection c) {
    if (c == null) {
        throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
    }
    Contract.EndContractBlock();

    if (index < 0 || index > _list.Count - c.Count) {
        throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
    }

    if (c.Count > 0) {
        IEnumerator en = c.GetEnumerator();
        while (en.MoveNext()) {
            _list[index++] = en.Current;
        }
        _version++;
    }
}

public override ArrayList GetRange(int index, int count) {
    if (index < 0 || count < 0)
        throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
    Contract.EndContractBlock();
    if (_list.Count - index < count)
        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
    return new Range(this, index, count);
}

public override void Sort(int index, int count, IComparer comparer) {
    if (index < 0 || count < 0)
        throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
    Contract.EndContractBlock();
    if (_list.Count - index < count)
        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));

    Object[] array = new Object[count];
    CopyTo(index, array, 0, count);
    Array.Sort(array, 0, count, comparer);
    for (int i = 0; i < count; i++) _list[i + index] = "array[i];" _version++; } public override object[] toarray() { array="new" object[count]; _list.copyto(array, 0); return array; [securitysafecritical] toarray(type type) if (type="=null)" throw new argumentnullexception("type"); contract.endcontractblock(); _list.count); void trimtosize() can't really do much here... this is the enumerator for an ilist that's been wrapped in another class that implements all of arraylist's methods. [serializable] private sealed ilistwrapperenumwrapper : ienumerator, icloneable ienumerator _en; int _remaining; _initialstartindex; reset _initialcount; bool _firstcall; firstcall to movenext ilistwrapperenumwrapper() internal ilistwrapperenumwrapper(ilistwrapper listwrapper, startindex, count) _en="listWrapper.GetEnumerator();" _initialstartindex="startIndex;" _initialcount="count;" while(startindex--=""> 0 && _en.MoveNext()); 
                    _remaining = count;
                    _firstCall = true;
                }
  
                public Object Clone() {
    // We must clone the underlying enumerator, I think. 
    IListWrapperEnumWrapper clone = new IListWrapperEnumWrapper();
    clone._en = (IEnumerator)((ICloneable)_en).Clone();
    clone._initialStartIndex = _initialStartIndex;
    clone._initialCount = _initialCount;
    clone._remaining = _remaining;
    clone._firstCall = _firstCall;
    return clone;
}

public bool MoveNext() {
    if (_firstCall) {
        _firstCall = false;
        return _remaining-- > 0 && _en.MoveNext();
    }
    if (_remaining < 0)
        return false;
    bool r = _en.MoveNext();
    return r && _remaining-- > 0;
}

public Object Current {
    get {
        if (_firstCall)
            throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
        if (_remaining < 0)
            throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
        return _en.Current;
    }
}

public void Reset() {
    _en.Reset();
    int startIndex = _initialStartIndex;
    while (startIndex-- > 0 && _en.MoveNext()) ;
    _remaining = _initialCount;
    _firstCall = true;
} 
            }
        } 
 
 
        [Serializable]
private class SyncArrayList : ArrayList {
    private ArrayList _list;
    private Object _root;

    internal SyncArrayList(ArrayList list)
        : base(false) {
        _list = list;
        _root = list.SyncRoot;
    }

    public override int Capacity {
        get {
            lock (_root) {
                return _list.Capacity;
            }
        }
        set {
            lock (_root) {
                _list.Capacity = value;
            }
        }
    }

    public override int Count {
        get { lock (_root) { return _list.Count; } }
    }

    public override bool IsReadOnly {
        get { return _list.IsReadOnly; }
    }

    public override bool IsFixedSize {
        get { return _list.IsFixedSize; }
    }


    public override bool IsSynchronized {
        get { return true; }
    }

    public override Object this[int index] {
        get {
            lock (_root) {
                return _list[index];
            }
        }
        set {
            lock (_root) {
                _list[index] = value;
            }
        }
    }

    public override Object SyncRoot {
        get { return _root; }
    }

    public override int Add(Object value) {
        lock (_root) {
            return _list.Add(value);
        }
    }

    public override void AddRange(ICollection c) {
        lock (_root) {
            _list.AddRange(c);
        }
    }

    public override int BinarySearch(Object value) {
        lock (_root) {
            return _list.BinarySearch(value);
        }
    }

    public override int BinarySearch(Object value, IComparer comparer) {
        lock (_root) {
            return _list.BinarySearch(value, comparer);
        }
    }

    public override int BinarySearch(int index, int count, Object value, IComparer comparer) {
        lock (_root) {
            return _list.BinarySearch(index, count, value, comparer);
        }
    }

    public override void Clear() {
        lock (_root) {
            _list.Clear();
        }
    }

    public override Object Clone() {
        lock (_root) {
            return new SyncArrayList((ArrayList)_list.Clone());
        }
    }

    public override bool Contains(Object item) {
        lock (_root) {
            return _list.Contains(item);
        }
    }

    public override void CopyTo(Array array) {
        lock (_root) {
            _list.CopyTo(array);
        }
    }

    public override void CopyTo(Array array, int index) {
        lock (_root) {
            _list.CopyTo(array, index);
        }
    }

    public override void CopyTo(int index, Array array, int arrayIndex, int count) {
        lock (_root) {
            _list.CopyTo(index, array, arrayIndex, count);
        }
    }

    public override IEnumerator GetEnumerator() {
        lock (_root) {
            return _list.GetEnumerator();
        }
    }

    public override IEnumerator GetEnumerator(int index, int count) {
        lock (_root) {
            return _list.GetEnumerator(index, count);
        }
    }

    public override int IndexOf(Object value) {
        lock (_root) {
            return _list.IndexOf(value);
        }
    }

    public override int IndexOf(Object value, int startIndex) {
        lock (_root) {
            return _list.IndexOf(value, startIndex);
        }
    }

    public override int IndexOf(Object value, int startIndex, int count) {
        lock (_root) {
            return _list.IndexOf(value, startIndex, count);
        }
    }

    public override void Insert(int index, Object value) {
        lock (_root) {
            _list.Insert(index, value);
        }
    }

    public override void InsertRange(int index, ICollection c) {
        lock (_root) {
            _list.InsertRange(index, c);
        }
    }

    public override int LastIndexOf(Object value) {
        lock (_root) {
            return _list.LastIndexOf(value);
        }
    }

    public override int LastIndexOf(Object value, int startIndex) {
        lock (_root) {
            return _list.LastIndexOf(value, startIndex);
        }
    }

    public override int LastIndexOf(Object value, int startIndex, int count) {
        lock (_root) {
            return _list.LastIndexOf(value, startIndex, count);
        }
    }

    public override void Remove(Object value) {
        lock (_root) {
            _list.Remove(value);
        }
    }

    public override void RemoveAt(int index) {
        lock (_root) {
            _list.RemoveAt(index);
        }
    }

    public override void RemoveRange(int index, int count) {
        lock (_root) {
            _list.RemoveRange(index, count);
        }
    }

    public override void Reverse(int index, int count) {
        lock (_root) {
            _list.Reverse(index, count);
        }
    }

    public override void SetRange(int index, ICollection c) {
        lock (_root) {
            _list.SetRange(index, c);
        }
    }

    public override ArrayList GetRange(int index, int count) {
        lock (_root) {
            return _list.GetRange(index, count);
        }
    }

    public override void Sort() {
        lock (_root) {
            _list.Sort();
        }
    }

    public override void Sort(IComparer comparer) {
        lock (_root) {
            _list.Sort(comparer);
        }
    }

    public override void Sort(int index, int count, IComparer comparer) {
        lock (_root) {
            _list.Sort(index, count, comparer);
        }
    }

    public override Object[] ToArray() {
        lock (_root) {
            return _list.ToArray();
        }
    }

    public override Array ToArray(Type type) {
        lock (_root) {
            return _list.ToArray(type);
        }
    }

    public override void TrimToSize() {
        lock (_root) {
            _list.TrimToSize();
        }
    }
}


[Serializable]
private class SyncIList : IList {
    private IList _list;
    private Object _root;

    internal SyncIList(IList list) {
        _list = list;
        _root = list.SyncRoot;
    }

    public virtual int Count {
        get { lock (_root) { return _list.Count; } }
    }

    public virtual bool IsReadOnly {
        get { return _list.IsReadOnly; }
    }

    public virtual bool IsFixedSize {
        get { return _list.IsFixedSize; }
    }


    public virtual bool IsSynchronized {
        get { return true; }
    }

    public virtual Object this[int index] {
        get {
            lock (_root) {
                return _list[index];
            }
        }
        set {
            lock (_root) {
                _list[index] = value;
            }
        }
    }

    public virtual Object SyncRoot {
        get { return _root; }
    }

    public virtual int Add(Object value) {
        lock (_root) {
            return _list.Add(value);
        }
    }


    public virtual void Clear() {
        lock (_root) {
            _list.Clear();
        }
    }

    public virtual bool Contains(Object item) {
        lock (_root) {
            return _list.Contains(item);
        }
    }

    public virtual void CopyTo(Array array, int index) {
        lock (_root) {
            _list.CopyTo(array, index);
        }
    }

    public virtual IEnumerator GetEnumerator() {
        lock (_root) {
            return _list.GetEnumerator();
        }
    }

    public virtual int IndexOf(Object value) {
        lock (_root) {
            return _list.IndexOf(value);
        }
    }

    public virtual void Insert(int index, Object value) {
        lock (_root) {
            _list.Insert(index, value);
        }
    }

    public virtual void Remove(Object value) {
        lock (_root) {
            _list.Remove(value);
        }
    }

    public virtual void RemoveAt(int index) {
        lock (_root) {
            _list.RemoveAt(index);
        }
    }
}

[Serializable]
private class FixedSizeList : IList {
    private IList _list;

    internal FixedSizeList(IList l) {
        _list = l;
    }

    public virtual int Count {
        get { return _list.Count; }
    }

    public virtual bool IsReadOnly {
        get { return _list.IsReadOnly; }
    }

    public virtual bool IsFixedSize {
        get { return true; }
    }

    public virtual bool IsSynchronized {
        get { return _list.IsSynchronized; }
    }

    public virtual Object this[int index] {
        get {
            return _list[index];
        }
        set {
            _list[index] = value;
        }
    }

    public virtual Object SyncRoot {
        get { return _list.SyncRoot; }
    }

    public virtual int Add(Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public virtual void Clear() {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public virtual bool Contains(Object obj) {
        return _list.Contains(obj);
    }

    public virtual void CopyTo(Array array, int index) {
        _list.CopyTo(array, index);
    }

    public virtual IEnumerator GetEnumerator() {
        return _list.GetEnumerator();
    }

    public virtual int IndexOf(Object value) {
        return _list.IndexOf(value);
    }

    public virtual void Insert(int index, Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public virtual void Remove(Object value) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public virtual void RemoveAt(int index) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }
}

[Serializable]
private class FixedSizeArrayList : ArrayList {
    private ArrayList _list;

    internal FixedSizeArrayList(ArrayList l) {
        _list = l;
        _version = _list._version;
    }

    public override int Count {
        get { return _list.Count; }
    }

    public override bool IsReadOnly {
        get { return _list.IsReadOnly; }
    }

    public override bool IsFixedSize {
        get { return true; }
    }

    public override bool IsSynchronized {
        get { return _list.IsSynchronized; }
    }

    public override Object this[int index] {
        get {
            return _list[index];
        }
        set {
            _list[index] = value;
            _version = _list._version;
        }
    }

    public override Object SyncRoot {
        get { return _list.SyncRoot; }
    }

    public override int Add(Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override void AddRange(ICollection c) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override int BinarySearch(int index, int count, Object value, IComparer comparer) {
        return _list.BinarySearch(index, count, value, comparer);
    }

    public override int Capacity {
        get { return _list.Capacity; }
        set { throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection")); }
    }

    public override void Clear() {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override Object Clone() {
        FixedSizeArrayList arrayList = new FixedSizeArrayList(_list);
        arrayList._list = (ArrayList)_list.Clone();
        return arrayList;
    }

    public override bool Contains(Object obj) {
        return _list.Contains(obj);
    }

    public override void CopyTo(Array array, int index) {
        _list.CopyTo(array, index);
    }

    public override void CopyTo(int index, Array array, int arrayIndex, int count) {
        _list.CopyTo(index, array, arrayIndex, count);
    }

    public override IEnumerator GetEnumerator() {
        return _list.GetEnumerator();
    }

    public override IEnumerator GetEnumerator(int index, int count) {
        return _list.GetEnumerator(index, count);
    }

    public override int IndexOf(Object value) {
        return _list.IndexOf(value);
    }

    public override int IndexOf(Object value, int startIndex) {
        return _list.IndexOf(value, startIndex);
    }

    public override int IndexOf(Object value, int startIndex, int count) {
        return _list.IndexOf(value, startIndex, count);
    }

    public override void Insert(int index, Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override void InsertRange(int index, ICollection c) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override int LastIndexOf(Object value) {
        return _list.LastIndexOf(value);
    }

    public override int LastIndexOf(Object value, int startIndex) {
        return _list.LastIndexOf(value, startIndex);
    }

    public override int LastIndexOf(Object value, int startIndex, int count) {
        return _list.LastIndexOf(value, startIndex, count);
    }

    public override void Remove(Object value) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override void RemoveAt(int index) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override void RemoveRange(int index, int count) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override void SetRange(int index, ICollection c) {
        _list.SetRange(index, c);
        _version = _list._version;
    }

    public override ArrayList GetRange(int index, int count) {
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (Count - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        return new Range(this, index, count);
    }

    public override void Reverse(int index, int count) {
        _list.Reverse(index, count);
        _version = _list._version;
    }

    public override void Sort(int index, int count, IComparer comparer) {
        _list.Sort(index, count, comparer);
        _version = _list._version;
    }

    public override Object[] ToArray() {
        return _list.ToArray();
    }

    public override Array ToArray(Type type) {
        return _list.ToArray(type);
    }

    public override void TrimToSize() {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }
}

[Serializable]
private class ReadOnlyList : IList {
    private IList _list;

    internal ReadOnlyList(IList l) {
        _list = l;
    }

    public virtual int Count {
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        get { return _list.Count; }
    }

    public virtual bool IsReadOnly {
        get { return true; }
    }

    public virtual bool IsFixedSize {
        get { return true; }
    }

    public virtual bool IsSynchronized {
        get { return _list.IsSynchronized; }
    }

    public virtual Object this[int index] {
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        get {
            return _list[index];
        }
        set {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
        }
    }

    public virtual Object SyncRoot {
        get { return _list.SyncRoot; }
    }

    public virtual int Add(Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public virtual void Clear() {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public virtual bool Contains(Object obj) {
        return _list.Contains(obj);
    }

    public virtual void CopyTo(Array array, int index) {
        _list.CopyTo(array, index);
    }

#if !FEATURE_CORECLR
    [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
    public virtual IEnumerator GetEnumerator() {
        return _list.GetEnumerator();
    }

    public virtual int IndexOf(Object value) {
        return _list.IndexOf(value);
    }

    public virtual void Insert(int index, Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public virtual void Remove(Object value) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public virtual void RemoveAt(int index) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }
}

[Serializable]
private class ReadOnlyArrayList : ArrayList {
    private ArrayList _list;

    internal ReadOnlyArrayList(ArrayList l) {
        _list = l;
    }

    public override int Count {
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        get { return _list.Count; }
    }

    public override bool IsReadOnly {
        get { return true; }
    }

    public override bool IsFixedSize {
        get { return true; }
    }

    public override bool IsSynchronized {
        get { return _list.IsSynchronized; }
    }

    public override Object this[int index] {
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        get {
            return _list[index];
        }
        set {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
        }
    }

    public override Object SyncRoot {
        get { return _list.SyncRoot; }
    }

    public override int Add(Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override void AddRange(ICollection c) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override int BinarySearch(int index, int count, Object value, IComparer comparer) {
        return _list.BinarySearch(index, count, value, comparer);
    }


    public override int Capacity {
        get { return _list.Capacity; }
        set { throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection")); }
    }

    public override void Clear() {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override Object Clone() {
        ReadOnlyArrayList arrayList = new ReadOnlyArrayList(_list);
        arrayList._list = (ArrayList)_list.Clone();
        return arrayList;
    }

    public override bool Contains(Object obj) {
        return _list.Contains(obj);
    }

    public override void CopyTo(Array array, int index) {
        _list.CopyTo(array, index);
    }

    public override void CopyTo(int index, Array array, int arrayIndex, int count) {
        _list.CopyTo(index, array, arrayIndex, count);
    }

    public override IEnumerator GetEnumerator() {
        return _list.GetEnumerator();
    }

    public override IEnumerator GetEnumerator(int index, int count) {
        return _list.GetEnumerator(index, count);
    }

    public override int IndexOf(Object value) {
        return _list.IndexOf(value);
    }

    public override int IndexOf(Object value, int startIndex) {
        return _list.IndexOf(value, startIndex);
    }

    public override int IndexOf(Object value, int startIndex, int count) {
        return _list.IndexOf(value, startIndex, count);
    }

    public override void Insert(int index, Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override void InsertRange(int index, ICollection c) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override int LastIndexOf(Object value) {
        return _list.LastIndexOf(value);
    }

    public override int LastIndexOf(Object value, int startIndex) {
        return _list.LastIndexOf(value, startIndex);
    }

    public override int LastIndexOf(Object value, int startIndex, int count) {
        return _list.LastIndexOf(value, startIndex, count);
    }

    public override void Remove(Object value) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override void RemoveAt(int index) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override void RemoveRange(int index, int count) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override void SetRange(int index, ICollection c) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override ArrayList GetRange(int index, int count) {
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (Count - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        return new Range(this, index, count);
    }

    public override void Reverse(int index, int count) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override void Sort(int index, int count, IComparer comparer) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override Object[] ToArray() {
        return _list.ToArray();
    }

    public override Array ToArray(Type type) {
        return _list.ToArray(type);
    }

    public override void TrimToSize() {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }
}


// Implements an enumerator for a ArrayList. The enumerator uses the
// internal version number of the list to ensure that no modifications are 
// made to the list while an enumeration is in progress. 
[Serializable]
private sealed class ArrayListEnumerator : IEnumerator, ICloneable {
    private ArrayList list;
    private int index;
    private int endIndex;       // Where to stop. 
    private int version;
    private Object currentElement;
    private int startIndex;     // Save this for Reset. 

    internal ArrayListEnumerator(ArrayList list, int index, int count) {
        this.list = list;
        startIndex = index;
        this.index = index - 1;
        endIndex = this.index + count;  // last valid index 
        version = list._version;
        currentElement = null;
    }

    public Object Clone() {
        return MemberwiseClone();
    }

    public bool MoveNext() {
        if (version != list._version) throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
        if (index < endIndex) {
            currentElement = list[++index];
            return true;
        } else {
            index = endIndex + 1;
        }

        return false;
    }

    public Object Current {
        get {
            if (index < startIndex)
                throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
            else if (index > endIndex) {
                throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
            }
            return currentElement;
        }
    }

    public void Reset() {
        if (version != list._version) throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
        index = startIndex - 1;
    }
}

// Implementation of a generic list subrange. An instance of this class 
// is returned by the default implementation of List.GetRange.
[Serializable]
private class Range : ArrayList {
    private ArrayList _baseList;
    private int _baseIndex;
    private int _baseSize;
    private int _baseVersion;

    internal Range(ArrayList list, int index, int count) : base(false) {
        _baseList = list;
        _baseIndex = index;
        _baseSize = count;
        _baseVersion = list._version;
        // we also need to update _version field to make Range of Range work 
        _version = list._version;
    }

    private void InternalUpdateRange() {
        if (_baseVersion != _baseList._version)
            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnderlyingArrayListChanged"));
    }

    private void InternalUpdateVersion() {
        _baseVersion++;
        _version++;
    }

    public override int Add(Object value) {
        InternalUpdateRange();
        _baseList.Insert(_baseIndex + _baseSize, value);
        InternalUpdateVersion();
        return _baseSize++;
    }

    public override void AddRange(ICollection c) {
        InternalUpdateRange();
        if (c == null) {
            throw new ArgumentNullException("c");
        }

        int count = c.Count;
        if (count > 0) {
            _baseList.InsertRange(_baseIndex + _baseSize, c);
            InternalUpdateVersion();
            _baseSize += count;
        }
    }

    // Other overloads with automatically work 
    public override int BinarySearch(int index, int count, Object value, IComparer comparer) {
        InternalUpdateRange();
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (_baseSize - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        int i = _baseList.BinarySearch(_baseIndex + index, count, value, comparer);
        if (i >= 0) return i - _baseIndex;
        return i + _baseIndex;
    }

    public override int Capacity {
        get {
            return _baseList.Capacity;
        }

        set {
            if (value < Count) throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
        }
    }


    public override void Clear() {
        InternalUpdateRange();
        if (_baseSize != 0) {
            _baseList.RemoveRange(_baseIndex, _baseSize);
            InternalUpdateVersion();
            _baseSize = 0;
        }
    }

    public override Object Clone() {
        InternalUpdateRange();
        Range arrayList = new Range(_baseList, _baseIndex, _baseSize);
        arrayList._baseList = (ArrayList)_baseList.Clone();
        return arrayList;
    }

    public override bool Contains(Object item) {
        InternalUpdateRange();
        if (item == null) {
            for (int i = 0; i < _baseSize; i++)
                if (_baseList[_baseIndex + i] == null)
                    return true;
            return false;
        } else {
            for (int i = 0; i < _baseSize; i++)
                if (_baseList[_baseIndex + i] != null && _baseList[_baseIndex + i].Equals(item))
                    return true;
            return false;
        }
    }

    public override void CopyTo(Array array, int index) {
        InternalUpdateRange();
        if (array == null)
            throw new ArgumentNullException("array");
        if (array.Rank != 1)
            throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
        if (index < 0)
            throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (array.Length - index < _baseSize)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        _baseList.CopyTo(_baseIndex, array, index, _baseSize);
    }

    public override void CopyTo(int index, Array array, int arrayIndex, int count) {
        InternalUpdateRange();
        if (array == null)
            throw new ArgumentNullException("array");
        if (array.Rank != 1)
            throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (array.Length - arrayIndex < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        if (_baseSize - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        _baseList.CopyTo(_baseIndex + index, array, arrayIndex, count);
    }

    public override int Count {
        get {
            InternalUpdateRange();
            return _baseSize;
        }
    }

    public override bool IsReadOnly {
        get { return _baseList.IsReadOnly; }
    }

    public override bool IsFixedSize {
        get { return _baseList.IsFixedSize; }
    }

    public override bool IsSynchronized {
        get { return _baseList.IsSynchronized; }
    }

    public override IEnumerator GetEnumerator() {
        return GetEnumerator(0, _baseSize);
    }

    public override IEnumerator GetEnumerator(int index, int count) {
        InternalUpdateRange();
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (_baseSize - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        return _baseList.GetEnumerator(_baseIndex + index, count);
    }

    public override ArrayList GetRange(int index, int count) {
        InternalUpdateRange();
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (_baseSize - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        return new Range(this, index, count);
    }

    public override Object SyncRoot {
        get {
            return _baseList.SyncRoot;
        }
    }


    public override int IndexOf(Object value) {
        InternalUpdateRange();
        int i = _baseList.IndexOf(value, _baseIndex, _baseSize);
        if (i >= 0) return i - _baseIndex;
        return -1;
    }

    public override int IndexOf(Object value, int startIndex) {
        InternalUpdateRange();
        if (startIndex < 0)
            throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (startIndex > _baseSize)
            throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));

        int i = _baseList.IndexOf(value, _baseIndex + startIndex, _baseSize - startIndex);
        if (i >= 0) return i - _baseIndex;
        return -1;
    }

    public override int IndexOf(Object value, int startIndex, int count) {
        InternalUpdateRange();
        if (startIndex < 0 || startIndex > _baseSize)
            throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));

        if (count < 0 || (startIndex > _baseSize - count))
            throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));

        int i = _baseList.IndexOf(value, _baseIndex + startIndex, count);
        if (i >= 0) return i - _baseIndex;
        return -1;
    }

    public override void Insert(int index, Object value) {
        InternalUpdateRange();
        if (index < 0 || index > _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
        _baseList.Insert(_baseIndex + index, value);
        InternalUpdateVersion();
        _baseSize++;
    }

    public override void InsertRange(int index, ICollection c) {
        InternalUpdateRange();
        if (index < 0 || index > _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));

        if (c == null) {
            throw new ArgumentNullException("c");

        }
        int count = c.Count;
        if (count > 0) {
            _baseList.InsertRange(_baseIndex + index, c);
            _baseSize += count;
            InternalUpdateVersion();
        }
    }

    public override int LastIndexOf(Object value) {
        InternalUpdateRange();
        int i = _baseList.LastIndexOf(value, _baseIndex + _baseSize - 1, _baseSize);
        if (i >= 0) return i - _baseIndex;
        return -1;
    }

    public override int LastIndexOf(Object value, int startIndex) {
        return LastIndexOf(value, startIndex, startIndex + 1);
    }

    public override int LastIndexOf(Object value, int startIndex, int count) {
        InternalUpdateRange();
        if (_baseSize == 0)
            return -1;

        if (startIndex >= _baseSize)
            throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
        if (startIndex < 0)
            throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));

        int i = _baseList.LastIndexOf(value, _baseIndex + startIndex, count);
        if (i >= 0) return i - _baseIndex;
        return -1;
    }

    // Don't need to override Remove

    public override void RemoveAt(int index) {
        InternalUpdateRange();
        if (index < 0 || index >= _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
        _baseList.RemoveAt(_baseIndex + index);
        InternalUpdateVersion();
        _baseSize--;
    }

    public override void RemoveRange(int index, int count) {
        InternalUpdateRange();
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (_baseSize - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        // No need to call _bastList.RemoveRange if count is 0. 
        // In addition, _baseList won't change the vresion number if count is 0.
        if (count > 0) {
            _baseList.RemoveRange(_baseIndex + index, count);
            InternalUpdateVersion();
            _baseSize -= count;
        }
    }

    public override void Reverse(int index, int count) {
        InternalUpdateRange();
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (_baseSize - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        _baseList.Reverse(_baseIndex + index, count);
        InternalUpdateVersion();
    }


    public override void SetRange(int index, ICollection c) {
        InternalUpdateRange();
        if (index < 0 || index >= _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
        _baseList.SetRange(_baseIndex + index, c);
        if (c.Count > 0) {
            InternalUpdateVersion();
        }
    }

    public override void Sort(int index, int count, IComparer comparer) {
        InternalUpdateRange();
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (_baseSize - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        _baseList.Sort(_baseIndex + index, count, comparer);
        InternalUpdateVersion();
    }

    public override Object this[int index] {
        get {
            InternalUpdateRange();
            if (index < 0 || index >= _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            return _baseList[_baseIndex + index];
        }
        set {
            InternalUpdateRange();
            if (index < 0 || index >= _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            _baseList[_baseIndex + index] = value;
            InternalUpdateVersion();
        }
    }

    public override Object[] ToArray() {
        InternalUpdateRange();
        Object[] array = new Object[_baseSize];
        Array.Copy(_baseList._items, _baseIndex, array, 0, _baseSize);
        return array;
    }

    [SecuritySafeCritical]
    public override Array ToArray(Type type) {
        InternalUpdateRange();
        if (type == null)
            throw new ArgumentNullException("type");
        Array array = Array.UnsafeCreateInstance(type, _baseSize);
        _baseList.CopyTo(_baseIndex, array, 0, _baseSize);
        return array;
    }

    public override void TrimToSize() {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_RangeCollection"));
    }
}

[Serializable]
private sealed class ArrayListEnumeratorSimple : IEnumerator, ICloneable {
    private ArrayList list;
    private int index;
    private int version;
    private Object currentElement;
    [NonSerialized]
    private bool isArrayList;
    // this object is used to indicate enumeration has not started or has terminated
    static Object dummyObject = new Object();

    internal ArrayListEnumeratorSimple(ArrayList list) {
        this.list = list;
        this.index = -1;
        version = list._version;
        isArrayList = (list.GetType() == typeof(ArrayList));
        currentElement = dummyObject;
    }

    public Object Clone() {
        return MemberwiseClone();
    }

    public bool MoveNext() {
        if (version != list._version) {
            throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
        }

        if (isArrayList) {  // avoid calling virtual methods if we are operating on ArrayList to improve performance 
            if (index < list._size - 1) {
                currentElement = list._items[++index];
                return true;
            } else {
                currentElement = dummyObject;
                index = list._size;
                return false;
            }
        } else {
            if (index < list.Count - 1) {
                currentElement = list[++index];
                return true;
            } else {
                index = list.Count;
                currentElement = dummyObject;
                return false;
            }
        }
    }

    public Object Current {
        get {
            object temp = currentElement;
            if (dummyObject == temp) { // check if enumeration has not started or has terminated 
                if (index == -1) {
                    throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                } else {
                    throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
                }
            }

            return temp;
        }
    }

    public void Reset() {
        if (version != list._version) {
            throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
        }

        currentElement = dummyObject;
        index = -1;
    }
}

internal class ArrayListDebugView {
    private ArrayList arrayList;

    public ArrayListDebugView(ArrayList arrayList) {
        if (arrayList == null)
            throw new ArgumentNullException("arrayList");

        this.arrayList = arrayList;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public Object[] Items {
        get {
            return arrayList.ToArray();
        }
    }
} 
    }
} 
 
// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  ArrayList 
**
** <owner>[....]</owner> 
**
**
** Purpose: Implements a dynamically sized List as an array,
**          and provides many convenience methods for treating 
**          an array as an IList.
** 
** 
===========================================================*/
namespace System.Collections {
    using System;
    using System.Runtime;
    using System.Security;
    using System.Security.Permissions;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Diagnostics.Contracts;

    // Implements a variable-size List that uses an array of objects to store the 
    // elements. A ArrayList has a capacity, which is the allocated length
    // of the internal array. As elements are added to a ArrayList, the capacity
    // of the ArrayList is automatically increased as required by reallocating the
    // internal array. 
    //
    [DebuggerTypeProxy(typeof(System.Collections.ArrayList.ArrayListDebugView))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class ArrayList : IList, ICloneable {
        private Object[] _items;
        [ContractPublicPropertyName("Count")]
        private int _size;
        private int _version;
        [NonSerialized]
        private Object _syncRoot;

        private const int _defaultCapacity = 4;
        private static readonly Object[] emptyArray = new Object[0];

        // Note: this constructor is a bogus constructor that does nothing 
        // and is for use only with SyncArrayList.
        internal ArrayList(bool trash) {
        }

        // Constructs a ArrayList. The list is initially empty and has a capacity
        // of zero. Upon adding the first element to the list the capacity is
        // increased to _defaultCapacity, and then increased in multiples of two as required.
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif 
        public ArrayList() {
            _items = emptyArray;
        }

        // Constructs a ArrayList with a given initial capacity. The list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required. 
        //
        public ArrayList(int capacity) {
            if (capacity < 0) throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", "capacity"));
            Contract.EndContractBlock();
            _items = new Object[capacity];
        }

        // Constructs a ArrayList, copying the contents of the given collection. The
        // size and capacity of the new list will both be equal to the size of the 
        // given collection.
        // 
        public ArrayList(ICollection c) {
            if (c == null)
                throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
            Contract.EndContractBlock();
            _items = new Object[c.Count];
            AddRange(c);
        }

        // Gets and sets the capacity of this list.  The capacity is the size of 
        // the internal array used to hold items.  When set, the internal 
        // array of the list is reallocated to the given capacity.
        // 
        public virtual int Capacity {
            get {
                Contract.Ensures(Contract.Result<int>() >= Count);
                return _items.Length;
            }
            set {
                if (value < _size) {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                }
                Contract.Ensures(Capacity >= 0);
                Contract.EndContractBlock();
                // We don't want to update the version number when we change the capacity.
                // Some existing applications have dependency on this. 
                if (value != _items.Length) {
                    if (value > 0) {
                        Object[] newItems = new Object[value];
                        if (_size > 0) {
                            Array.Copy(_items, 0, newItems, 0, _size);
                        }
                        _items = newItems;
                    } else {
                        _items = new Object[_defaultCapacity];
                    }
                }
            }
        }

        // Read-only property describing how many elements are in the List.
        public virtual int Count {
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return _size;
            }
        }

        public virtual bool IsFixedSize {
            get { return false; }
        }


        // Is this ArrayList read-only? 
        public virtual bool IsReadOnly {
            get { return false; }
        }

        // Is this ArrayList synchronized (thread-safe)?
        public virtual bool IsSynchronized {
            get { return false; }
        }

        // Synchronization root for this object. 
        public virtual Object SyncRoot {
            get {
                if (_syncRoot == null) {
                    System.Threading.Interlocked.CompareExchange<object>(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        // Sets or Gets the element at the given index.
        // 
        public virtual Object this[int index] {
            get {
                if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                Contract.EndContractBlock();
                return _items[index];
            }
            set {
                if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                Contract.EndContractBlock();
                _items[index] = value;
                _version++;
            }
        }

        // Creates a ArrayList wrapper for a particular IList.  This does not 
        // copy the contents of the IList, but only wraps the ILIst.  So any 
        // changes to the underlying list will affect the ArrayList.  This would
        // be useful if you want to Reverse a subrange of an IList, or want to 
        // use a generic BinarySearch or Sort method without implementing one yourself.
        // However, since these methods are generic, the performance may not be
        // nearly as good for some operations as they would be on the IList itself.
        // 
        public static ArrayList Adapter(IList list) {
            if (list == null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<arraylist>() != null);
            Contract.EndContractBlock();
            return new IListWrapper(list);
        }

        // Adds the given object to the end of this list. The size of the list is 
        // increased by one. If required, the capacity of the list is doubled
        // before adding the new element. 
        // 
        public virtual int Add(Object value) {
            Contract.Ensures(Contract.Result<int>() >= 0);
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            _items[_size] = value;
            _version++;
            return _size++;
        }

        // Adds the elements of the given collection to the end of this list. If 
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger. 
        //
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif 
        public virtual void AddRange(ICollection c) {
            InsertRange(_size, c);
        }

        // Searches a section of the list for a given element using a binary search 
        // algorithm. Elements of the list are compared to the search value using
        // the given IComparer interface. If comparer is null, elements of
        // the list are compared to the search value using the IComparable
        // interface, which in that case must be implemented by all elements of the 
        // list and the given search value. This method assumes that the given
        // section of the list is already sorted; if this is not the case, the 
        // result will be incorrect. 
        //
        // The method returns the index of the given value in the list. If the 
        // list does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value. This is also the index at which 
        // the search value should be inserted into the list in order for the list
        // to remain sorted. 
        // 
        // The method uses the Array.BinarySearch method to perform the
        // search. 
        //
        public virtual int BinarySearch(int index, int count, Object value, IComparer comparer) {
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.Ensures(Contract.Result<int>() < index + count);
            Contract.EndContractBlock();

            return Array.BinarySearch((Array)_items, index, count, value, comparer);
        }

        public virtual int BinarySearch(Object value) {
            Contract.Ensures(Contract.Result<int>() < Count);
            return BinarySearch(0, Count, value, null);
        }

        public virtual int BinarySearch(Object value, IComparer comparer) {
            Contract.Ensures(Contract.Result<int>() < Count);
            return BinarySearch(0, Count, value, comparer);
        }


        // Clears the contents of ArrayList. 
        public virtual void Clear() {
            if (_size > 0) {
                Array.Clear(_items, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references. 
                _size = 0;
            }
            _version++;
        }

        // Clones this ArrayList, doing a shallow copy.  (A copy is made of all
        // Object references in the ArrayList, but the Objects pointed to
        // are not cloned).
        public virtual Object Clone() {
            Contract.Ensures(Contract.Result<object>() != null);
            ArrayList la = new ArrayList(_size);
            la._size = _size;
            la._version = _version;
            Array.Copy(_items, 0, la._items, 0, _size);
            return la;
        }


        // Contains returns true if the specified element is in the ArrayList. 
        // It does a linear, O(n) search.  Equality is determined by calling 
        // item.Equals().
        // 
        public virtual bool Contains(Object item) {
            if (item == null) {
                for (int i = 0; i < _size; i++)
                    if (_items[i] == null)
                        return true;
                return false;
            } else {
                for (int i = 0; i < _size; i++)
                    if ((_items[i] != null) && (_items[i].Equals(item)))
                        return true;
                return false;
            }
        }

        // Copies this ArrayList into array, which must be of a 
        // compatible array type.
        // 
        public virtual void CopyTo(Array array) {
            CopyTo(array, 0);
        }

        // Copies this ArrayList into array, which must be of a
        // compatible array type. 
        // 
        public virtual void CopyTo(Array array, int arrayIndex) {
            if ((array != null) && (array.Rank != 1))
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
            Contract.EndContractBlock();
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        // Copies a section of this list to the given array at the given index. 
        //
        // The method uses the Array.Copy method to copy the elements. 
        //
        public virtual void CopyTo(int index, Array array, int arrayIndex, int count) {
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            if ((array != null) && (array.Rank != 1))
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
            Contract.EndContractBlock();
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, index, array, arrayIndex, count);
        }

        // Ensures that the capacity of this list is at least the given minimum
        // value. If the currect capacity of the list is less than min, the 
        // capacity is increased to twice the current capacity or to min,
        // whichever is larger. 
        private void EnsureCapacity(int min) {
            if (_items.Length < min) {
                int newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length * 2;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }

        // Returns a list wrapper that is fixed at the current size.  Operations 
        // that add or remove items will fail, however, replacing items is allowed. 
        //
        public static IList FixedSize(IList list) {
            if (list == null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<ilist>() != null);
            Contract.EndContractBlock();
            return new FixedSizeList(list);
        }

        // Returns a list wrapper that is fixed at the current size.  Operations
        // that add or remove items will fail, however, replacing items is allowed. 
        //
        public static ArrayList FixedSize(ArrayList list) {
            if (list == null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<arraylist>() != null);
            Contract.EndContractBlock();
            return new FixedSizeArrayList(list);
        }

        // Returns an enumerator for this list with the given
        // permission for removal of elements. If modifications made to the list
        // while an enumeration is in progress, the MoveNext and
        // GetObject methods of the enumerator will throw an exception. 
        //
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public virtual IEnumerator GetEnumerator() {
            Contract.Ensures(Contract.Result<ienumerator>() != null);
            return new ArrayListEnumeratorSimple(this);
        }

        // Returns an enumerator for a section of this list with the given
        // permission for removal of elements. If modifications made to the list 
        // while an enumeration is in progress, the MoveNext and 
        // GetObject methods of the enumerator will throw an exception.
        // 
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
#if false 
        private void TrimSrcHack() {}    // workaround to avoid unclosed "#if !FEATURE_CORECLR"
#endif 
        public virtual IEnumerator GetEnumerator(int index, int count) {
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.Ensures(Contract.Result<ienumerator>() != null);
            Contract.EndContractBlock();

            return new ArrayListEnumerator(this, index, count);
        }

        // Returns the index of the first occurrence of a given value in a range of 
        // this list. The list is searched forwards from beginning to end.
        // The elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search. 
        // 
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public virtual int IndexOf(Object value) {
            Contract.Ensures(Contract.Result<int>() < Count);
            return Array.IndexOf((Array)_items, value, 0, _size);
        }

        // Returns the index of the first occurrence of a given value in a range of 
        // this list. The list is searched forwards, starting at index
        // startIndex and ending at count number of elements. The 
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        //
        // This method uses the Array.IndexOf method to perform the 
        // search.
        // 
        public virtual int IndexOf(Object value, int startIndex) {
            if (startIndex > _size)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();
            return Array.IndexOf((Array)_items, value, startIndex, _size - startIndex);
        }

        // Returns the index of the first occurrence of a given value in a range of 
        // this list. The list is searched forwards, starting at index 
        // startIndex and upto count number of elements. The
        // elements of the list are compared to the given value using the 
        // Object.Equals method.
        //
        // This method uses the Array.IndexOf method to perform the
        // search. 
        //
        public virtual int IndexOf(Object value, int startIndex, int count) {
            if (startIndex > _size)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (count < 0 || startIndex > _size - count) throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();
            return Array.IndexOf((Array)_items, value, startIndex, count);
        }

        // Inserts an element into this list at a given index. The size of the list 
        // is increased by one. If required, the capacity of the list is doubled 
        // before inserting the new element.
        // 
        public virtual void Insert(int index, Object value) {
            // Note that insertions at the end are legal.
            if (index < 0 || index > _size) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_ArrayListInsert"));
            //Contract.Ensures(Count == Contract.OldValue(Count) + 1); 
            Contract.EndContractBlock();

            if (_size == _items.Length) EnsureCapacity(_size + 1);
            if (index < _size) {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = value;
            _size++;
            _version++;
        }

        // Inserts the elements of the given collection at a given index. If 
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.  Ranges may be added 
        // to the end of the list by setting index to the ArrayList's size.
        //
        public virtual void InsertRange(int index, ICollection c) {
            if (c == null)
                throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
            if (index < 0 || index > _size) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            //Contract.Ensures(Count == Contract.OldValue(Count) + c.Count); 
            Contract.EndContractBlock();

            int count = c.Count;
            if (count > 0) {
                EnsureCapacity(_size + count);
                // shift existing items 
                if (index < _size) {
                    Array.Copy(_items, index, _items, index + count, _size - index);
                }

                Object[] itemsToInsert = new Object[count];
                c.CopyTo(itemsToInsert, 0);
                itemsToInsert.CopyTo(_items, index);
                _size += count;
                _version++;
            }
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at the end 
        // and ending at the first element in the list. The elements of the list
        // are compared to the given value using the Object.Equals method.
        //
        // This method uses the Array.LastIndexOf method to perform the 
        // search.
        // 
        public virtual int LastIndexOf(Object value) {
            Contract.Ensures(Contract.Result<int>() < _size);
            return LastIndexOf(value, _size - 1, _size);
        }

        // Returns the index of the last occurrence of a given value in a range of 
        // this list. The list is searched backwards, starting at index
        // startIndex and ending at the first element in the list. The 
        // elements of the list are compared to the given value using the 
        // Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        //
        public virtual int LastIndexOf(Object value, int startIndex) {
            if (startIndex >= _size)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();
            return LastIndexOf(value, startIndex, startIndex + 1);
        }

        // Returns the index of the last occurrence of a given value in a range of 
        // this list. The list is searched backwards, starting at index
        // startIndex and upto count elements. The elements of 
        // the list are compared to the given value using the Object.Equals 
        // method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        //
        public virtual int LastIndexOf(Object value, int startIndex, int count) {
            if (Count != 0 && (startIndex < 0 || count < 0))
                throw new ArgumentOutOfRangeException((startIndex < 0 ? "startIndex" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();

            if (_size == 0)  // Special case for an empty list
                return -1;

            if (startIndex >= _size || count > startIndex + 1)
                throw new ArgumentOutOfRangeException((startIndex >= _size ? "startIndex" : "count"), Environment.GetResourceString("ArgumentOutOfRange_BiggerThanCollection"));

            return Array.LastIndexOf((Array)_items, value, startIndex, count);
        }

        // Returns a read-only IList wrapper for the given IList.
        //
        public static IList ReadOnly(IList list) {
            if (list == null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<ilist>() != null);
            Contract.EndContractBlock();
            return new ReadOnlyList(list);
        }

        // Returns a read-only ArrayList wrapper for the given ArrayList.
        //
        public static ArrayList ReadOnly(ArrayList list) {
            if (list == null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<arraylist>() != null);
            Contract.EndContractBlock();
            return new ReadOnlyArrayList(list);
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one. 
        //
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public virtual void Remove(Object obj) {
            Contract.Ensures(Count >= 0);

            int index = IndexOf(obj);
            BCLDebug.Correctness(index >= 0 || !(obj is Int32), "You passed an Int32 to Remove that wasn't in the ArrayList." + Environment.NewLine + "Did you mean RemoveAt?  int: " + obj + "  Count: " + Count);
            if (index >= 0)
                RemoveAt(index);
        }

        // Removes the element at the given index. The size of the list is 
        // decreased by one.
        //
        public virtual void RemoveAt(int index) {
            if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.Ensures(Count >= 0);
            //Contract.Ensures(Count == Contract.OldValue(Count) - 1); 
            Contract.EndContractBlock();

            _size--;
            if (index < _size) {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = null;
            _version++;
        }

        // Removes a range of elements from this list.
        // 
        public virtual void RemoveRange(int index, int count) {
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.Ensures(Count >= 0);
            //Contract.Ensures(Count == Contract.OldValue(Count) - count); 
            Contract.EndContractBlock();

            if (count > 0) {
                int i = _size;
                _size -= count;
                if (index < _size) {
                    Array.Copy(_items, index + count, _items, index, _size - index);
                }
                while (i > _size) _items[--i] = null;
                _version++;
            }
        }

        // Returns an IList that contains count copies of value.
        // 
        public static ArrayList Repeat(Object value, int count) {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.Ensures(Contract.Result<arraylist>() != null);
            Contract.EndContractBlock();

            ArrayList list = new ArrayList((count > _defaultCapacity) ? count : _defaultCapacity);
            for (int i = 0; i < count; i++) list.add(value); return list; } reverses the elements in this list. public virtual void reverse() { reverse(0, count); a range of following call to method, an element given by index and count which was previously located at i will now be + (index - 1). method uses array.reverse reverse elements. reverse(int index, int count) if < 0 || 0) throw new argumentoutofrangeexception((index<0 ? "index" : "count"), environment.getresourcestring("argumentoutofrange_neednonnegnum")); (_size argumentexception(environment.getresourcestring("argument_invalidofflen")); contract.endcontractblock(); array.reverse(_items, _version++; sets starting collection. setrange(int icollection c) (c="=null)" argumentnullexception("c", environment.getresourcestring("argumentnull_collection"));=""> _size - count) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
 
            if (count > 0) { 
                c.CopyTo(_items, index);
                _version++; 
            }
}

public virtual ArrayList GetRange(int index, int count) {
    if (index < 0 || count < 0)
        throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
    if (_size - index < count)
        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
    Contract.Ensures(Contract.Result<arraylist>() != null);
    Contract.EndContractBlock();
    return new Range(this, index, count);
}

// Sorts the elements in this list.  Uses the default comparer and
// Array.Sort.
public virtual void Sort() {
    Sort(0, Count, Comparer.Default);
}

// Sorts the elements in this list.  Uses Array.Sort with the 
// provided comparer.
public virtual void Sort(IComparer comparer) {
    Sort(0, Count, comparer);
}

// Sorts the elements in a section of this list. The sort compares the 
// elements to each other using the given IComparer interface. If
// comparer is null, the elements are compared to each other using 
// the IComparable interface, which in that case must be implemented by all
// elements of the list.
//
// This method uses the Array.Sort method to sort the elements. 
//
public virtual void Sort(int index, int count, IComparer comparer) {
    if (index < 0 || count < 0)
        throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
    if (_size - index < count)
        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
    Contract.EndContractBlock();

    Array.Sort(_items, index, count, comparer);
    _version++;
}

// Returns a thread-safe wrapper around an IList.
// 
[HostProtection(Synchronization = true)]
public static IList Synchronized(IList list) {
    if (list == null)
        throw new ArgumentNullException("list");
    Contract.Ensures(Contract.Result<ilist>() != null);
    Contract.EndContractBlock();
    return new SyncIList(list);
}

// Returns a thread-safe wrapper around a ArrayList.
//
[HostProtection(Synchronization = true)]
public static ArrayList Synchronized(ArrayList list) {
    if (list == null)
        throw new ArgumentNullException("list");
    Contract.Ensures(Contract.Result<arraylist>() != null);
    Contract.EndContractBlock();
    return new SyncArrayList(list);
}

// ToArray returns a new Object array containing the contents of the ArrayList.
// This requires copying the ArrayList, which is an O(n) operation. 
public virtual Object[] ToArray() {
    Contract.Ensures(Contract.Result<object[]>() != null);

    Object[] array = new Object[_size];
    Array.Copy(_items, 0, array, 0, _size);
    return array;
}

// ToArray returns a new array of a particular type containing the contents 
// of the ArrayList.  This requires copying the ArrayList and potentially
// downcasting all elements.  This copy may fail and is an O(n) operation. 
// Internally, this implementation calls Array.Copy. 
//
[SecuritySafeCritical]
public virtual Array ToArray(Type type) {
    if (type == null)
        throw new ArgumentNullException("type");
    Contract.Ensures(Contract.Result<array>() != null);
    Contract.EndContractBlock();
    Array array = Array.UnsafeCreateInstance(type, _size);
    Array.Copy(_items, 0, array, 0, _size);
    return array;
}

// Sets the capacity of this list to the size of the list. This method can
// be used to minimize a list's memory overhead once it is known that no
// new elements will be added to the list. To completely clear a list and 
// release all memory referenced by the list, execute the following
// statements: 
// 
// list.Clear();
// list.TrimToSize(); 
//
public virtual void TrimToSize() {
    Capacity = _size;
}


// This class wraps an IList, exposing it as a ArrayList 
// Note this requires reimplementing half of ArrayList...
[Serializable]
private class IListWrapper : ArrayList {
    private IList _list;

    internal IListWrapper(IList list) {
        _list = list;
        _version = 0; // list doesn't not contain a version number 
    }

    public override int Capacity {
        get { return _list.Count; }
        set {
            if (value < _list.Count) throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
        }
    }

    public override int Count {
        get { return _list.Count; }
    }

    public override bool IsReadOnly {
        get { return _list.IsReadOnly; }
    }

    public override bool IsFixedSize {
        get { return _list.IsFixedSize; }
    }


    public override bool IsSynchronized {
        get { return _list.IsSynchronized; }
    }

    public override Object this[int index] {
        get {
            return _list[index];
        }
        set {
            _list[index] = value;
            _version++;
        }
    }

    public override Object SyncRoot {
        get { return _list.SyncRoot; }
    }

    public override int Add(Object obj) {
        int i = _list.Add(obj);
        _version++;
        return i;
    }

    public override void AddRange(ICollection c) {
        InsertRange(Count, c);
    }

    // Other overloads with automatically work 
    public override int BinarySearch(int index, int count, Object value, IComparer comparer) {
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (_list.Count - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        if (comparer == null)
            comparer = Comparer.Default;

        int lo = index;
        int hi = index + count - 1;
        int mid;
        while (lo <= hi) {
            mid = (lo + hi) / 2;
            int r = comparer.Compare(value, _list[mid]);
            if (r == 0)
                return mid;
            if (r < 0)
                hi = mid - 1;
            else
                lo = mid + 1;
        }
        // return bitwise complement of the first element greater than value. 
        // Since hi is less than lo now, ~lo is the correct item.
        return ~lo;
    }

    public override void Clear() {
        // If _list is an array, it will support Clear method. 
        // We shouldn't allow clear operation on a FixedSized ArrayList 
        if (_list.IsFixedSize) {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
        }

        _list.Clear();
        _version++;
    }

    public override Object Clone() {
        // This does not do a shallow copy of _list into a ArrayList!
        // This clones the IListWrapper, creating another wrapper class! 
        return new IListWrapper(_list);
    }

    public override bool Contains(Object obj) {
        return _list.Contains(obj);
    }

    public override void CopyTo(Array array, int index) {
        _list.CopyTo(array, index);
    }

    public override void CopyTo(int index, Array array, int arrayIndex, int count) {
        if (array == null)
            throw new ArgumentNullException("array");
        if (index < 0 || arrayIndex < 0)
            throw new ArgumentOutOfRangeException((index < 0) ? "index" : "arrayIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (count < 0)
            throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (array.Length - arrayIndex < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        if (array.Rank != 1)
            throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
        Contract.EndContractBlock();

        if (_list.Count - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));

        for (int i = index; i < index + count; i++) array.setvalue(_list[i], arrayindex++); } public override ienumerator getenumerator() { return _list.getenumerator(); getenumerator(int index, int count) if (index < 0 || count 0) throw new argumentoutofrangeexception((index<0 ? "index" : "count"), environment.getresourcestring("argumentoutofrange_neednonnegnum")); contract.endcontractblock(); (_list.count - index argumentexception(environment.getresourcestring("argument_invalidofflen")); ilistwrapperenumwrapper(this, count); indexof(object value) _list.indexof(value); value, startindex) indexof(value, startindex, _list.count startindex); (startindex startindex=""> _list.Count) throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                if (count< 0 || startIndex> _list.Count - count) throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
 
                int endIndex = startIndex + count; 
                if (value == null) {
                    for(int i=startIndex; i<endindex; i++) if (_list[i]="=" null) return i; -1; } else { for(int i="startIndex;" i<endindex; !="null" && _list[i].equals(value)) public override void insert(int index, object obj) _list.insert(index, obj); _version++; insertrange(int icollection c) (c="=null)" throw new argumentnullexception("c", environment.getresourcestring("argumentnull_collection")); (index < 0 || index=""> _list.Count) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
  
                if(c.Count > 0) {
                    ArrayList al = _list as ArrayList;
                    if(al != null) {
                        // We need to special case ArrayList. 
                        // When c is a range of _list, we need to handle this in a special way.
                        // See ArrayList.InsertRange for details. 
                        al.InsertRange(index, c); 
                    }
                    else { 
                        IEnumerator en = c.GetEnumerator();
                        while(en.MoveNext()) {
                            _list.Insert(index++, en.Current);
                        } 
                    }
                    _version++; 
                } 
            }
  
            public override int LastIndexOf(Object value) {
    return LastIndexOf(value, _list.Count - 1, _list.Count);
}

public override int LastIndexOf(Object value, int startIndex) {
    return LastIndexOf(value, startIndex, startIndex + 1);
}

public override int LastIndexOf(Object value, int startIndex, int count) {
    if (_list.Count == 0)
        return -1;

    if (startIndex < 0 || startIndex >= _list.Count) throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
    if (count < 0 || count > startIndex + 1) throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));

    int endIndex = startIndex - count + 1;
    if (value == null) {
        for (int i = startIndex; i >= endIndex; i--)
            if (_list[i] == null)
                return i;
        return -1;
    } else {
        for (int i = startIndex; i >= endIndex; i--)
            if (_list[i] != null && _list[i].Equals(value))
                return i;
        return -1;
    }
}

public override void Remove(Object value) {
    int index = IndexOf(value);
    if (index >= 0)
        RemoveAt(index);
}

public override void RemoveAt(int index) {
    _list.RemoveAt(index);
    _version++;
}

public override void RemoveRange(int index, int count) {
    if (index < 0 || count < 0)
        throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
    Contract.EndContractBlock();
    if (_list.Count - index < count)
        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));

    if (count > 0)    // be consistent with ArrayList
        _version++;

    while (count > 0) {
        _list.RemoveAt(index);
        count--;
    }
}

public override void Reverse(int index, int count) {
    if (index < 0 || count < 0)
        throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
    Contract.EndContractBlock();
    if (_list.Count - index < count)
        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));

    int i = index;
    int j = index + count - 1;
    while (i < j) {
        Object tmp = _list[i];
        _list[i++] = _list[j];
        _list[j--] = tmp;
    }
    _version++;
}

public override void SetRange(int index, ICollection c) {
    if (c == null) {
        throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
    }
    Contract.EndContractBlock();

    if (index < 0 || index > _list.Count - c.Count) {
        throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
    }

    if (c.Count > 0) {
        IEnumerator en = c.GetEnumerator();
        while (en.MoveNext()) {
            _list[index++] = en.Current;
        }
        _version++;
    }
}

public override ArrayList GetRange(int index, int count) {
    if (index < 0 || count < 0)
        throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
    Contract.EndContractBlock();
    if (_list.Count - index < count)
        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
    return new Range(this, index, count);
}

public override void Sort(int index, int count, IComparer comparer) {
    if (index < 0 || count < 0)
        throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
    Contract.EndContractBlock();
    if (_list.Count - index < count)
        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));

    Object[] array = new Object[count];
    CopyTo(index, array, 0, count);
    Array.Sort(array, 0, count, comparer);
    for (int i = 0; i < count; i++) _list[i + index] = "array[i];" _version++; } public override object[] toarray() { array="new" object[count]; _list.copyto(array, 0); return array; [securitysafecritical] toarray(type type) if (type="=null)" throw new argumentnullexception("type"); contract.endcontractblock(); _list.count); void trimtosize() can't really do much here... this is the enumerator for an ilist that's been wrapped in another class that implements all of arraylist's methods. [serializable] private sealed ilistwrapperenumwrapper : ienumerator, icloneable ienumerator _en; int _remaining; _initialstartindex; reset _initialcount; bool _firstcall; firstcall to movenext ilistwrapperenumwrapper() internal ilistwrapperenumwrapper(ilistwrapper listwrapper, startindex, count) _en="listWrapper.GetEnumerator();" _initialstartindex="startIndex;" _initialcount="count;" while(startindex--=""> 0 && _en.MoveNext()); 
                    _remaining = count;
                    _firstCall = true;
                }
  
                public Object Clone() {
    // We must clone the underlying enumerator, I think. 
    IListWrapperEnumWrapper clone = new IListWrapperEnumWrapper();
    clone._en = (IEnumerator)((ICloneable)_en).Clone();
    clone._initialStartIndex = _initialStartIndex;
    clone._initialCount = _initialCount;
    clone._remaining = _remaining;
    clone._firstCall = _firstCall;
    return clone;
}

public bool MoveNext() {
    if (_firstCall) {
        _firstCall = false;
        return _remaining-- > 0 && _en.MoveNext();
    }
    if (_remaining < 0)
        return false;
    bool r = _en.MoveNext();
    return r && _remaining-- > 0;
}

public Object Current {
    get {
        if (_firstCall)
            throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
        if (_remaining < 0)
            throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
        return _en.Current;
    }
}

public void Reset() {
    _en.Reset();
    int startIndex = _initialStartIndex;
    while (startIndex-- > 0 && _en.MoveNext()) ;
    _remaining = _initialCount;
    _firstCall = true;
} 
            }
        } 
 
 
        [Serializable]
private class SyncArrayList : ArrayList {
    private ArrayList _list;
    private Object _root;

    internal SyncArrayList(ArrayList list)
        : base(false) {
        _list = list;
        _root = list.SyncRoot;
    }

    public override int Capacity {
        get {
            lock (_root) {
                return _list.Capacity;
            }
        }
        set {
            lock (_root) {
                _list.Capacity = value;
            }
        }
    }

    public override int Count {
        get { lock (_root) { return _list.Count; } }
    }

    public override bool IsReadOnly {
        get { return _list.IsReadOnly; }
    }

    public override bool IsFixedSize {
        get { return _list.IsFixedSize; }
    }


    public override bool IsSynchronized {
        get { return true; }
    }

    public override Object this[int index] {
        get {
            lock (_root) {
                return _list[index];
            }
        }
        set {
            lock (_root) {
                _list[index] = value;
            }
        }
    }

    public override Object SyncRoot {
        get { return _root; }
    }

    public override int Add(Object value) {
        lock (_root) {
            return _list.Add(value);
        }
    }

    public override void AddRange(ICollection c) {
        lock (_root) {
            _list.AddRange(c);
        }
    }

    public override int BinarySearch(Object value) {
        lock (_root) {
            return _list.BinarySearch(value);
        }
    }

    public override int BinarySearch(Object value, IComparer comparer) {
        lock (_root) {
            return _list.BinarySearch(value, comparer);
        }
    }

    public override int BinarySearch(int index, int count, Object value, IComparer comparer) {
        lock (_root) {
            return _list.BinarySearch(index, count, value, comparer);
        }
    }

    public override void Clear() {
        lock (_root) {
            _list.Clear();
        }
    }

    public override Object Clone() {
        lock (_root) {
            return new SyncArrayList((ArrayList)_list.Clone());
        }
    }

    public override bool Contains(Object item) {
        lock (_root) {
            return _list.Contains(item);
        }
    }

    public override void CopyTo(Array array) {
        lock (_root) {
            _list.CopyTo(array);
        }
    }

    public override void CopyTo(Array array, int index) {
        lock (_root) {
            _list.CopyTo(array, index);
        }
    }

    public override void CopyTo(int index, Array array, int arrayIndex, int count) {
        lock (_root) {
            _list.CopyTo(index, array, arrayIndex, count);
        }
    }

    public override IEnumerator GetEnumerator() {
        lock (_root) {
            return _list.GetEnumerator();
        }
    }

    public override IEnumerator GetEnumerator(int index, int count) {
        lock (_root) {
            return _list.GetEnumerator(index, count);
        }
    }

    public override int IndexOf(Object value) {
        lock (_root) {
            return _list.IndexOf(value);
        }
    }

    public override int IndexOf(Object value, int startIndex) {
        lock (_root) {
            return _list.IndexOf(value, startIndex);
        }
    }

    public override int IndexOf(Object value, int startIndex, int count) {
        lock (_root) {
            return _list.IndexOf(value, startIndex, count);
        }
    }

    public override void Insert(int index, Object value) {
        lock (_root) {
            _list.Insert(index, value);
        }
    }

    public override void InsertRange(int index, ICollection c) {
        lock (_root) {
            _list.InsertRange(index, c);
        }
    }

    public override int LastIndexOf(Object value) {
        lock (_root) {
            return _list.LastIndexOf(value);
        }
    }

    public override int LastIndexOf(Object value, int startIndex) {
        lock (_root) {
            return _list.LastIndexOf(value, startIndex);
        }
    }

    public override int LastIndexOf(Object value, int startIndex, int count) {
        lock (_root) {
            return _list.LastIndexOf(value, startIndex, count);
        }
    }

    public override void Remove(Object value) {
        lock (_root) {
            _list.Remove(value);
        }
    }

    public override void RemoveAt(int index) {
        lock (_root) {
            _list.RemoveAt(index);
        }
    }

    public override void RemoveRange(int index, int count) {
        lock (_root) {
            _list.RemoveRange(index, count);
        }
    }

    public override void Reverse(int index, int count) {
        lock (_root) {
            _list.Reverse(index, count);
        }
    }

    public override void SetRange(int index, ICollection c) {
        lock (_root) {
            _list.SetRange(index, c);
        }
    }

    public override ArrayList GetRange(int index, int count) {
        lock (_root) {
            return _list.GetRange(index, count);
        }
    }

    public override void Sort() {
        lock (_root) {
            _list.Sort();
        }
    }

    public override void Sort(IComparer comparer) {
        lock (_root) {
            _list.Sort(comparer);
        }
    }

    public override void Sort(int index, int count, IComparer comparer) {
        lock (_root) {
            _list.Sort(index, count, comparer);
        }
    }

    public override Object[] ToArray() {
        lock (_root) {
            return _list.ToArray();
        }
    }

    public override Array ToArray(Type type) {
        lock (_root) {
            return _list.ToArray(type);
        }
    }

    public override void TrimToSize() {
        lock (_root) {
            _list.TrimToSize();
        }
    }
}


[Serializable]
private class SyncIList : IList {
    private IList _list;
    private Object _root;

    internal SyncIList(IList list) {
        _list = list;
        _root = list.SyncRoot;
    }

    public virtual int Count {
        get { lock (_root) { return _list.Count; } }
    }

    public virtual bool IsReadOnly {
        get { return _list.IsReadOnly; }
    }

    public virtual bool IsFixedSize {
        get { return _list.IsFixedSize; }
    }


    public virtual bool IsSynchronized {
        get { return true; }
    }

    public virtual Object this[int index] {
        get {
            lock (_root) {
                return _list[index];
            }
        }
        set {
            lock (_root) {
                _list[index] = value;
            }
        }
    }

    public virtual Object SyncRoot {
        get { return _root; }
    }

    public virtual int Add(Object value) {
        lock (_root) {
            return _list.Add(value);
        }
    }


    public virtual void Clear() {
        lock (_root) {
            _list.Clear();
        }
    }

    public virtual bool Contains(Object item) {
        lock (_root) {
            return _list.Contains(item);
        }
    }

    public virtual void CopyTo(Array array, int index) {
        lock (_root) {
            _list.CopyTo(array, index);
        }
    }

    public virtual IEnumerator GetEnumerator() {
        lock (_root) {
            return _list.GetEnumerator();
        }
    }

    public virtual int IndexOf(Object value) {
        lock (_root) {
            return _list.IndexOf(value);
        }
    }

    public virtual void Insert(int index, Object value) {
        lock (_root) {
            _list.Insert(index, value);
        }
    }

    public virtual void Remove(Object value) {
        lock (_root) {
            _list.Remove(value);
        }
    }

    public virtual void RemoveAt(int index) {
        lock (_root) {
            _list.RemoveAt(index);
        }
    }
}

[Serializable]
private class FixedSizeList : IList {
    private IList _list;

    internal FixedSizeList(IList l) {
        _list = l;
    }

    public virtual int Count {
        get { return _list.Count; }
    }

    public virtual bool IsReadOnly {
        get { return _list.IsReadOnly; }
    }

    public virtual bool IsFixedSize {
        get { return true; }
    }

    public virtual bool IsSynchronized {
        get { return _list.IsSynchronized; }
    }

    public virtual Object this[int index] {
        get {
            return _list[index];
        }
        set {
            _list[index] = value;
        }
    }

    public virtual Object SyncRoot {
        get { return _list.SyncRoot; }
    }

    public virtual int Add(Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public virtual void Clear() {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public virtual bool Contains(Object obj) {
        return _list.Contains(obj);
    }

    public virtual void CopyTo(Array array, int index) {
        _list.CopyTo(array, index);
    }

    public virtual IEnumerator GetEnumerator() {
        return _list.GetEnumerator();
    }

    public virtual int IndexOf(Object value) {
        return _list.IndexOf(value);
    }

    public virtual void Insert(int index, Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public virtual void Remove(Object value) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public virtual void RemoveAt(int index) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }
}

[Serializable]
private class FixedSizeArrayList : ArrayList {
    private ArrayList _list;

    internal FixedSizeArrayList(ArrayList l) {
        _list = l;
        _version = _list._version;
    }

    public override int Count {
        get { return _list.Count; }
    }

    public override bool IsReadOnly {
        get { return _list.IsReadOnly; }
    }

    public override bool IsFixedSize {
        get { return true; }
    }

    public override bool IsSynchronized {
        get { return _list.IsSynchronized; }
    }

    public override Object this[int index] {
        get {
            return _list[index];
        }
        set {
            _list[index] = value;
            _version = _list._version;
        }
    }

    public override Object SyncRoot {
        get { return _list.SyncRoot; }
    }

    public override int Add(Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override void AddRange(ICollection c) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override int BinarySearch(int index, int count, Object value, IComparer comparer) {
        return _list.BinarySearch(index, count, value, comparer);
    }

    public override int Capacity {
        get { return _list.Capacity; }
        set { throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection")); }
    }

    public override void Clear() {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override Object Clone() {
        FixedSizeArrayList arrayList = new FixedSizeArrayList(_list);
        arrayList._list = (ArrayList)_list.Clone();
        return arrayList;
    }

    public override bool Contains(Object obj) {
        return _list.Contains(obj);
    }

    public override void CopyTo(Array array, int index) {
        _list.CopyTo(array, index);
    }

    public override void CopyTo(int index, Array array, int arrayIndex, int count) {
        _list.CopyTo(index, array, arrayIndex, count);
    }

    public override IEnumerator GetEnumerator() {
        return _list.GetEnumerator();
    }

    public override IEnumerator GetEnumerator(int index, int count) {
        return _list.GetEnumerator(index, count);
    }

    public override int IndexOf(Object value) {
        return _list.IndexOf(value);
    }

    public override int IndexOf(Object value, int startIndex) {
        return _list.IndexOf(value, startIndex);
    }

    public override int IndexOf(Object value, int startIndex, int count) {
        return _list.IndexOf(value, startIndex, count);
    }

    public override void Insert(int index, Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override void InsertRange(int index, ICollection c) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override int LastIndexOf(Object value) {
        return _list.LastIndexOf(value);
    }

    public override int LastIndexOf(Object value, int startIndex) {
        return _list.LastIndexOf(value, startIndex);
    }

    public override int LastIndexOf(Object value, int startIndex, int count) {
        return _list.LastIndexOf(value, startIndex, count);
    }

    public override void Remove(Object value) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override void RemoveAt(int index) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override void RemoveRange(int index, int count) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }

    public override void SetRange(int index, ICollection c) {
        _list.SetRange(index, c);
        _version = _list._version;
    }

    public override ArrayList GetRange(int index, int count) {
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (Count - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        return new Range(this, index, count);
    }

    public override void Reverse(int index, int count) {
        _list.Reverse(index, count);
        _version = _list._version;
    }

    public override void Sort(int index, int count, IComparer comparer) {
        _list.Sort(index, count, comparer);
        _version = _list._version;
    }

    public override Object[] ToArray() {
        return _list.ToArray();
    }

    public override Array ToArray(Type type) {
        return _list.ToArray(type);
    }

    public override void TrimToSize() {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
    }
}

[Serializable]
private class ReadOnlyList : IList {
    private IList _list;

    internal ReadOnlyList(IList l) {
        _list = l;
    }

    public virtual int Count {
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        get { return _list.Count; }
    }

    public virtual bool IsReadOnly {
        get { return true; }
    }

    public virtual bool IsFixedSize {
        get { return true; }
    }

    public virtual bool IsSynchronized {
        get { return _list.IsSynchronized; }
    }

    public virtual Object this[int index] {
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        get {
            return _list[index];
        }
        set {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
        }
    }

    public virtual Object SyncRoot {
        get { return _list.SyncRoot; }
    }

    public virtual int Add(Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public virtual void Clear() {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public virtual bool Contains(Object obj) {
        return _list.Contains(obj);
    }

    public virtual void CopyTo(Array array, int index) {
        _list.CopyTo(array, index);
    }

#if !FEATURE_CORECLR
    [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
    public virtual IEnumerator GetEnumerator() {
        return _list.GetEnumerator();
    }

    public virtual int IndexOf(Object value) {
        return _list.IndexOf(value);
    }

    public virtual void Insert(int index, Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public virtual void Remove(Object value) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public virtual void RemoveAt(int index) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }
}

[Serializable]
private class ReadOnlyArrayList : ArrayList {
    private ArrayList _list;

    internal ReadOnlyArrayList(ArrayList l) {
        _list = l;
    }

    public override int Count {
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        get { return _list.Count; }
    }

    public override bool IsReadOnly {
        get { return true; }
    }

    public override bool IsFixedSize {
        get { return true; }
    }

    public override bool IsSynchronized {
        get { return _list.IsSynchronized; }
    }

    public override Object this[int index] {
#if !FEATURE_CORECLR
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        get {
            return _list[index];
        }
        set {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
        }
    }

    public override Object SyncRoot {
        get { return _list.SyncRoot; }
    }

    public override int Add(Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override void AddRange(ICollection c) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override int BinarySearch(int index, int count, Object value, IComparer comparer) {
        return _list.BinarySearch(index, count, value, comparer);
    }


    public override int Capacity {
        get { return _list.Capacity; }
        set { throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection")); }
    }

    public override void Clear() {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override Object Clone() {
        ReadOnlyArrayList arrayList = new ReadOnlyArrayList(_list);
        arrayList._list = (ArrayList)_list.Clone();
        return arrayList;
    }

    public override bool Contains(Object obj) {
        return _list.Contains(obj);
    }

    public override void CopyTo(Array array, int index) {
        _list.CopyTo(array, index);
    }

    public override void CopyTo(int index, Array array, int arrayIndex, int count) {
        _list.CopyTo(index, array, arrayIndex, count);
    }

    public override IEnumerator GetEnumerator() {
        return _list.GetEnumerator();
    }

    public override IEnumerator GetEnumerator(int index, int count) {
        return _list.GetEnumerator(index, count);
    }

    public override int IndexOf(Object value) {
        return _list.IndexOf(value);
    }

    public override int IndexOf(Object value, int startIndex) {
        return _list.IndexOf(value, startIndex);
    }

    public override int IndexOf(Object value, int startIndex, int count) {
        return _list.IndexOf(value, startIndex, count);
    }

    public override void Insert(int index, Object obj) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override void InsertRange(int index, ICollection c) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override int LastIndexOf(Object value) {
        return _list.LastIndexOf(value);
    }

    public override int LastIndexOf(Object value, int startIndex) {
        return _list.LastIndexOf(value, startIndex);
    }

    public override int LastIndexOf(Object value, int startIndex, int count) {
        return _list.LastIndexOf(value, startIndex, count);
    }

    public override void Remove(Object value) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override void RemoveAt(int index) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override void RemoveRange(int index, int count) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override void SetRange(int index, ICollection c) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override ArrayList GetRange(int index, int count) {
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (Count - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        return new Range(this, index, count);
    }

    public override void Reverse(int index, int count) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override void Sort(int index, int count, IComparer comparer) {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }

    public override Object[] ToArray() {
        return _list.ToArray();
    }

    public override Array ToArray(Type type) {
        return _list.ToArray(type);
    }

    public override void TrimToSize() {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
    }
}


// Implements an enumerator for a ArrayList. The enumerator uses the
// internal version number of the list to ensure that no modifications are 
// made to the list while an enumeration is in progress. 
[Serializable]
private sealed class ArrayListEnumerator : IEnumerator, ICloneable {
    private ArrayList list;
    private int index;
    private int endIndex;       // Where to stop. 
    private int version;
    private Object currentElement;
    private int startIndex;     // Save this for Reset. 

    internal ArrayListEnumerator(ArrayList list, int index, int count) {
        this.list = list;
        startIndex = index;
        this.index = index - 1;
        endIndex = this.index + count;  // last valid index 
        version = list._version;
        currentElement = null;
    }

    public Object Clone() {
        return MemberwiseClone();
    }

    public bool MoveNext() {
        if (version != list._version) throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
        if (index < endIndex) {
            currentElement = list[++index];
            return true;
        } else {
            index = endIndex + 1;
        }

        return false;
    }

    public Object Current {
        get {
            if (index < startIndex)
                throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
            else if (index > endIndex) {
                throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
            }
            return currentElement;
        }
    }

    public void Reset() {
        if (version != list._version) throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
        index = startIndex - 1;
    }
}

// Implementation of a generic list subrange. An instance of this class 
// is returned by the default implementation of List.GetRange.
[Serializable]
private class Range : ArrayList {
    private ArrayList _baseList;
    private int _baseIndex;
    private int _baseSize;
    private int _baseVersion;

    internal Range(ArrayList list, int index, int count) : base(false) {
        _baseList = list;
        _baseIndex = index;
        _baseSize = count;
        _baseVersion = list._version;
        // we also need to update _version field to make Range of Range work 
        _version = list._version;
    }

    private void InternalUpdateRange() {
        if (_baseVersion != _baseList._version)
            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnderlyingArrayListChanged"));
    }

    private void InternalUpdateVersion() {
        _baseVersion++;
        _version++;
    }

    public override int Add(Object value) {
        InternalUpdateRange();
        _baseList.Insert(_baseIndex + _baseSize, value);
        InternalUpdateVersion();
        return _baseSize++;
    }

    public override void AddRange(ICollection c) {
        InternalUpdateRange();
        if (c == null) {
            throw new ArgumentNullException("c");
        }

        int count = c.Count;
        if (count > 0) {
            _baseList.InsertRange(_baseIndex + _baseSize, c);
            InternalUpdateVersion();
            _baseSize += count;
        }
    }

    // Other overloads with automatically work 
    public override int BinarySearch(int index, int count, Object value, IComparer comparer) {
        InternalUpdateRange();
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (_baseSize - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        int i = _baseList.BinarySearch(_baseIndex + index, count, value, comparer);
        if (i >= 0) return i - _baseIndex;
        return i + _baseIndex;
    }

    public override int Capacity {
        get {
            return _baseList.Capacity;
        }

        set {
            if (value < Count) throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
        }
    }


    public override void Clear() {
        InternalUpdateRange();
        if (_baseSize != 0) {
            _baseList.RemoveRange(_baseIndex, _baseSize);
            InternalUpdateVersion();
            _baseSize = 0;
        }
    }

    public override Object Clone() {
        InternalUpdateRange();
        Range arrayList = new Range(_baseList, _baseIndex, _baseSize);
        arrayList._baseList = (ArrayList)_baseList.Clone();
        return arrayList;
    }

    public override bool Contains(Object item) {
        InternalUpdateRange();
        if (item == null) {
            for (int i = 0; i < _baseSize; i++)
                if (_baseList[_baseIndex + i] == null)
                    return true;
            return false;
        } else {
            for (int i = 0; i < _baseSize; i++)
                if (_baseList[_baseIndex + i] != null && _baseList[_baseIndex + i].Equals(item))
                    return true;
            return false;
        }
    }

    public override void CopyTo(Array array, int index) {
        InternalUpdateRange();
        if (array == null)
            throw new ArgumentNullException("array");
        if (array.Rank != 1)
            throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
        if (index < 0)
            throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (array.Length - index < _baseSize)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        _baseList.CopyTo(_baseIndex, array, index, _baseSize);
    }

    public override void CopyTo(int index, Array array, int arrayIndex, int count) {
        InternalUpdateRange();
        if (array == null)
            throw new ArgumentNullException("array");
        if (array.Rank != 1)
            throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (array.Length - arrayIndex < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        if (_baseSize - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        _baseList.CopyTo(_baseIndex + index, array, arrayIndex, count);
    }

    public override int Count {
        get {
            InternalUpdateRange();
            return _baseSize;
        }
    }

    public override bool IsReadOnly {
        get { return _baseList.IsReadOnly; }
    }

    public override bool IsFixedSize {
        get { return _baseList.IsFixedSize; }
    }

    public override bool IsSynchronized {
        get { return _baseList.IsSynchronized; }
    }

    public override IEnumerator GetEnumerator() {
        return GetEnumerator(0, _baseSize);
    }

    public override IEnumerator GetEnumerator(int index, int count) {
        InternalUpdateRange();
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (_baseSize - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        return _baseList.GetEnumerator(_baseIndex + index, count);
    }

    public override ArrayList GetRange(int index, int count) {
        InternalUpdateRange();
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (_baseSize - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        return new Range(this, index, count);
    }

    public override Object SyncRoot {
        get {
            return _baseList.SyncRoot;
        }
    }


    public override int IndexOf(Object value) {
        InternalUpdateRange();
        int i = _baseList.IndexOf(value, _baseIndex, _baseSize);
        if (i >= 0) return i - _baseIndex;
        return -1;
    }

    public override int IndexOf(Object value, int startIndex) {
        InternalUpdateRange();
        if (startIndex < 0)
            throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (startIndex > _baseSize)
            throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));

        int i = _baseList.IndexOf(value, _baseIndex + startIndex, _baseSize - startIndex);
        if (i >= 0) return i - _baseIndex;
        return -1;
    }

    public override int IndexOf(Object value, int startIndex, int count) {
        InternalUpdateRange();
        if (startIndex < 0 || startIndex > _baseSize)
            throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));

        if (count < 0 || (startIndex > _baseSize - count))
            throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));

        int i = _baseList.IndexOf(value, _baseIndex + startIndex, count);
        if (i >= 0) return i - _baseIndex;
        return -1;
    }

    public override void Insert(int index, Object value) {
        InternalUpdateRange();
        if (index < 0 || index > _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
        _baseList.Insert(_baseIndex + index, value);
        InternalUpdateVersion();
        _baseSize++;
    }

    public override void InsertRange(int index, ICollection c) {
        InternalUpdateRange();
        if (index < 0 || index > _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));

        if (c == null) {
            throw new ArgumentNullException("c");

        }
        int count = c.Count;
        if (count > 0) {
            _baseList.InsertRange(_baseIndex + index, c);
            _baseSize += count;
            InternalUpdateVersion();
        }
    }

    public override int LastIndexOf(Object value) {
        InternalUpdateRange();
        int i = _baseList.LastIndexOf(value, _baseIndex + _baseSize - 1, _baseSize);
        if (i >= 0) return i - _baseIndex;
        return -1;
    }

    public override int LastIndexOf(Object value, int startIndex) {
        return LastIndexOf(value, startIndex, startIndex + 1);
    }

    public override int LastIndexOf(Object value, int startIndex, int count) {
        InternalUpdateRange();
        if (_baseSize == 0)
            return -1;

        if (startIndex >= _baseSize)
            throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
        if (startIndex < 0)
            throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));

        int i = _baseList.LastIndexOf(value, _baseIndex + startIndex, count);
        if (i >= 0) return i - _baseIndex;
        return -1;
    }

    // Don't need to override Remove

    public override void RemoveAt(int index) {
        InternalUpdateRange();
        if (index < 0 || index >= _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
        _baseList.RemoveAt(_baseIndex + index);
        InternalUpdateVersion();
        _baseSize--;
    }

    public override void RemoveRange(int index, int count) {
        InternalUpdateRange();
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (_baseSize - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        // No need to call _bastList.RemoveRange if count is 0. 
        // In addition, _baseList won't change the vresion number if count is 0.
        if (count > 0) {
            _baseList.RemoveRange(_baseIndex + index, count);
            InternalUpdateVersion();
            _baseSize -= count;
        }
    }

    public override void Reverse(int index, int count) {
        InternalUpdateRange();
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (_baseSize - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        _baseList.Reverse(_baseIndex + index, count);
        InternalUpdateVersion();
    }


    public override void SetRange(int index, ICollection c) {
        InternalUpdateRange();
        if (index < 0 || index >= _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
        _baseList.SetRange(_baseIndex + index, c);
        if (c.Count > 0) {
            InternalUpdateVersion();
        }
    }

    public override void Sort(int index, int count, IComparer comparer) {
        InternalUpdateRange();
        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        if (_baseSize - index < count)
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
        _baseList.Sort(_baseIndex + index, count, comparer);
        InternalUpdateVersion();
    }

    public override Object this[int index] {
        get {
            InternalUpdateRange();
            if (index < 0 || index >= _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            return _baseList[_baseIndex + index];
        }
        set {
            InternalUpdateRange();
            if (index < 0 || index >= _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            _baseList[_baseIndex + index] = value;
            InternalUpdateVersion();
        }
    }

    public override Object[] ToArray() {
        InternalUpdateRange();
        Object[] array = new Object[_baseSize];
        Array.Copy(_baseList._items, _baseIndex, array, 0, _baseSize);
        return array;
    }

    [SecuritySafeCritical]
    public override Array ToArray(Type type) {
        InternalUpdateRange();
        if (type == null)
            throw new ArgumentNullException("type");
        Array array = Array.UnsafeCreateInstance(type, _baseSize);
        _baseList.CopyTo(_baseIndex, array, 0, _baseSize);
        return array;
    }

    public override void TrimToSize() {
        throw new NotSupportedException(Environment.GetResourceString("NotSupported_RangeCollection"));
    }
}

[Serializable]
private sealed class ArrayListEnumeratorSimple : IEnumerator, ICloneable {
    private ArrayList list;
    private int index;
    private int version;
    private Object currentElement;
    [NonSerialized]
    private bool isArrayList;
    // this object is used to indicate enumeration has not started or has terminated
    static Object dummyObject = new Object();

    internal ArrayListEnumeratorSimple(ArrayList list) {
        this.list = list;
        this.index = -1;
        version = list._version;
        isArrayList = (list.GetType() == typeof(ArrayList));
        currentElement = dummyObject;
    }

    public Object Clone() {
        return MemberwiseClone();
    }

    public bool MoveNext() {
        if (version != list._version) {
            throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
        }

        if (isArrayList) {  // avoid calling virtual methods if we are operating on ArrayList to improve performance 
            if (index < list._size - 1) {
                currentElement = list._items[++index];
                return true;
            } else {
                currentElement = dummyObject;
                index = list._size;
                return false;
            }
        } else {
            if (index < list.Count - 1) {
                currentElement = list[++index];
                return true;
            } else {
                index = list.Count;
                currentElement = dummyObject;
                return false;
            }
        }
    }

    public Object Current {
        get {
            object temp = currentElement;
            if (dummyObject == temp) { // check if enumeration has not started or has terminated 
                if (index == -1) {
                    throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                } else {
                    throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
                }
            }

            return temp;
        }
    }

    public void Reset() {
        if (version != list._version) {
            throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
        }

        currentElement = dummyObject;
        index = -1;
    }
}

internal class ArrayListDebugView {
    private ArrayList arrayList;

    public ArrayListDebugView(ArrayList arrayList) {
        if (arrayList == null)
            throw new ArgumentNullException("arrayList");

        this.arrayList = arrayList;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public Object[] Items {
        get {
            return arrayList.ToArray();
        }
    }
} 
    }
} 
 
// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
                         
 
                        <p></p>
                        <script type = "text/javascript" >
                            SyntaxHighlighter.all()
                        </ script >



                    < div class="col-sm-3" style="border: 1px solid #81919f;border-radius: 10px;">
                        <h3 style = "background-color:#7899a0;margin-bottom: 5%;" > Link Menu</h3>
                        <a href = "http://www.amazon.com/exec/obidos/ASIN/1555583156/httpnetwoprog-20" >
                            < img class="img-responsive" alt="Network programming in C#, Network Programming in VB.NET, Network Programming in .NET" src="http://www.webtropy.com/articles/screenshots/book.jpg" width="192" height="237" border="0"></a><br>
                        <span class="copy">
 
                            This book is available now!<br>
 
                            <a style = "text-decoration: underline; font-family: Verdana,Geneva,Arial,Helvetica,sans-serif; color: Red; font-size: 11px; font-weight: bold;" href="http://www.amazon.com/exec/obidos/ASIN/1555583156/httpnetwoprog-20"> Buy at Amazon US</a> or<br>
   
                            <a style = "text-decoration: underline; font-family: Verdana,Geneva,Arial,Helvetica,sans-serif; color: Red; font-size: 11px; font-weight: bold;" href= "http://www.amazon.co.uk/exec/obidos/ASIN/1555583156/wwwxamlnet-21" > Buy at Amazon UK</a> <br>
                            <br>
 
                            <script type = "text/javascript" >< !--
                                  google_ad_client = "pub-6435000594396515";
/* network.programming-in.net */
google_ad_slot = "3902760999";
                                google_ad_width = 160;
                                google_ad_height = 600;
                                //-->
                            </script>
                            <script type = "text/javascript" src="http://pagead2.googlesyndication.com/pagead/show_ads.js">
                            </script><ins id = "aswift_0_expand" style="display: inline-table; border: medium none; height: 0px; margin: 0px; padding: 0px; position: relative; visibility: visible; width: 160px; background-color: transparent;" data-ad-slot="3902760999"><ins id = "aswift_0_anchor" style="display: block; border: medium none; height: 0px; margin: 0px; padding: 0px; position: relative; visibility: visible; width: 160px; background-color: transparent; overflow: hidden; opacity: 0;"><iframe marginwidth = "0" marginheight="0" vspace="0" hspace="0" allowtransparency="true" scrolling="no" allowfullscreen="true" onload="var i=this.id,s=window.google_iframe_oncopy,H=s&&s.handlers,h=H&&H[i],w=this.contentWindow,d;try{d=w.document}catch(e){}if(h&&d&&(!d.body||!d.body.firstChild)){if(h.call){setTimeout(h,0)}else if(h.match){try{h=s.upd(h,i)}catch(e){}w.location.replace(h)}}" id="aswift_0" name="aswift_0" style="left:0;position:absolute;top:0;border:0px;width:160px;height:600px;" width="160" height="600" frameborder="0"></iframe></ins></ins>
                            <ul>
                                 
                                        <li style = "padding:5px;" >< a href="http://www.dotnetframework.org/default.aspx/Dotnetfx_Vista_SP2/Dotnetfx_Vista_SP2/8@0@50727@4016/DEVDIV/depot/DevDiv/releases/whidbey/NetFxQFE/ndp/fx/src/Configuration/System/Configuration/ProviderBase@cs/1/ProviderBase@cs
">
                                                <span style = "word-wrap: break-word;" > ProviderBase.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Win7_3@5@1/Dotnetfx_Win7_3@5@1/3@5@1/DEVDIV/depot/DevDiv/releases/Orcas/NetFXw7/wpf/src/Base/System/Windows/Interop/ComponentDispatcherThread@cs/1/ComponentDispatcherThread@cs
">
                                                <span style = "word-wrap: break-word;" > ComponentDispatcherThread.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FX-1434/FX-1434/1@0/untmp/whidbey/REDBITS/ndp/fx/src/Net/System/Net/Mail/SmtpFailedRecipientsException@cs/1/SmtpFailedRecipientsException@cs
">
                                                <span style = "word-wrap: break-word;" > SmtpFailedRecipientsException.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/MIT/System/Web/UI/MobileControls/ItemPager@cs/1305376/ItemPager@cs
">
                                                <span style = "word-wrap: break-word;" > ItemPager.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/whidbey/netfxsp/ndp/fx/src/WinForms/Managed/System/WinForms/DataGridRow@cs/2/DataGridRow@cs
">
                                                <span style = "word-wrap: break-word;" > DataGridRow.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/whidbey/netfxsp/ndp/fx/src/WinForms/Managed/System/WinForms/VisualStyles/VisualStyleRenderer@cs/2/VisualStyleRenderer@cs
">
                                                <span style = "word-wrap: break-word;" > VisualStyleRenderer.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/xsp/System/Web/UI/WebParts/PropertyGridEditorPart@cs/1305376/PropertyGridEditorPart@cs
">
                                                <span style = "word-wrap: break-word;" > PropertyGridEditorPart.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/clr/src/BCL/System/Reflection/Emit/ParameterToken@cs/1305376/ParameterToken@cs
">
                                                <span style = "word-wrap: break-word;" > ParameterToken.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/XmlUtils/System/Xml/Xsl/XsltOld/RecordBuilder@cs/1305376/RecordBuilder@cs
">
                                                <span style = "word-wrap: break-word;" > RecordBuilder.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/DataEntity/System/Data/Query/PlanCompiler/ConstraintManager@cs/1305376/ConstraintManager@cs
">
                                                <span style = "word-wrap: break-word;" > ConstraintManager.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/xsp/System/Web/XmlSiteMapProvider@cs/1305376/XmlSiteMapProvider@cs
">
                                                <span style = "word-wrap: break-word;" > XmlSiteMapProvider.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Vista_SP2/Dotnetfx_Vista_SP2/8@0@50727@4016/DEVDIV/depot/DevDiv/releases/Orcas/QFE/ndp/fx/src/DataEntity/System/Data/Query/InternalTrees/columnmapfactory@cs/1/columnmapfactory@cs
">
                                                <span style = "word-wrap: break-word;" > columnmapfactory.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/cdf/src/NetFx40/Tools/System@Activities@Presentation/System/Activities/Presentation/Model/ModelItemExtensions@cs/1407647/ModelItemExtensions@cs
">
                                                <span style = "word-wrap: break-word;" > ModelItemExtensions.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FX-1434/FX-1434/1@0/untmp/whidbey/REDBITS/ndp/fx/src/Net/System/GenericUriParser@cs/2/GenericUriParser@cs
">
                                                <span style = "word-wrap: break-word;" > GenericUriParser.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FX-1434/FX-1434/1@0/untmp/whidbey/REDBITS/ndp/fx/src/xsp/System/Web/UI/WebParts/WebPartMinimizeVerb@cs/1/WebPartMinimizeVerb@cs
">
                                                <span style = "word-wrap: break-word;" > WebPartMinimizeVerb.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/DotNET/DotNET/8@0/untmp/WIN_WINDOWS/lh_tools_devdiv_wpf/Windows/wcp/Framework/System/Windows/Automation/Peers/ContextMenuAutomationPeer@cs/1/ContextMenuAutomationPeer@cs
">
                                                <span style = "word-wrap: break-word;" > ContextMenuAutomationPeer.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/DataEntity/System/Data/Objects/DataClasses/EdmScalarPropertyAttribute@cs/1305376/EdmScalarPropertyAttribute@cs
">
                                                <span style = "word-wrap: break-word;" > EdmScalarPropertyAttribute.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/Xml/System/Xml/schema/XmlSchemaNotation@cs/1305376/XmlSchemaNotation@cs
">
                                                <span style = "word-wrap: break-word;" > XmlSchemaNotation.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/clr/src/BCL/System/Security/Cryptography/ICspAsymmetricAlgorithm@cs/1305376/ICspAsymmetricAlgorithm@cs
">
                                                <span style = "word-wrap: break-word;" > ICspAsymmetricAlgorithm.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Vista_SP2/Dotnetfx_Vista_SP2/8@0@50727@4016/DEVDIV/depot/DevDiv/releases/whidbey/NetFxQFE/ndp/clr/src/BCL/System/Runtime/CompilerServices/CompilationRelaxations@cs/1/CompilationRelaxations@cs
">
                                                <span style = "word-wrap: break-word;" > CompilationRelaxations.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Vista_SP2/Dotnetfx_Vista_SP2/8@0@50727@4016/DEVDIV/depot/DevDiv/releases/Orcas/QFE/ndp/fx/src/DataEntity/System/Data/Query/PlanCompiler/CodeGen@cs/2/CodeGen@cs
">
                                                <span style = "word-wrap: break-word;" > CodeGen.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/Regex/System/Text/RegularExpressions/RegexMatch@cs/1305376/RegexMatch@cs
">
                                                <span style = "word-wrap: break-word;" > RegexMatch.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/wpf/src/Framework/System/Windows/TemplateNameScope@cs/1305600/TemplateNameScope@cs
">
                                                <span style = "word-wrap: break-word;" > TemplateNameScope.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/DataEntityDesign/Design/System/Data/EntityModel/Emitters/SchemaTypeEmitter@cs/1305376/SchemaTypeEmitter@cs
">
                                                <span style = "word-wrap: break-word;" > SchemaTypeEmitter.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/Orcas/SP/wpf/src/UIAutomation/Win32Providers/MS/Internal/AutomationProxies/SafeThemeHandle@cs/1/SafeThemeHandle@cs
">
                                                <span style = "word-wrap: break-word;" > SafeThemeHandle.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/cdf/src/NetFx40/System@ServiceModel@Activities/System/ServiceModel/Activities/Tracking/Configuration/TrackingQueryElement@cs/1305376/TrackingQueryElement@cs
">
                                                <span style = "word-wrap: break-word;" > TrackingQueryElement.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/cdf/src/NetFx40/System@Activities/System/Activities/BookmarkOptionsHelper@cs/1305376/BookmarkOptionsHelper@cs
">
                                                <span style = "word-wrap: break-word;" > BookmarkOptionsHelper.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/Data/System/Data/SqlClient/SqlClientWrapperSmiStream@cs/1305376/SqlClientWrapperSmiStream@cs
">
                                                <span style = "word-wrap: break-word;" > SqlClientWrapperSmiStream.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Vista_SP2/Dotnetfx_Vista_SP2/8@0@50727@4016/DEVDIV/depot/DevDiv/releases/whidbey/NetFxQFE/ndp/fx/src/xsp/System/Web/Util/versioninfo@cs/1/versioninfo@cs
">
                                                <span style = "word-wrap: break-word;" > versioninfo.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/wpf/src/Framework/System/Windows/Documents/TextPointerBase@cs/1305600/TextPointerBase@cs
">
                                                <span style = "word-wrap: break-word;" > TextPointerBase.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/Orcas/SP/wpf/src/Core/CSharp/System/Windows/Media3D/MaterialGroup@cs/1/MaterialGroup@cs
">
                                                <span style = "word-wrap: break-word;" > MaterialGroup.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FXUpdate3074/FXUpdate3074/1@1/untmp/whidbey/QFE/ndp/fx/src/Xml/System/Xml/Dom/XmlCharacterData@cs/2/XmlCharacterData@cs
">
                                                <span style = "word-wrap: break-word;" > XmlCharacterData.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Win7_3@5@1/Dotnetfx_Win7_3@5@1/3@5@1/DEVDIV/depot/DevDiv/releases/whidbey/NetFXspW7/ndp/fx/src/Data/System/Data/SqlClient/TdsParserStateObject@cs/1/TdsParserStateObject@cs
">
                                                <span style = "word-wrap: break-word;" > TdsParserStateObject.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/xsp/System/DynamicData/DynamicData/FilterFactory@cs/1305376/FilterFactory@cs
">
                                                <span style = "word-wrap: break-word;" > FilterFactory.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/whidbey/netfxsp/ndp/fx/src/CompMod/System/ComponentModel/TypeListConverter@cs/1/TypeListConverter@cs
">
                                                <span style = "word-wrap: break-word;" > TypeListConverter.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/clr/src/BCL/System/Security/Policy/FileCodeGroup@cs/1305376/FileCodeGroup@cs
">
                                                <span style = "word-wrap: break-word;" > FileCodeGroup.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Vista_SP2/Dotnetfx_Vista_SP2/8@0@50727@4016/DEVDIV/depot/DevDiv/releases/whidbey/NetFxQFE/ndp/fx/src/Net/System/Net/Mail/ClosableStream@cs/1/ClosableStream@cs
">
                                                <span style = "word-wrap: break-word;" > ClosableStream.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Win7_3@5@1/Dotnetfx_Win7_3@5@1/3@5@1/DEVDIV/depot/DevDiv/releases/whidbey/NetFXspW7/ndp/fx/src/xsp/System/Web/UI/WebControls/ObjectDataSourceSelectingEventArgs@cs/1/ObjectDataSourceSelectingEventArgs@cs
">
                                                <span style = "word-wrap: break-word;" > ObjectDataSourceSelectingEventArgs.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Win7_3@5@1/Dotnetfx_Win7_3@5@1/3@5@1/DEVDIV/depot/DevDiv/releases/Orcas/NetFXw7/wpf/src/Base/MS/Internal/Security/RightsManagement/CallbackHandler@cs/1/CallbackHandler@cs
">
                                                <span style = "word-wrap: break-word;" > CallbackHandler.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Vista_SP2/Dotnetfx_Vista_SP2/8@0@50727@4016/DEVDIV/depot/DevDiv/releases/Orcas/QFE/wpf/src/Core/CSharp/System/Windows/Media/PointHitTestParameters@cs/1/PointHitTestParameters@cs
">
                                                <span style = "word-wrap: break-word;" > PointHitTestParameters.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/cdf/src/NetFx35/System@WorkflowServices/System/Workflow/Activities/Design/ServiceContractListItem@cs/1305376/ServiceContractListItem@cs
">
                                                <span style = "word-wrap: break-word;" > ServiceContractListItem.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FXUpdate3074/FXUpdate3074/1@1/DEVDIV/depot/DevDiv/releases/whidbey/QFE/ndp/fx/src/xsp/System/Web/HttpClientCertificate@cs/2/HttpClientCertificate@cs
">
                                                <span style = "word-wrap: break-word;" > HttpClientCertificate.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/wpf/src/Base/MS/Internal/IO/Zip/ZipIORawDataFileBlock@cs/1305600/ZipIORawDataFileBlock@cs
">
                                                <span style = "word-wrap: break-word;" > ZipIORawDataFileBlock.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/WCF/WCF/3@5@30729@1/untmp/Orcas/SP/ndp/cdf/src/WCF/ServiceModel/System/ServiceModel/Administration/ServiceInfo@cs/1/ServiceInfo@cs
">
                                                <span style = "word-wrap: break-word;" > ServiceInfo.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/whidbey/netfxsp/ndp/clr/src/BCL/System/Security/Cryptography/rsa@cs/1/rsa@cs
">
                                                <span style = "word-wrap: break-word;" > rsa.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/DotNET/DotNET/8@0/untmp/whidbey/REDBITS/ndp/fx/src/Sys/System/Configuration/ConfigXmlAttribute@cs/1/ConfigXmlAttribute@cs
">
                                                <span style = "word-wrap: break-word;" > ConfigXmlAttribute.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/DataWeb/Client/System/Data/Services/Client/Util@cs/1625574/Util@cs
">
                                                <span style = "word-wrap: break-word;" > Util.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FX-1434/FX-1434/1@0/untmp/whidbey/REDBITS/ndp/fx/src/Designer/WebForms/System/Web/UI/Design/ExpressionBindingsDialog@cs/1/ExpressionBindingsDialog@cs
">
                                                <span style = "word-wrap: break-word;" > ExpressionBindingsDialog.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/whidbey/netfxsp/ndp/fx/src/xsp/System/Web/Hosting/HostingEnvironmentException@cs/1/HostingEnvironmentException@cs
">
                                                <span style = "word-wrap: break-word;" > HostingEnvironmentException.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/cdf/src/NetFx40/System@ServiceModel@Channels/System/ServiceModel/Configuration/ByteStreamMessageEncodingElement@cs/1305376/ByteStreamMessageEncodingElement@cs
">
                                                <span style = "word-wrap: break-word;" > ByteStreamMessageEncodingElement.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/clr/src/ManagedLibraries/Remoting/MetaData/SdlChannelSink@cs/1305376/SdlChannelSink@cs
">
                                                <span style = "word-wrap: break-word;" > SdlChannelSink.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/wpf/src/Framework/System/Windows/Shell/JumpItem@cs/1305600/JumpItem@cs
">
                                                <span style = "word-wrap: break-word;" > JumpItem.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FXUpdate3074/FXUpdate3074/1@1/untmp/whidbey/QFE/ndp/fx/src/Xml/System/Xml/Core/XmlValidatingReader@cs/1/XmlValidatingReader@cs
">
                                                <span style = "word-wrap: break-word;" > XmlValidatingReader.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/DotNET/DotNET/8@0/untmp/WIN_WINDOWS/lh_tools_devdiv_wpf/Windows/wcp/Framework/System/Windows/Data/CollectionView@cs/4/CollectionView@cs
">
                                                <span style = "word-wrap: break-word;" > CollectionView.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/WinForms/Managed/System/WinForms/StatusBarDrawItemEvent@cs/1305376/StatusBarDrawItemEvent@cs
">
                                                <span style = "word-wrap: break-word;" > StatusBarDrawItemEvent.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/DotNET/DotNET/8@0/untmp/WIN_WINDOWS/lh_tools_devdiv_wpf/Windows/wcp/Core/MS/Internal/FontCache/CachedTypeface@cs/1/CachedTypeface@cs
">
                                                <span style = "word-wrap: break-word;" > CachedTypeface.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/Data/System/Data/SqlClient/TdsParserSafeHandles@cs/1305376/TdsParserSafeHandles@cs
">
                                                <span style = "word-wrap: break-word;" > TdsParserSafeHandles.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Vista_SP2/Dotnetfx_Vista_SP2/8@0@50727@4016/DEVDIV/depot/DevDiv/releases/Orcas/QFE/ndp/fx/src/DataEntityDesign/Design/System/Data/Entity/Design/MetadataItemCollectionFactory@cs/2/MetadataItemCollectionFactory@cs
">
                                                <span style = "word-wrap: break-word;" > MetadataItemCollectionFactory.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/wpf/src/Core/CSharp/System/Windows/Media/MILUtilities@cs/1305600/MILUtilities@cs
">
                                                <span style = "word-wrap: break-word;" > MILUtilities.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FX-1434/FX-1434/1@0/untmp/whidbey/REDBITS/ndp/clr/src/BCL/System/Int64@cs/1/Int64@cs
">
                                                <span style = "word-wrap: break-word;" > Int64.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FX-1434/FX-1434/1@0/untmp/whidbey/REDBITS/ndp/fx/src/CompMod/System/Collections/Generic/TreeSet@cs/1/TreeSet@cs
">
                                                <span style = "word-wrap: break-word;" > TreeSet.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/Orcas/SP/ndp/fx/src/xsp/System/Web/Extensions/Compilation/WCFModel/SvcMapFileLoader@cs/1/SvcMapFileLoader@cs
">
                                                <span style = "word-wrap: break-word;" > SvcMapFileLoader.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/DotNET/DotNET/8@0/untmp/whidbey/REDBITS/ndp/fx/src/WinForms/Managed/System/WinForms/HandledMouseEvent@cs/1/HandledMouseEvent@cs
">
                                                <span style = "word-wrap: break-word;" > HandledMouseEvent.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FX-1434/FX-1434/1@0/untmp/whidbey/REDBITS/ndp/clr/src/BCL/System/Security/Permissions/HostProtectionPermission@cs/1/HostProtectionPermission@cs
">
                                                <span style = "word-wrap: break-word;" > HostProtectionPermission.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/wpf/src/Framework/System/Windows/Automation/Peers/DataGridAutomationPeer@cs/1305600/DataGridAutomationPeer@cs
">
                                                <span style = "word-wrap: break-word;" > DataGridAutomationPeer.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/DotNET/DotNET/8@0/untmp/WIN_WINDOWS/lh_tools_devdiv_wpf/Windows/wcp/Framework/System/Windows/Controls/GroupItem@cs/1/GroupItem@cs
">
                                                <span style = "word-wrap: break-word;" > GroupItem.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FX-1434/FX-1434/1@0/untmp/whidbey/REDBITS/ndp/fx/src/WinForms/Managed/System/WinForms/DataGridTextBoxColumn@cs/1/DataGridTextBoxColumn@cs
">
                                                <span style = "word-wrap: break-word;" > DataGridTextBoxColumn.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/Orcas/SP/wpf/src/Core/CSharp/MS/Internal/AppModel/CustomCredentialPolicy@cs/2/CustomCredentialPolicy@cs
">
                                                <span style = "word-wrap: break-word;" > CustomCredentialPolicy.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FXUpdate3074/FXUpdate3074/1@1/DEVDIV/depot/DevDiv/releases/whidbey/QFE/ndp/fx/src/xsp/System/Web/UI/WebParts/ConnectionsZone@cs/4/ConnectionsZone@cs
">
                                                <span style = "word-wrap: break-word;" > ConnectionsZone.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FX-1434/FX-1434/1@0/untmp/whidbey/REDBITS/ndp/clr/src/BCL/System/Security/AccessControl/CommonObjectSecurity@cs/1/CommonObjectSecurity@cs
">
                                                <span style = "word-wrap: break-word;" > CommonObjectSecurity.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/Orcas/SP/wpf/src/Core/CSharp/MS/Internal/AppModel/SiteOfOriginContainer@cs/1/SiteOfOriginContainer@cs
">
                                                <span style = "word-wrap: break-word;" > SiteOfOriginContainer.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/wpf/src/Core/CSharp/MS/Internal/Shaping/Positioning@cs/1305600/Positioning@cs
">
                                                <span style = "word-wrap: break-word;" > Positioning.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Win7_3@5@1/Dotnetfx_Win7_3@5@1/3@5@1/DEVDIV/depot/DevDiv/releases/Orcas/NetFXw7/wpf/src/Framework/MS/Internal/Data/ParameterCollection@cs/1/ParameterCollection@cs
">
                                                <span style = "word-wrap: break-word;" > ParameterCollection.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Vista_SP2/Dotnetfx_Vista_SP2/8@0@50727@4016/DEVDIV/depot/DevDiv/releases/Orcas/QFE/wpf/src/Core/CSharp/MS/Internal/Ink/InkSerializedFormat/AlgoModule@cs/1/AlgoModule@cs
">
                                                <span style = "word-wrap: break-word;" > AlgoModule.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/wpf/src/Core/CSharp/System/Windows/Media/textformatting/TextSpan@cs/1305600/TextSpan@cs
">
                                                <span style = "word-wrap: break-word;" > TextSpan.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Win7_3@5@1/Dotnetfx_Win7_3@5@1/3@5@1/DEVDIV/depot/DevDiv/releases/Orcas/NetFXw7/wpf/src/Framework/System/Windows/RequestBringIntoViewEventArgs@cs/1/RequestBringIntoViewEventArgs@cs
">
                                                <span style = "word-wrap: break-word;" > RequestBringIntoViewEventArgs.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FX-1434/FX-1434/1@0/untmp/whidbey/REDBITS/ndp/fx/src/CompMod/System/ComponentModel/HandledEventArgs@cs/1/HandledEventArgs@cs
">
                                                <span style = "word-wrap: break-word;" > HandledEventArgs.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/wpf/src/Framework/System/Windows/Automation/Peers/MediaElementAutomationPeer@cs/1305600/MediaElementAutomationPeer@cs
">
                                                <span style = "word-wrap: break-word;" > MediaElementAutomationPeer.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/xsp/System/Web/UI/WebParts/WebPartConnectionsCancelEventArgs@cs/1305376/WebPartConnectionsCancelEventArgs@cs
">
                                                <span style = "word-wrap: break-word;" > WebPartConnectionsCancelEventArgs.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Vista_SP2/Dotnetfx_Vista_SP2/8@0@50727@4016/DEVDIV/depot/DevDiv/releases/Orcas/QFE/ndp/fx/src/DataEntity/System/Data/Common/FieldMetadata@cs/2/FieldMetadata@cs
">
                                                <span style = "word-wrap: break-word;" > FieldMetadata.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/Orcas/SP/wpf/src/UIAutomation/UIAutomationClient/MS/Internal/Automation/FocusTracker@cs/1/FocusTracker@cs
">
                                                <span style = "word-wrap: break-word;" > FocusTracker.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/DotNET/DotNET/8@0/untmp/whidbey/REDBITS/ndp/fx/src/Net/System/Net/Mail/BufferedReadStream@cs/1/BufferedReadStream@cs
">
                                                <span style = "word-wrap: break-word;" > BufferedReadStream.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/DataEntityDesign/Design/System/Data/EntityModel/Emitters/Emitter@cs/1305376/Emitter@cs
">
                                                <span style = "word-wrap: break-word;" > Emitter.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/DotNET/DotNET/8@0/untmp/whidbey/REDBITS/ndp/fx/src/Designer/System/data/design/TypeConvertions@cs/2/TypeConvertions@cs
">
                                                <span style = "word-wrap: break-word;" > TypeConvertions.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/WinForms/Managed/System/WinForms/PropertyTabChangedEvent@cs/1305376/PropertyTabChangedEvent@cs
">
                                                <span style = "word-wrap: break-word;" > PropertyTabChangedEvent.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Win7_3@5@1/Dotnetfx_Win7_3@5@1/3@5@1/DEVDIV/depot/DevDiv/releases/whidbey/NetFXspW7/ndp/fx/src/Net/System/Net/Configuration/HttpWebRequestElement@cs/1/HttpWebRequestElement@cs
">
                                                <span style = "word-wrap: break-word;" > HttpWebRequestElement.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/WCF/WCF/3@5@30729@1/untmp/Orcas/SP/ndp/cdf/src/NetFx35/System@ServiceModel@Web/System/Runtime/Serialization/Json/JsonReaderDelegator@cs/1/JsonReaderDelegator@cs
">
                                                <span style = "word-wrap: break-word;" > JsonReaderDelegator.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/cdf/src/WCF/infocard/Client/System/IdentityModel/Selectors/InfoCardRSAPKCS1SignatureDeformatter@cs/1305376/InfoCardRSAPKCS1SignatureDeformatter@cs
">
                                                <span style = "word-wrap: break-word;" > InfoCardRSAPKCS1SignatureDeformatter.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/WCF/WCF/3@5@30729@1/untmp/Orcas/SP/ndp/cdf/src/WCF/ServiceModel/System/ServiceModel/ChannelFactory@cs/1/ChannelFactory@cs
">
                                                <span style = "word-wrap: break-word;" > ChannelFactory.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/DotNET/DotNET/8@0/untmp/WIN_WINDOWS/lh_tools_devdiv_wpf/Windows/wcp/Framework/System/Windows/Annotations/LocatorBase@cs/1/LocatorBase@cs
">
                                                <span style = "word-wrap: break-word;" > LocatorBase.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FX-1434/FX-1434/1@0/untmp/whidbey/REDBITS/ndp/fx/src/Net/System/Net/_NestedSingleAsyncResult@cs/1/_NestedSingleAsyncResult@cs
">
                                                <span style = "word-wrap: break-word;" > _NestedSingleAsyncResult.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/Core/Microsoft/Scripting/Actions/SetIndexBinder@cs/1305376/SetIndexBinder@cs
">
                                                <span style = "word-wrap: break-word;" > SetIndexBinder.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/DotNET/DotNET/8@0/untmp/whidbey/REDBITS/ndp/clr/src/BCL/System/IO/FileSystemInfo@cs/1/FileSystemInfo@cs
">
                                                <span style = "word-wrap: break-word;" > FileSystemInfo.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/Orcas/SP/ndp/fx/src/xsp/System/Web/Extensions/Compilation/WCFModel/ReferencedCollectionType@cs/1/ReferencedCollectionType@cs
">
                                                <span style = "word-wrap: break-word;" > ReferencedCollectionType.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FX-1434/FX-1434/1@0/untmp/whidbey/REDBITS/ndp/fx/src/CommonUI/System/Drawing/Advanced/PropertyItem@cs/1/PropertyItem@cs
">
                                                <span style = "word-wrap: break-word;" > PropertyItem.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/Xml/System/Xml/Serialization/SoapIgnoreAttribute@cs/1305376/SoapIgnoreAttribute@cs
">
                                                <span style = "word-wrap: break-word;" > SoapIgnoreAttribute.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/DotNET/DotNET/8@0/untmp/whidbey/REDBITS/ndp/fx/src/CommonUI/System/Drawing/Advanced/MetafileHeaderWmf@cs/1/MetafileHeaderWmf@cs
">
                                                <span style = "word-wrap: break-word;" > MetafileHeaderWmf.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/Dotnetfx_Vista_SP2/Dotnetfx_Vista_SP2/8@0@50727@4016/DEVDIV/depot/DevDiv/releases/whidbey/NetFxQFE/ndp/fx/src/Configuration/System/Configuration/KeyValueConfigurationCollection@cs/1/KeyValueConfigurationCollection@cs
">
                                                <span style = "word-wrap: break-word;" > KeyValueConfigurationCollection.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/FX-1434/FX-1434/1@0/untmp/whidbey/REDBITS/ndp/fx/src/xsp/System/Web/UI/WebParts/WebPartUtil@cs/1/WebPartUtil@cs
">
                                                <span style = "word-wrap: break-word;" > WebPartUtil.cs
</ span >
                                            </ a ></ li >



                                        < li style="padding:5px;"><a href = "http://www.dotnetframework.org/default.aspx/DotNET/DotNET/8@0/untmp/whidbey/REDBITS/ndp/fx/src/xsp/System/Web/UI/WebControls/HyperLinkColumn@cs/1/HyperLinkColumn@cs
">
                                                <span style = "word-wrap: break-word;" > HyperLinkColumn.cs
</ span >
                                            </ a ></ li >



                            </ ul >
                    </ span ></ div >



            < div class="row">
                <div class="col-sm-12" id="footer">
                    <p>Copyright © 2010-2020 <a href = "http://www.infiniteloop.ie" > Infinite Loop Ltd</a> </p>
 
                </div>
 
            </div>
            <script type = "text/javascript" >
                    var gaJsHost = (("https:" == document.location.protocol) ? "https://ssl." : "http://www.");
document.write(unescape("%3Cscript src='" + gaJsHost + "google-analytics.com/ga.js' type='text/javascript'%3E%3C/script%3E"));
            </script><script src = "http://www.google-analytics.com/ga.js" type="text/javascript"></script>
            <script type = "text/javascript" >
                var pageTracker = _gat._getTracker("UA-3658396-9");
                pageTracker._trackPageview();
            </script>
         
     
 
 
 
</count;></endindex;></index+count;></array></object[]></arraylist></ilist></arraylist></count;></arraylist></arraylist></ilist></int></int></int></int></int></int></ienumerator></ienumerator></arraylist></ilist></object></int></int></int></int></int></arraylist></object></int></int></count;></endindex;></index+count;></array></object[]></arraylist></ilist></arraylist></count;></arraylist></arraylist></ilist></int></int></int></int></int></int></ienumerator></ienumerator></arraylist></ilist></object></int></int></int></int></int></arraylist></object></int></int>