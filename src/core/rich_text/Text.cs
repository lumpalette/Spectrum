using Espejismo.Core.RichText.Parsing;
using Espejismo.Core.RichText.Shaping;
using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Espejismo.Core.RichText;

/// <summary>
///   Represents a rich-text string that can be shaped into renderable glyphs.
/// </summary>
public partial class Text
{
	private readonly TextServer _TS = TextServerManager.GetPrimaryInterface();
	private readonly Dictionary<TextStyle, ResolvedStyle> _styleMap = [];
	private readonly List<Glyph> _glyphs = [];
	private readonly List<LineLayout> _lines = [];
	private readonly List<TextMarker> _markers = [];
	private readonly List<Paragraph> _paragraphs = [];

	private readonly ShapeItem[] _items;

	private Font? _fallbackFont;
	private ushort _fallbackFontSize;
	private int _fallbackLeading;

	internal Text(ShapeItem[] items, TextStyle style)
	{
		_items = items;

		if (style == default)
		{
			GenerateStyleMap();
		}
		else
		{
			Style = style;
		}
	}

	/// <summary>
	///   Gets a value indicating whether text's attributes have been changed and needs reshaping.
	/// </summary>
	public bool IsDirty { get; private set; } = true;

	/// <summary>
	///   Gets the shaped <see cref="Glyph"/> instances, in visual order (LTR).
	/// </summary>
	/// <remarks>
	///   A text reshape is triggered when accessing this property and <see cref="IsDirty"/> is <see langword="true"/>.
	/// </remarks>
	public ReadOnlySpan<Glyph> Glyphs
	{
		get
		{
			Shape();
			return CollectionsMarshal.AsSpan(_glyphs);
		}
	}

	/// <summary>
	///   Gets the total number of shaped <see cref="Glyph"/> instances.
	/// </summary>
	/// <remarks>
	///   A text reshape is triggered when accessing this property and <see cref="IsDirty"/> is <see langword="true"/>.
	/// </remarks>
	public int Length
	{
		get
		{
			Shape();
			return _glyphs.Count;
		}
	}

	/// <summary>
	///   Gets the layout of each shaped line, in visual order (top-to-bottom).
	/// </summary>
	/// <remarks>
	/// <para>
	///   You can use this property along with <see cref="Glyphs"/> to access the glyphs from a specific line of text.
	/// </para>
	/// <para>
	///   A text reshape is triggered when accessing this property and <see cref="IsDirty"/> is <see langword="true"/>.
	/// </para>
	/// </remarks>
	public ReadOnlySpan<LineLayout> Lines
	{
		get
		{
			Shape();
			return CollectionsMarshal.AsSpan(_lines);
		}
	}

	/// <summary>
	///   Gets the <see cref="TextMarker"/> instances embedded into the text.
	/// </summary>
	/// <remarks>
	///   A text reshape is triggered when accessing this property and <see cref="IsDirty"/> is <see langword="true"/>.
	/// </remarks>
	public ReadOnlySpan<TextMarker> Markers
	{
		get
		{
			Shape();
			return CollectionsMarshal.AsSpan(_markers);
		}
	}

	/// <summary>
	///   Gets a value indicating whether the text has at least one assigned <see cref="TextEffect"/> instance.
	/// </summary>
	/// <remarks>
	///   The value of this property is recalculated whenever <see cref="Style"/> changes. You can use this property to
	///   determine whether the text needs to be continuously redrawn to avoid any unnecessary redraws for static text.
	/// </remarks>
	public bool HasEffects { get; private set; }

	/// <summary>
	///   Gets or sets the base style applied to all the text.
	/// </summary>
	public TextStyle Style
	{
		get;
		set
		{
			if (field != value)
			{
				field = value;
				Invalidate();
				GenerateStyleMap();
			}
		}
	}

	/// <summary>
	///   Gets or sets the maximum width allowed for a text line, in pixels.
	/// </summary>
	public float Width
	{
		get;
		set
		{
			if (field != value)
			{
				field = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	///   Gets the total size occupied by the shaped content, in pixels.
	/// </summary>
	/// <remarks>
	/// <para>
	///   The <see cref="Vector2.X"/> component is the width of the widest line, and the <see cref="Vector2.Y"/>
	///   component is the sum of the heights of all the lines. The resulting vector is calculated at shape time, so
	///   no recomputation is done per access.
	/// </para>
	/// <para>
	///   For lines using <see cref="HorizontalAlignment.Fill"/>, the line's width reflects the width it was fit to,
	///   rather than the natural width of its content.
	/// </para>
	/// <para>
	///   A text reshape is triggered when accessing this property and <see cref="IsDirty"/> is <see langword="true"/>.
	/// </para>
	/// </remarks>
	public Vector2 ContentSize
	{
		get
		{
			Shape();
			
			var size = Vector2.Zero;
			var lines = Lines;

			if (lines.Length > 0)
			{
				foreach (ref readonly var line in lines)
				{
					if (line.Width > size.X)
					{
						size.X = line.Width;
					}

					size.Y += line.Height;
				}

				size.Y -= lines[^1].Leading;
			}

			return size;
		}
	}

	/// <summary>
	///   Gets or sets the horizontal alignment of the text.
	/// </summary>
	public HorizontalAlignment Alignment
	{
		get;
		set
		{
			if (field != value)
			{
				field = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	///   Parses a rich-text string into a <see cref="Text"/> with the specified <see cref="TextStyle"/> applied.
	/// </summary>
	/// <param name="richText">
	///   The rich-text formatted string to parse.
	/// </param>
	/// <param name="style">
	///   The style to apply to the resulting text.
	/// </param>
	/// <param name="visibleChars">
	///   The maximum number of glyphs to generate. If set to -1, no limit is applied.
	/// </param>
	/// <returns>
	///   The <see cref="Text"/> representation of <paramref name="richText"/>. 
	/// </returns>
	/// <exception cref="ArgumentNullException">
	///   Thrown if <paramref name="richText"/> is <see langword="null"/>.
	/// </exception>
	public static Text Parse(string richText, TextStyle style, int visibleChars = -1)
	{
		ArgumentNullException.ThrowIfNull(richText, nameof(richText));

		if (richText.Length == 0 || visibleChars == 0)
		{
			return new Text([], style);
		}

		var document = new Document(richText);
		var builder = new TextBuilder(visibleChars);

		// Looks cursed somehow, but whatever, it works.
		new Synthesizer(document, builder).Read();

		return builder.Build(style);
	}

	/// <summary>
	///   Shapes the stored shape items into a sequence of <see cref="Glyph"/> instances.
	/// </summary>
	public void Shape()
	{
		if (!IsDirty)
		{
			return;
		}

		IsDirty = false;

		// The shaper doesn't automatically clear the output.
		_glyphs.Clear();
		_lines.Clear();
		_markers.Clear();
		_paragraphs.Clear();

		// Now it looks nicer, cool I guess.
		var shaper = new Shaper
		{
			// Input.
			TS = _TS,
			Items = _items,
			StyleMap = _styleMap,

			// Layout options.
			MaxWidth = Width,
			BaseAlignment = Alignment,
			Direction = TextServer.Direction.Auto,
			Orientation = TextServer.Orientation.Horizontal, // for now, only horizontal scripts are supported.

			// Output.
			Glyphs = _glyphs,
			Lines = _lines,
			Markers = _markers,
			Paragraphs = _paragraphs,

			// Fallback values.
			FallbackFont = _fallbackFont!,
			FallbackFontSize = _fallbackFontSize,
			FallbackLeading = _fallbackLeading
		};

		shaper.Shape();
	}

	private void Invalidate()
	{
		IsDirty = true;
	}

	private void GenerateStyleMap()
	{
		_styleMap.Clear();
		HasEffects = false;

		// The base style is fully resolved here, because an exception is thrown at soon as the config file is loaded
		// the first time if it isn't.
		var baseStyle = TextConfig.DefaultStyle.CreateFrom(Style);

		_fallbackFont = baseStyle.Font!.GetVariant(baseStyle.FontStyle!.Value);
		_fallbackFontSize = baseStyle.FontSize!.Value;
		_fallbackLeading = baseStyle.Spacing!.Value.Y;

		foreach (var item in _items)
		{
			if (item.Type is not (ShapeItemType.Run or ShapeItemType.Icon))
			{
				continue;
			}

			var itemStyle = (item.Type == ShapeItemType.Run) ? item.Run!.Value.Style : item.Icon!.Value.Style;
			
			if (_styleMap.ContainsKey(itemStyle))
			{
				continue;
			}

			// We don't pass the line spacing to the font, as it is stored inside the LineSpan.Leading separately.
			var mergedStyle = itemStyle.MergedWith(baseStyle);
			
			var font = mergedStyle.Font!.GetVariant(mergedStyle.FontStyle!.Value);
			var spacing = mergedStyle.Spacing!.Value;
			
			if (spacing.X != 0)
			{
				font = new FontVariation
				{
					BaseFont = font,
					SpacingGlyph = spacing.X,
				};
			}

			_styleMap[itemStyle] = new ResolvedStyle
			{
				Font = font,
				FontSize = mergedStyle.FontSize!.Value,
				Leading = spacing.Y,
				Style = new GlyphStyle
				{
					Color = mergedStyle.Color!.Value,
					Effect = mergedStyle.Effect,
					ShadowSize = mergedStyle.ShadowSize!.Value,
					ShadowColor = mergedStyle.ShadowColor!.Value,
					ShadowOffset = mergedStyle.ShadowOffset!.Value,
					OutlineSize = mergedStyle.OutlineSize!.Value,
					OutlineColor = mergedStyle.OutlineColor!.Value
				}
			};

			if (mergedStyle.Effect is not null)
			{
				HasEffects = true;
			}
		}
	}
}
