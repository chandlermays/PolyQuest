using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    [Serializable]
    public class Array2D<T>
    {
        [SerializeField] private T[] m_array;
        [SerializeField, Delayed] private int m_width;
        [SerializeField, Delayed] private int m_height;

        public int Length => m_array?.Length ?? 0;
        public int Width => m_width;
        public int Height => m_height;

        /*---------------------------------------------------------------------------------
        | --- Constructor: Creates a new 2D array with the specified width and height --- |
        ---------------------------------------------------------------------------------*/
        public Array2D(int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be non-negative.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be non-negative.");

            m_width = width;
            m_height = height;
            m_array = new T[width * height];
        }

        /*-------------------------------------------------------------------------------
        | --- Indexer: Gets or sets the element at the specified (x, y) coordinates --- |
        -------------------------------------------------------------------------------*/
        public T this[int x, int y]
        {
            get
            {
                EnsureArrayInitialized();
                ValidateIndices(x, y);
                return m_array[ConvertToIndex(x, y)];
            }
            set
            {
                EnsureArrayInitialized();
                ValidateIndices(x, y);
                m_array[ConvertToIndex(x, y)] = value;
            }
        }

        /*-----------------------------------------------------------------------------------------------
        | --- ConvertToIndex: Converts (x, y) coordinates to a linear index in the underlying array --- |
        -----------------------------------------------------------------------------------------------*/
        internal int ConvertToIndex(int x, int y)
        {
            return y * m_width + x;
        }

        /*--------------------------------------------------------------------------------
        | --- EnsureArrayInitialized: Initializes the underlying array if it is null --- |
        --------------------------------------------------------------------------------*/
        private void EnsureArrayInitialized()
        {
            if (m_array == null)
            {
                if (m_width <= 0 || m_height <= 0)
                    throw new InvalidOperationException("Array2D is not initialized: width and height must be positive.");
                m_array = new T[m_width * m_height];
            }
        }

        /*-------------------------------------------------------------------------------------------
        | --- ValidateIndices: Validates that the provided (x, y) coordinates are within bounds --- |
        -------------------------------------------------------------------------------------------*/
        private void ValidateIndices(int x, int y)
        {
            if ((uint)x >= (uint)m_width)
                throw new ArgumentOutOfRangeException(nameof(x), $"x must be in range [0, {m_width - 1}] but was {x}.");
            if ((uint)y >= (uint)m_height)
                throw new ArgumentOutOfRangeException(nameof(y), $"y must be in range [0, {m_height - 1}] but was {y}.");
        }
    }
}