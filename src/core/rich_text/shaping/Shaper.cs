using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Espejismo.Core.RichText.Shaping;

// One-shot shaping engine that reads a sequence of shaped items and produces renderable glyphs.
internal readonly struct Shaper()
{
	/* Nunca suelo escribir acerca de mis experiencias diseñando un sistema cuando programo, y mucho menos hacerlo
	 * dentro del código fuente, pero la sensación que tuve al escribir esto se puede resumir con la frase aquella
	 * mística y poderosa señora, intentando deducir cómo se opera una cámara de teléfono móvil
	 * 
	 * 
	 * 
	 * 
	 * 
	 * no puedo martha
	 */

	private readonly List<Paragraph> _paragraphs = [];

	// Input.
	public required TextServer TS { get; init; }
	public required ShapeItem[] Items { get; init; }
	public required Dictionary<TextStyle, ResolvedStyle> StyleMap { get; init; }

	// Layout options.
	public required float MaxWidth { get; init; }
	public required HorizontalAlignment Alignment { get; init; }
	
	// Output buffers, they must be cleared by caller.
	public required Rid Shaped { get; init; }
	public required List<Glyph> Glyphs { get; init; }
	public required List<LineLayout> Lines { get; init; }
	public required List<TextMarker> Markers { get; init; }

	// Fallback values.
	public required Font FallbackFont { get; init; }
	public required ushort FallbackFontSize { get; init; }
	public required int FallbackLeading { get; init; }

	public void Shape()
	{
		if (Items.Length == 0)
		{
			InsertEmptyLine(Alignment);
			return;
		}

		WriteParagraphs();

		if (_paragraphs.Count > 0)
		{
			WriteLines();
		}
	}

	private static int CountCodepoints(ReadOnlySpan<char> text)
	{
		var count = 0;

		foreach (var _ in text.EnumerateRunes())
		{
			count++;
		}

		return count;
	}

	private void WriteParagraphs()
	{
		var paragraph = new Paragraph { Alignment = Alignment };
		var independent = true;

		for (var i = 0; i < Items.Length; i++)
		{
			var item = Items[i];

			switch (item.Type)
			{
				case ShapeItemType.Run:
					var run = item.Run!.Value;

					var resolved = StyleMap[run.Style];
					var fonts = resolved.Font.GetRids();
					var fontSize = resolved.FontSize;

					TS.ShapedTextAddString(Shaped, run.Text, fonts, fontSize, meta: i);
					paragraph.Length += CountCodepoints(run.Text);
					break;

				case ShapeItemType.Icon:
					TS.ShapedTextAddObject(Shaped, i, item.Icon!.Value.Size, item.Icon!.Value.Alignment);
					paragraph.Length++;
					break;

				case ShapeItemType.Marker:
					TS.ShapedTextAddObject(Shaped, i, Vector2.Zero);
					paragraph.Length++;
					break;

				case ShapeItemType.Break:
					if (independent)
					{
						_paragraphs.Add(paragraph);

						paragraph = new Paragraph
						{
							Start = (int)TS.ShapedTextGetGlyphCount(Shaped),
							Alignment = paragraph.Alignment
						};
					}

					independent = true;
					break;

				case ShapeItemType.Align:
					var itemAlignment = item.Align!.Value.Alignment ?? Alignment;

					if (paragraph.Length > 0)
					{
						_paragraphs.Add(paragraph);

						paragraph = new Paragraph
						{
							Start = (int)TS.ShapedTextGetGlyphCount(Shaped),
							Alignment = itemAlignment
						};
					}
					else
					{
						paragraph.Alignment = itemAlignment;
					}

					independent = false; // stupid
					break;
			}
		}

		if (independent)
		{
			_paragraphs.Add(paragraph);
		}

		/* There is a bug in TextServerAdvance::shaped_text_get_line_breaks that assumes that the passed shaped cannot
		 * be a substr buffer. The method internally calls shaped_text_update_breaks, which is responsible for setting
		 * the grapheme flags used by BREAK_WORD_BOUND. It does it by directly reading the text data from the passed
		 * shaped, but substr buffers does not contain any actual data, but a pointer to the source shaped. Because of
		 * this, those flags are not set, and it makes that shaped_text_get_line_breaks returns incorrect results.
		 * 
		 * By calling this method on the source shaped, we make sure that the grapheme flags are set, making every
		 * subsequent substr have the correct data.
		 */
		TS.ShapedTextGetLineBreaks(Shaped, float.MaxValue, 0, TextServer.LineBreakFlag.WordBound);
	}

	private void WriteLines()
	{
		foreach (var paragraph in _paragraphs)
		{
			if (paragraph.Length == 0)
			{
				InsertEmptyLine(paragraph.Alignment);
				continue;
			}

			var paraShaped = TS.ShapedTextSubstr(Shaped, paragraph.Start, paragraph.Length);
			var breaks = CalculateLineBreaks(paraShaped);

			for (var i = 0; i < breaks.Length; i += 2)
			{
				var lineShaped = TS.ShapedTextSubstr(paraShaped, breaks[i], breaks[i + 1] - breaks[i]);

				if (paragraph.Alignment == HorizontalAlignment.Fill && MaxWidth > 0)
				{
					TS.ShapedTextFitToWidth(lineShaped, MaxWidth);
				}

				InsertLine(lineShaped, paragraph.Alignment);

				TS.FreeRid(lineShaped);
			}

			TS.FreeRid(paraShaped);
		}
	}

	private void InsertLine(Rid lineShaped, HorizontalAlignment alignment)
	{
		var initialGlyphCount = Glyphs.Count;
		var maxLeading = float.MinValue;

		foreach (var gl in TS.ShapedTextGetGlyphs(lineShaped))
		{
			var leading = ProcessRawGlyph(gl, lineShaped);

			if (leading > maxLeading)
			{
				maxLeading = leading;
			}
		}

		Lines.Add(new LineLayout(
			start: initialGlyphCount,
			length: Glyphs.Count - initialGlyphCount,
			width: (float)TS.ShapedTextGetWidth(lineShaped),
			ascent: (float)TS.ShapedTextGetAscent(lineShaped),
			descent: (float)TS.ShapedTextGetDescent(lineShaped),
			maxLeading,
			alignment));
	}

	private void InsertEmptyLine(HorizontalAlignment alignment)
	{
		float ascent, descent, leading;

		if (Lines.Count == 0)
		{
			// No previous line, so we have to make some bullshit metrics by ourselves.
			var font = FallbackFont.GetRids()[0];

			ascent = (float)TS.FontGetAscent(font, FallbackFontSize);
			descent = (float)TS.FontGetDescent(font, FallbackFontSize);
			leading = FallbackLeading;
		}
		else
		{
			// Just copy the previous line bro it doesn't matter.
			var previousLine = Lines[^1];

			ascent = previousLine.Ascent;
			descent = previousLine.Descent;
			leading = previousLine.Leading;
		}

		Lines.Add(new LineLayout(
			start: Glyphs.Count,
			length: 0,
			width: 0f,
			ascent,
			descent,
			leading,
			alignment));
	}

	// Returns the leading associated to the specified glyph.
	private float ProcessRawGlyph(Godot.Collections.Dictionary gl, Rid lineShaped)
	{
		var flags = (TextServer.GraphemeFlag)(long)gl["flags"];
		var spanIndex = (int)gl["span_index"];

		var itemIndex = (int)(flags.HasFlag(TextServer.GraphemeFlag.EmbeddedObject)
			? TS.ShapedGetSpanEmbeddedObject(lineShaped, spanIndex)
			: TS.ShapedGetSpanMeta(lineShaped, spanIndex));

		var item = Items[itemIndex];

		switch (item.Type)
		{
			case ShapeItemType.Run:
				return AppendChar(gl, item.Run!.Value);
			case ShapeItemType.Icon:
				return AppendIcon(gl, item.Icon!.Value, itemIndex, lineShaped);
			default:
				return AppendMarker(item.Marker!.Value);
		}
	}

	private float AppendChar(Godot.Collections.Dictionary gl, in ItemRun item)
	{
		var resolved = StyleMap[item.Style];
		var glyph = Glyph.CreateChar(gl, resolved.Style);

		Glyphs.Add(glyph);

		return resolved.Leading;
	}

	private float AppendIcon(Godot.Collections.Dictionary gl, in ItemIcon item, int itemIndex, Rid shaped)
	{
		var ori = TS.ShapedTextGetOrientation(Shaped);
		var rect = TS.ShapedTextGetObjectRect(shaped, itemIndex);
		
		rect.Position = new Vector2
		{
			X = (ori == TextServer.Orientation.Vertical) ? rect.Position.X : 0f,
			Y = (ori == TextServer.Orientation.Vertical) ? 0f : rect.Position.Y
		};

		var resolved = StyleMap[item.Style];
		var glyph = Glyph.CreateIcon(gl, resolved.Style, item.Texture, rect);

		Glyphs.Add(glyph);

		return resolved.Leading;
	}

	private float AppendMarker(in ItemMarker marker)
	{
		Markers.Add(new TextMarker(marker.Name, marker.Attributes, Glyphs.Count));
		return 0f;
	}

	private int[] CalculateLineBreaks(Rid shaped)
	{
		var width = (MaxWidth > 0) ? MaxWidth : float.MaxValue;

		var breakFlags = TextServer.LineBreakFlag.WordBound
			| TextServer.LineBreakFlag.Adaptive
			| TextServer.LineBreakFlag.TrimStartEdgeSpaces
			| TextServer.LineBreakFlag.TrimEndEdgeSpaces;

		return TS.ShapedTextGetLineBreaks(shaped, width, start: 0, breakFlags);
	}
}
