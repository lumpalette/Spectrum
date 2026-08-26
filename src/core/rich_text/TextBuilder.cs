using Espejismo.Core.RichText.Shaping;
using Godot;
using System;
using System.Collections.Generic;

namespace Espejismo.Core.RichText;

/// <summary>
///   Provides a mechanism for sequentially constructing <see cref="Text"/> instances.
/// </summary>
/// <remarks>
/// <para>
///   A rich-text string is composed by five types of items, known as <b>shape items</b>:
///   <list type="bullet">
///     <item>
///       <term>Runs</term>
///       <description>A sequence of characters that shares the same style.</description>
///     </item>
///     <item>
///       <term>Icons</term>
///       <description>A texture embedded directly into the text.</description>
///     </item>
///     <item>
///       <term>Markers</term>
///       <description>Named container of tag attributes, inserted at a specific glyph index.</description>
///     </item>
///     <item>
///       <term>Line breaks</term>
///       <description>Indicates the position of a structural or explicit line break.</description>
///     </item>
///     <item>
///       <term>Alignment</term>
///       <description>The horizontal alignment in which text runs or icons are positioned.</description>
///     </item>
///   </list>
/// </para>
/// <para>
///   To synthesize rich-text, use the methods provided by this class to sequentially append shape items to the output.
///   At the end, use <see cref="Build"/> to generate a <see cref="Text"/> instance based on the final state of the
///   builder.
/// </para>
/// <para>
///   The class also maintains a stack of styles overrides and alignments, which you can modify using the
///   <c>Push/Pop*</c> methods, which allows complex nesting if required.
/// </para>
/// </remarks>
public class TextBuilder
{
	private readonly List<ShapeItem> _items = [];
	private readonly Stack<TextStyle> _styleStack = [];
	private readonly Stack<HorizontalAlignment> _alignmentStack = [];

	/// <summary>
	///   Gets the style override currently at the top of the style stack.
	/// </summary>
	/// <remarks>
	///   Returns a <see langword="default"/> style if the style stack is empty.
	/// </remarks>
	public TextStyle TopStyle
	{
		get
		{
			_styleStack.TryPeek(out var result);
			return result;
		}
	}

	/// <summary>
	///   Gets the alignment currently at the top of the style stack.
	/// </summary>
	/// <remarks>
	///   Returns <see langword="null"/> if the alignment stack is empty, which indicates that the base alignment
	///   should be used instead.
	/// </remarks>
	public HorizontalAlignment? TopAlignment
	{
		get
		{
			if (!_alignmentStack.TryPeek(out var result))
			{
				return null;
			}

			return result;
		}
	}

	/// <summary>
	///   Inserts the specified <see cref="TextStyle"/> override at the top of the style stack.
	/// </summary>
	/// <remarks>
	///   This inserts a new, fresh <see cref="TextStyle"/> in the stack. To merge the properties from the current
	///   active style, use the <see cref="TopStyle"/> property using the <c><see langword="with"/></c> expression
	///   syntax.
	/// </remarks>
	/// <param name="style">
	///   The style to push onto the stack.
	/// </param>
	/// <returns>
	///   The same <see cref="TextBuilder"/> instance.
	/// </returns>
	public TextBuilder PushStyle(TextStyle style)
	{
		_styleStack.Push(style);
		return this;
	}

	/// <summary>
	///   Removes the style override currently at the top of the style stack.
	/// </summary>
	/// <returns>
	///   The same <see cref="TextBuilder"/> instance.
	/// </returns>
	public TextBuilder PopStyle()
	{
		_styleStack.TryPop(out _);
		return this;
	}

	/// <summary>
	///   Appends a run of text using the <see cref="TopStyle"/>.
	/// </summary>
	/// <remarks>
	///   If <paramref name="text"/> is empty, the method call is ignored.
	/// </remarks>
	/// <param name="text">
	///   The text to append.
	/// </param>
	/// <returns>
	///   The same <see cref="TextBuilder"/> instance.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	///   Thrown if <paramref name="text"/> is <see langword="null"/>.
	/// </exception>
	public TextBuilder AppendText(string text)
	{
		ArgumentNullException.ThrowIfNull(text, nameof(text));

		if (text.Length == 0)
		{
			return this;
		}

		_items.Add(ShapeItem.CreateRun(text, TopStyle));
		return this;
	}

	/// <summary>
	///   Appends an icon associated to the specified <see cref="Texture2D"/>.
	/// </summary>
	/// <param name="texture">
	///   The texture associated to the icon.
	/// </param>
	/// <param name="alignment">
	///   The vertical alignment of the icon relative to the surrounding text.
	/// </param>
	/// <param name="size">
	///   The size of the texture rect, in pixels.
	/// </param>
	/// <returns>
	///   The same <see cref="TextBuilder"/> instance.
	/// </returns>
	/// <exception cref="ArgumentException">
	///   Thrown if <paramref name="size"/> is negative.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	///   Thrown if <paramref name="texture"/> is <see langword="null"/>.
	/// </exception>
	public TextBuilder AppendIcon(Texture2D texture, InlineAlignment alignment, Vector2 size)
	{
		ArgumentNullException.ThrowIfNull(texture, nameof(texture));
		
		if (size.X < 0 || size.Y < 0)
		{
			throw new ArgumentException($"Icon size {size} cannot be negative");
		}

		_items.Add(ShapeItem.CreateTexture(texture, alignment, size, TopStyle));
		return this;
	}

	/// <summary>
	///   Appends a marker with to the specified name and attributes.
	/// </summary>
	/// <param name="name">
	///   The name for the marker.
	/// </param>
	/// <param name="attributes">
	///   The attributes associated to the marker.
	/// </param>
	/// <returns>
	///   The same <see cref="TextBuilder"/> instance.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	///   Thrown if <paramref name="name"/> is <see langword="null"/>.
	/// </exception>
	public TextBuilder AppendMarker(string name, ReadOnlySpan<TagAttribute> attributes)
	{
		ArgumentNullException.ThrowIfNull(name, nameof(name));

		_items.Add(ShapeItem.CreateMarker(name, attributes.ToArray()));
		return this;
	}

	/// <summary>
	///   Appends a marker for a hard line break.
	/// </summary>
	/// <returns>
	///   The same <see cref="TextBuilder"/> instance.
	/// </returns>
	public TextBuilder AppendBreak()
	{
		_items.Add(ShapeItem.CreateBreak());
		return this;
	}

	/// <summary>
	///   Inserts the specified <see cref="HorizontalAlignment"/> at the top of the alignment stack, which affects all
	///   subsequent text runs and icons.
	/// </summary>
	/// <param name="alignment">
	///   The alignment to push onto the stack.
	/// </param>
	/// <returns>
	///   The same <see cref="TextBuilder"/> instance.
	/// </returns>
	public TextBuilder PushAlignment(HorizontalAlignment alignment)
	{
		_alignmentStack.Push(alignment);
		_items.Add(ShapeItem.CreateAlign(alignment));
		return this;
	}

	/// <summary>
	///   Removes the alignment currently at the top of the alignment stack, reverting back to the previous alignment.
	/// </summary>
	/// <returns>
	///   The same <see cref="TextBuilder"/> instance.
	/// </returns>
	public TextBuilder PopAlignment()
	{
		if (_alignmentStack.TryPop(out _))
		{
			_items.Add(ShapeItem.CreateAlign(TopAlignment));
		}

		return this;
	}

	/// <summary>
	///   Creates a new <see cref="Text"/> instance based on the state of the builder.
	/// </summary>
	/// <param name="style">
	///   The base style to apply to the text.
	/// </param>
	/// <returns>
	///   The resulting <see cref="Text"/>.
	/// </returns>
	public Text Build(TextStyle style)
	{
		return new Text([.. _items], style);
	}

	/// <summary>
	///   Clears the shape items appended to the builder.
	/// </summary>
	public void Clear()
	{
		_items.Clear();
		_styleStack.Clear();
		_alignmentStack.Clear();
	}
}
