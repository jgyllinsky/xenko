﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;

namespace SiliconStudio.Core
{
    /// <summary>
    /// A struct to dispose <see cref="IDisposable"/>, <see cref="IReferencable"/> instances and allocated unmanaged memory.
    /// </summary>
    public struct ObjectCollector : IDisposable
    {
        private List<object> disposables;

        /// <summary>
        /// Gets the number of elements to dispose.
        /// </summary>
        /// <value>The number of elements to dispose.</value>
        public int Count
        {
            get { return disposables.Count; }
        }

        /// <summary>
        /// Disposes all object collected by this class and clear the list. The collector can still be used for collecting.
        /// </summary>
        /// <remarks>
        /// To completely dispose this instance and avoid further dispose, use <see cref="Dispose"/> method instead.
        /// </remarks>
        public void Dispose()
        {
            if (disposables == null)
            {
                return;
            }

            for (int i = disposables.Count - 1; i >= 0; i--)
            {
                var objectToDispose = disposables[i];
                DisposeObject(objectToDispose);
                disposables.RemoveAt(i);
            }
            disposables.Clear();
        }

        public void EnsureValid()
        {
            if (disposables == null)
                disposables = new List<object>();
        }

        /// <summary>
        /// Adds a <see cref="IDisposable"/> object or a <see cref="IntPtr"/> allocated using <see cref="Utilities.AllocateMemory"/> to the list of the objects to dispose.
        /// </summary>
        /// <param name="objectToDispose">To dispose.</param>
        /// <exception cref="ArgumentException">If objectToDispose argument is not IDisposable or a valid memory pointer allocated by <see cref="Utilities.AllocateMemory"/></exception>
        public T Add<T>(T objectToDispose)
        {
            if (!(objectToDispose is IDisposable || objectToDispose is IntPtr || objectToDispose is IReferencable))
                throw new ArgumentException("Argument must be IDisposable, IReferenceable or IntPtr");

            // Check memory alignment
            if (objectToDispose is IntPtr)
            {
                var memoryPtr = (IntPtr)(object)objectToDispose;
                if (!Utilities.IsMemoryAligned(memoryPtr))
                    throw new ArgumentException("Memory pointer is invalid. Memory must have been allocated with Utilties.AllocateMemory");
            }

            EnsureValid();

            if (!disposables.Contains(objectToDispose))
                disposables.Add(objectToDispose);

            return objectToDispose;
        }

        /// <summary>
        /// Dispose a disposable object and set the reference to null. Removes this object from this instance..
        /// </summary>
        /// <param name="objectToDispose">Object to dispose.</param>
        public void RemoveAndDispose<T>(ref T objectToDispose)
        {
            if (disposables != null)
            {
                Remove(objectToDispose);
                DisposeObject(objectToDispose);
                objectToDispose = default(T);
            }
        }

        /// <summary>
        /// Removes a disposable object to the list of the objects to dispose.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToDispose">To dispose.</param>
        public void Remove<T>(T objectToDispose)
        {
            if (disposables != null && disposables.Contains(objectToDispose))
                disposables.Remove(objectToDispose);
        }

        private void DisposeObject(object objectToDispose)
        {
            if (objectToDispose == null)
                return;

            var referenceableObject = objectToDispose as IReferencable;
            if (referenceableObject != null)
            {
                referenceableObject.Release();
                return;
            }

            var disposableObject = objectToDispose as IDisposable;
            if (disposableObject != null)
            {
                disposableObject.Dispose();
            }
            else
            {
                var localData = objectToDispose;
                var dataPointer = (IntPtr)localData;
                Utilities.FreeMemory(dataPointer);
            }            
        }
    }
}