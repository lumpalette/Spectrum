using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Espejismo.Core.RichText;

/// <summary>
///   Represents a single laid-out line of text, defined as a slice of <see cref="Glyph"/> instances from a source
///   <see cref="Text"/>.
/// </summary>
public readonly struct LineSpan
{
	private readonly List<Glyph> _glyphs;

	internal LineSpan(
		List<Glyph> glyphs,
		int start,
		int length,
		float width,
		float ascent,
		float descent,
		float leading,
		HorizontalAlignment alignment)
	{
		_glyphs = glyphs;

		Start = start;
		Length = length;

		Width = width;
		Ascent = ascent;
		Descent = descent;
		Leading = leading;

		Alignment = alignment;
	}

	/// <summary>
	///   Gets the index of the first glyph on the line within the source <see cref="Text"/>.
	/// </summary>
	public int Start { get; }

	/// <summary>
	///   Gets the number of glyphs in the line.
	/// </summary>
	public int Length { get; }

	/// <summary>
	///   Gets the total extent of the line, in pixels.
	/// </summary>
	public float Width { get; }

	/// <summary>
	///   Gets the distance from the baseline to the top of the line, in pixels.
	/// </summary>
	public float Ascent { get; }

	/// <summary>
	///   Gets the distance from the baseline to the bottom of the line, in pixels.
	/// </summary>
	public float Descent { get; }

	/// <summary>
	///   Gets the extra vertical added between lines of text, from the bottom of the line.
	/// </summary>
	public float Leading { get; }

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
	public HorizontalAlignment Alignment { get; }

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
	public ref readonly Glyph this[int index]
	{
		get
		{
			ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length, nameof(index));

			return ref CollectionsMarshal.AsSpan(_glyphs)[Start + index];
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

	/// <summary>
	///   Enumerates through the glyphs of a <see cref="LineSpan"/>.
	/// </summary>
	public ref struct Enumerator : IEnumerator<Glyph>
	{
		private readonly List<Glyph> _list;
		private readonly int _start;
		private readonly int _length;

		private int _index = -1;

		internal Enumerator(LineSpan line)
		{
			_list = line._glyphs;
			_start = line.Start;
			_length = line.Length;
		}

		/// <summary>
		///   Gets a reference to the glyph at the current position of the enumerator.
		/// </summary>
		public readonly ref readonly Glyph Current => ref CollectionsMarshal.AsSpan(_list)[_start + _index];

		/// <inheritdoc/>
		public bool MoveNext()
		{
			if (_index < _length)
			{
				_index++;
				return true;
			}

			return false;
		}

		readonly Glyph IEnumerator<Glyph>.Current => Current;

		readonly object IEnumerator.Current => Current;

		void IDisposable.Dispose()
		{
		}

		void IEnumerator.Reset()
		{
			_index = -1;
		}
	}
}
