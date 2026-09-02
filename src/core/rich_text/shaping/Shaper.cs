using Godot;
using System.Collections.Generic;

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

	// Input.
	public required TextServer TS { get; init; }
	public required ShapeItem[] Items { get; init; }
	public required Dictionary<TextStyle, ResolvedStyle> StyleMap { get; init; }

	// Layout options.
	public required float MaxWidth { get; init; }
	public required HorizontalAlignment BaseAlignment { get; init; }
	public required TextServer.Direction Direction { get; init; }
	public required TextServer.Orientation Orientation { get; init; }

	// Output, the lists must be cleared by caller.
	public required List<Glyph> Glyphs { get; init; }
	public required List<LineLayout> Lines { get; init; }
	public required List<TextMarker> Markers { get; init; }
	public required List<Paragraph> Paragraphs { get; init; }

	// Fallback values.
	public required Font FallbackFont { get; init; }
	public required ushort FallbackFontSize { get; init; }
	public required int FallbackLeading { get; init; }

	public void Shape()
	{
		if (Items.Length == 0)
		{
			InsertEmptyLine(BaseAlignment);
			return;
		}

		WriteParagraphs();

		if (Paragraphs.Count > 0)
		{
			WriteLines();
		}
	}

	private void WriteParagraphs()
	{
		if (Items.Length == 0)
		{
			return;
		}

		var paragraph = new Paragraph(TS, Direction, Orientation) { Alignment = BaseAlignment };
		var independent = true;

		for (var i = 0; i < Items.Length; i++)
		{
			var item = Items[i];

			switch (item.Type)
			{
				case ShapeItemType.Run:
					var resolved = StyleMap[item.Run!.Value.Style];
					var fonts = resolved.Font.GetRids();
					var fontSize = resolved.FontSize;

					TS.ShapedTextAddString(paragraph.Shaped, item.Run!.Value.Text, fonts, fontSize, meta: i);
					break;

				case ShapeItemType.Icon:
					TS.ShapedTextAddObject(paragraph.Shaped, i, item.Icon!.Value.Size, item.Icon!.Value.Alignment);
					break;

				case ShapeItemType.Marker:
					TS.ShapedTextAddObject(paragraph.Shaped, i, Vector2.Zero);
					break;

				case ShapeItemType.Break:
					if (independent)
					{
						Paragraphs.Add(paragraph);
						paragraph = new Paragraph(TS, Direction, Orientation) { Alignment = paragraph.Alignment };
					}

					independent = true;
					break;

				case ShapeItemType.Align:
					var alignment = item.Align!.Value.Alignment ?? BaseAlignment;

					if (paragraph.HasContent)
					{
						Paragraphs.Add(paragraph);
						paragraph = new Paragraph(TS, Direction, Orientation) { Alignment = alignment };
					}
					else
					{
						paragraph = paragraph with { Alignment = alignment };
					}

					independent = false; // stupid
					break;
			}

			if (item.Type is ShapeItemType.Run or ShapeItemType.Icon or ShapeItemType.Marker)
			{
				paragraph = paragraph with { HasContent = true };
				independent = true;
			}
		}

		if (independent)
		{
			Paragraphs.Add(paragraph);
		}
		else
		{
			TS.FreeRid(paragraph.Shaped);
		}
	}

	private void WriteLines()
	{
		foreach (var paragraph in Paragraphs)
		{
			if (!paragraph.HasContent)
			{
				InsertEmptyLine(paragraph.Alignment);
				TS.FreeRid(paragraph.Shaped);
				continue;
			}

			var breaks = CalculateLineBreaks(paragraph.Shaped);

			for (var i = 0; i < breaks.Length; i += 2)
			{
				InsertLine(paragraph, breaks[i], breaks[i + 1] - breaks[i]);
			}

			TS.FreeRid(paragraph.Shaped);
		}
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

	private void InsertLine(Paragraph paragraph, int start, int length)
	{
		var lineShaped = SplitParagraph(paragraph, start, length);
		var initialGlyphCount = Glyphs.Count;
		var maxLeading = float.MinValue;

		foreach (var g in TS.ShapedTextGetGlyphs(lineShaped))
		{
			var leading = ProcessRawGlyph(g, lineShaped);

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
			paragraph.Alignment));

		TS.FreeRid(lineShaped);
	}

	private Rid SplitParagraph(Paragraph paragraph, int start, int length)
	{
		var lineShaped = TS.ShapedTextSubstr(paragraph.Shaped, start, length);

		if (paragraph.Alignment == HorizontalAlignment.Fill && MaxWidth > 0)
		{
			TS.ShapedTextFitToWidth(lineShaped, MaxWidth);
		}

		return lineShaped;
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
				return AppendChar(gl, item);

			case ShapeItemType.Icon:
				return AppendIcon(gl, item, itemIndex, lineShaped);

			default:
				Markers.Add(new TextMarker(item.Marker!.Value.Name, item.Marker!.Value.Attributes, Glyphs.Count));
				return 0f;
		}
	}

	private float AppendChar(Godot.Collections.Dictionary gl, in ShapeItem item)
	{
		var resolved = StyleMap[item.Run!.Value.Style];
		var glyph = Glyph.CreateChar(gl, resolved.Style);

		Glyphs.Add(glyph);

		return resolved.Leading;
	}

	private float AppendIcon(Godot.Collections.Dictionary gl, in ShapeItem item, int itemIndex, Rid shaped)
	{
		var rect = TS.ShapedTextGetObjectRect(shaped, itemIndex);
		rect.Position = new Vector2
		{
			X = (Orientation == TextServer.Orientation.Vertical) ? rect.Position.X : 0f,
			Y = (Orientation == TextServer.Orientation.Vertical) ? 0f : rect.Position.Y
		};

		var resolved = StyleMap[item.Icon!.Value.Style];
		var glyph = Glyph.CreateIcon(gl, resolved.Style, item.Icon!.Value.Texture, rect);

		Glyphs.Add(glyph);

		return resolved.Leading;
	}
}
