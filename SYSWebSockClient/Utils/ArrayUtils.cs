﻿using System;

namespace SYSWebSockClient
{
    public static class ArrayUtils<T>
    {
        #region Fields

        public static readonly T[] Empty = new T[0];

        #endregion Fields
    }

    public static class ArrayUtils
    {
        #region Methods

        public static void Add<T>(ref T[] array, T newItem)
        {
            if (array == null)
            {
                array = new T[1];
                array[0] = newItem;
                return;
            }

            int count = array.Length;
            T[] newArray = new T[count + 1];
            Array.Copy(array, newArray, count);
            newArray[count] = newItem;
            array = newArray;
        }

        public static void Add<T>(ref T[] array, ref int count, T newItem)
        {
            if (array == null)
            {
                array = new T[1];
                array[0] = newItem;
                count = 1;
                return;
            }

            if (count < array.Length)
            {
                array[count++] = newItem;
                return;
            }

            // have to reallocate
            int newSize = (int)PowerFuncs.GreaterPower2(count + 1);

            Array.Resize(ref array, newSize);
            array[count] = newItem;

            count++;
        }

        public static void Add<T>(ref T[] array, ref int count, ref T newItem)
        {
            if (array == null)
            {
                array = new T[1];
                array[0] = newItem;
                count = 1;
                return;
            }

            if (count < array.Length)
            {
                array[count++] = newItem;
                return;
            }

            // have to reallocate
            int growth;
            if (count < 2)
                growth = 1;
            else
                growth = (int)Math.Log(count) + 1;

            T[] newArray = new T[count + growth];
            Array.Copy(array, newArray, count);
            newArray[count] = newItem;
            array = newArray;

            count++;
        }

        public static void AddRange<StorageType>(ref StorageType[] array, ref int count, StorageType[] newItems, int newItemCount)
        {
            if (array == null)
            {
                Array.Resize(ref array, newItemCount);
                count = newItemCount;
                return;
            }

            if ((count + newItemCount) < array.Length)
            {
                Array.Copy(newItems, 0, array, count, newItemCount);
                count += newItemCount;

                return;
            }

            // have to reallocate
            int allocSize = HigherPower2(count + newItemCount);
            StorageType[] newArray = new StorageType[allocSize];
            Array.Copy(array, newArray, count);
            Array.Copy(newItems, 0, newArray, count, newItemCount);
            count += newItemCount;
            array = newArray;
        }

        public static void AddRange<T>(ref T[] array, ref int count, T[] newItems)
        {
            int newItemCount = newItems.Length;

            if (newItemCount == 0)
                return;

            if (array == null)
            {
                array = new T[newItemCount];
                Array.Copy(newItems, array, newItemCount);
                count = newItemCount;
                return;
            }

            if ((count + newItemCount) < array.Length)
            {
                Array.Copy(newItems, 0, array, count, newItemCount);
                count += newItemCount;

                return;
            }

            // have to reallocate
            T[] newArray = new T[count + newItemCount];
            Array.Copy(array, newArray, count);
            Array.Copy(newItems, 0, newArray, count, newItemCount);
            count += newItemCount;
            array = newArray;
        }

        public static bool Contains<T>(T[] array, int count, T searchItem)
        {
            for (int i = 0; i < count; i++)
            {
                if (Equals(array[i], searchItem))
                    return true;
            }

            return false;
        }

        public static int CreateSlot<T>(ref T[] array, ref int count)
        {
            int entry;

            // assumption for this function is the array passed in is never null
            /*if (array == null)
            {
                array = new T[1];
                count = 1;
                return entry;
            }*/

            if (count < array.Length)
            {
                entry = count;
                count++;
                return entry;
            }

            // have to reallocate
            int growth;
            if (count < 2)
                growth = 1;
            else
                growth = (int)Math.Log(count) + 1;

            T[] newArray = new T[count + growth];
            Array.Copy(array, newArray, count);
            array = newArray;

            entry = count;
            count++;

            return entry;
        }

        public static int HigherPower2(int value)
        {
            return (int)PowerFuncs.GreaterPower2(value);
        }

        public static int Remove<T>(ref T[] array, T removedItem)
        {
            int count = array.Length;

            if (count == 0)
                return -1;

            for (int i = 0; i < count; i++)
            {
                if (!Equals(array[i], removedItem))
                    continue;

                T[] newArray = new T[count - 1];
                Array.Copy(array, 0, newArray, 0, i);
                Array.Copy(array, i + 1, newArray, i, count - i - 1);
                array = newArray;

                return i;
            }

            return -1;
        }

        public static void Remove<T>(ref T[] array, ref int count, T removedItem)
        {
            for (int i = 0; i < count; i++)
            {
                if (!Equals(array[i], removedItem))
                    continue;

                RemoveAt(ref array, ref count, i);

                return;
            }
        }

        public static void RemoveAt<T>(ref T[] array, int index)
        {
            int count = array.Length;

            T[] newArray = new T[count > 1 ? count - 1 : count];
            Array.Copy(array, 0, newArray, 0, index);
            Array.Copy(array, index + 1, newArray, index, count - index - 1);
            array = newArray;
        }

        public static void RemoveAt<T>(ref T[] array, ref int count, int index)
        {
            Array.Copy(array, index + 1, array, index, count - index - 1);
            count--;
        }

        public static void Reserve<StorageType>(ref StorageType[] array, ref int count, int elementCount)
        {
            if (array == null)
            {
                array = new StorageType[elementCount];
                count = 0;
                return;
            }

            if (array.Length < elementCount)
            {
                int allocSize = HigherPower2(elementCount);

                Array.Resize(ref array, allocSize);
            }
        }

        public static void Reserve<StorageType>(ref StorageType[] array, int elementCount)
        {
            if (array == null)
            {
                array = new StorageType[elementCount];
                return;
            }

            if (array.Length < elementCount)
            {
                int allocSize = HigherPower2(elementCount);

                Array.Resize(ref array, allocSize);
            }
        }

        #endregion Methods
    }

    public class FreeListArray<StorageType> : IDisposable
    {
        #region Fields

        public int Count;
        public StorageType[] Storage = ArrayUtils<StorageType>.Empty;

        private int[] FreeList = ArrayUtils<int>.Empty;
        private int FreeListCount;

        #endregion Fields

        #region Methods

        public int Alloc()
        {
            int handleIndex;

            if (FreeListCount > 0)
            {
                FreeListCount--;
                handleIndex = FreeList[FreeListCount];
            }
            else
            {
                handleIndex = Count;
                ArrayUtils.Add(ref Storage, ref Count, default(StorageType));
            }

            return handleIndex;
        }

        public void Free(int handle)
        {
            Storage[handle] = default(StorageType);
            ArrayUtils.Add(ref FreeList, ref FreeListCount, handle);
        }

        #endregion Methods

        #region IDisposable Members

        public void Dispose()
        {
            int count = Count;
            for (int i = 0; i < count; i++)
                Storage[i] = default(StorageType);

            Count = 0;
            FreeListCount = 0;
        }

        #endregion IDisposable Members
    }
}