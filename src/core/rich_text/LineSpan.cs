using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Espejismo.Core.RichText;

/// <summary>
///   Represents a single laid-out line of text, defined as a slice of <see cref="Glyph"/> instances from a source
///   <see cref="Text"/>.
/// </summary>
public readonly struct LineSpan : IEnumerable<Glyph>
{
	/// <summary>
	///   Gets the index of the first glyph on the line within the source <see cref="Text"/>.
	/// </summary>
	public int Start { get; internal init; }

	/// <summary>
	///   Gets the number of glyphs in the line.
	/// </summary>
	public int Length { get; internal init; }

	/// <summary>
	///   Gets the total extent of the line, in pixels.
	/// </summary>
	public float Width { get; internal init; }

	/// <summary>
	///   Gets the distance from the baseline to the top of the line, in pixels.
	/// </summary>
	public float Ascent { get; internal init; }

	/// <summary>
	///   Gets the distance from the baseline to the bottom of the line, in pixels.
	/// </summary>
	public float Descent { get; internal init; }

	/// <summary>
	///   Gets the extra vertical added between lines of text, from the bottom of the line.
	/// </summary>
	public float Leading { get; internal init; }

	/// <summary>
	///   Gets the total height of the line, including the line gap, in pixels.
	/// </summary>
	/// <value>
	///   The sum of <see cref="Ascent"/>, <see cref="Descent"/>, and <see cref="Leading"/>.
	/// </value>
	public float Height => Ascent + Descent + Leading;

	/// <summary>
	///   Gets the horizontal alignment applied to the line.
	/// </summary>
	public HorizontalAlignment Alignment { get; internal init; }

	internal List<Glyph> Glyphs { get; init; }

	/// <summary>
	///   Gets the <see cref="Glyph"/> at the specified index.
	/// </summary>
	/// <param name="index">
	///   The zero-based index of the glyph.
	/// </param>
	/// <returns>
	///   The <see cref="Glyph"/> at the specified <paramref name="index"/>.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">
	///   Thrown if <paramref name="index"/> is negative or greater than or equal to <see cref="Length"/>.
	/// </exception>
	public Glyph this[int index]
	{
		get
		{
			ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length, nameof(index));

			return Glyphs[Start + index];
		}
	}

	/// <summary>
	///   Returns an enumerator that iterates through the <see cref="Glyph"/> instances in the line.
	/// </summary>
	/// <returns>
	///   A <see cref="Enumerator"/> for the <see cref="LineSpan"/>.
	/// </returns>
	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator<Glyph> IEnumerable<Glyph>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	/// <summary>
	///   Enumerates through the glyphs of a <see cref="LineSpan"/>.
	/// </summary>
	public struct Enumerator : IEnumerator<Glyph>
	{
		private readonly List<Glyph> _list;
		private readonly int _start;
		private readonly int _length;

		private int _index;
		private Glyph _current;

		internal Enumerator(LineSpan line)
		{
			_list = line.Glyphs;
			_start = line.Start;
			_length = line.Length;
		}

		/// <inheritdoc/>
		public readonly Glyph Current => _current;

		/// <inheritdoc/>
		public readonly void Dispose()
		{
		}

		/// <inheritdoc/>
		public bool MoveNext()
		{
			if (_index < _length)
			{
				_current = _list[_start + _index];
				_index++;
				return true;
			}

			_current = default;
			return false;
		}

		readonly object IEnumerator.Current => Current;

		void IEnumerator.Reset()
		{
			_current = default;
			_index = 0;
		}
	}
}
