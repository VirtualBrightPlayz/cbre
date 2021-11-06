using System;
using System.Runtime.InteropServices;

namespace MonoGame.Utilities
{
    internal abstract unsafe class Pointer : IDisposable
    {
        protected static long _allocatedTotal;
        protected static object _lock = new object();

        public abstract long Size { get; }
        public abstract void* Ptr { get; }

        public static long AllocatedTotal
        {
            get { return _allocatedTotal; }
        }

        public abstract void Dispose();

        public static implicit operator void*(Pointer ptr)
        {
            return ptr.Ptr;
        }

        public static implicit operator byte*(Pointer ptr)
        {
            return (byte*) ptr.Ptr;
        }

        public static implicit operator short*(Pointer ptr)
        {
            return (short*) ptr.Ptr;
        }
    }

    internal unsafe class PinnedArray<T> : Pointer where T : struct
    {
        private bool _disposed;
        private long _size;

        private void* _ptr;
        public override void* Ptr
        {
            get { return _ptr; }
        }

        public Span<T> Data => new Span<T>(_ptr, (int)Count);

        public T this[long index]
        {
            get { return Data[(int)index]; }
            set { Data[(int)index] = value; }
        }

        public long Count { get; private set; }

        public override long Size
        {
            get { return _size; }
        }

        public long ElementSize { get; private set; }

        public PinnedArray(long size)
        {
            ElementSize = Marshal.SizeOf(typeof(T));
            Count = size;
            _size = Count * ElementSize;
            _ptr = Marshal.AllocHGlobal((int)_size).ToPointer();
            GC.AddMemoryPressure(_size);

            lock (_lock)
            {
                _allocatedTotal += _size;
            }
        }

        ~PinnedArray()
        {
            Dispose(false);
        }

        public override void Dispose()
        {
            Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            lock (_lock)
            {
                _allocatedTotal -= Size;
            }

            if (_ptr != null)
            {
                Marshal.FreeHGlobal((IntPtr)_ptr);
                GC.RemoveMemoryPressure(_size);
                _ptr = null;
                _size = 0;
            }

            _disposed = true;
        }
    }
}
