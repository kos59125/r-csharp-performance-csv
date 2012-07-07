﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using RDotNet.Internals;

namespace RDotNet
{
	/// <summary>
	/// A vector base.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
	public abstract class Vector<T> : SymbolicExpression, IEnumerable<T>
	{
		/// <summary>
		/// Creates a new vector with the specified size.
		/// </summary>
		/// <param name="engine">The <see cref="REngine"/> handling this instance.</param>
		/// <param name="type">The element type.</param>
		/// <param name="length">The length of vector.</param>
		protected Vector(REngine engine, SymbolicExpressionType type, int length)
			: base(engine, engine.GetFunction<Rf_allocVector>("Rf_allocVector")(type, length))
		{
			if (length <= 0)
			{
				throw new ArgumentOutOfRangeException("length");
			}
			var empty = new byte[length * DataSize];
			Marshal.Copy(empty, 0, DataPointer, empty.Length);
		}

		/// <summary>
		/// Creates a new vector with the specified values.
		/// </summary>
		/// <param name="engine">The <see cref="REngine"/> handling this instance.</param>
		/// <param name="type">The element type.</param>
		/// <param name="vector">The elements of vector.</param>
		protected Vector(REngine engine, SymbolicExpressionType type, IEnumerable<T> vector)
			: base(engine, engine.GetFunction<Rf_allocVector>("Rf_allocVector")(type, vector.Count()))
		{
			int index = 0;
			foreach (T element in vector)
			{
				this[index++] = element;
			}
		}

		/// <summary>
		/// Creates a new instance for a vector.
		/// </summary>
		/// <param name="engine">The <see cref="REngine"/> handling this instance.</param>
		/// <param name="coerced">The pointer to a vector.</param>
		protected Vector(REngine engine, IntPtr coerced)
			: base(engine, coerced)
		{}

		/// <summary>
		/// Gets or sets the element at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the element to get or set.</param>
		/// <returns>The element at the specified index.</returns>
		public abstract T this[int index] { get; set; }

		/// <summary>
		/// Gets or sets the element at the specified name.
		/// </summary>
		/// <param name="name">The name of the element to get or set.</param>
		/// <returns>The element at the specified name.</returns>
		public virtual T this[string name]
		{
			get
			{
				if (name == null)
				{
					throw new ArgumentNullException("name");
				}
				string[] names = Names;
				if (names == null)
				{
					throw new InvalidOperationException();
				}
				int index = Array.IndexOf(names, name);
				return this[index];
			}
			set
			{
				string[] names = Names;
				if (names == null)
				{
					throw new InvalidOperationException();
				}
				int index = Array.IndexOf(names, name);
				this[index] = value;
			}
		}

		/// <summary>
		/// Gets the number of elements.
		/// </summary>
		public int Length
		{
			get { return Engine.GetFunction<Rf_length>("Rf_length")(handle); }
		}

		/// <summary>
		/// Gets the names of elements.
		/// </summary>
		public string[] Names
		{
			get
			{
				SymbolicExpression namesSymbol = Engine.GetPredefinedSymbol("R_NamesSymbol");
				SymbolicExpression names = GetAttribute(namesSymbol);
				if (names == null)
				{
					return null;
				}
				CharacterVector namesVector = names.AsCharacter();
				if (namesVector == null)
				{
					return null;
				}

				int length = namesVector.Length;
				var result = new string[length];
				namesVector.CopyTo(result, length);
				return result;
			}
		}

		/// <summary>
		/// Gets the pointer for the first element.
		/// </summary>
		protected IntPtr DataPointer
		{
			get { return IntPtr.Add(handle, Marshal.SizeOf(typeof(VECTOR_SEXPREC))); }
		}

		/// <summary>
		/// Gets the size of an element in byte.
		/// </summary>
		protected abstract int DataSize { get; }

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
		{
			for (int index = 0; index < Length; index++)
			{
				yield return this[index];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		/// <summary>
		/// Copies the elements to the specified array.
		/// </summary>
		/// <param name="destination">The destination array.</param>
		/// <param name="length">The length to copy.</param>
		/// <param name="sourceIndex">The first index of the vector.</param>
		/// <param name="destinationIndex">The first index of the destination array.</param>
		public void CopyTo(T[] destination, int length, int sourceIndex = 0, int destinationIndex = 0)
		{
			if (destination == null)
			{
				throw new ArgumentNullException("destination");
			}
			if (length < 0)
			{
				throw new IndexOutOfRangeException("length");
			}
			if (sourceIndex < 0 || Length < sourceIndex + length)
			{
				throw new IndexOutOfRangeException("sourceIndex");
			}
			if (destinationIndex < 0 || destination.Length < destinationIndex + length)
			{
				throw new IndexOutOfRangeException("destinationIndex");
			}

			while (--length >= 0)
			{
				destination[destinationIndex++] = this[sourceIndex++];
			}
		}

		/// <summary>
		/// Gets the offset for the specified index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns>The offset.</returns>
		protected int GetOffset(int index)
		{
			return DataSize * index;
		}
	}
}
