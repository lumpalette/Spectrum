using Godot;
using Godot.Collections;

namespace Espejismo.Core.RichText;

/// <summary>
///   Represents the visual shape of a text character or icon.
/// </summary>
public readonly struct Glyph // 64 bytes pesa la marranota
{
	/// <summary>
	///   Gets the start index of the glyph within the source string.
	/// </summary>
	public int Start { get; private init; }

	/// <summary>
	///   Gets the end index of the glyph within the source string.
	/// </summary>
	public int End { get; private init; }

	/// <summary>
	///   Gets the number of glyphs in the grapheme cluster, only set in the first glyph.
	/// </summary>
	public byte Count { get; private init; }

	/// <summary>
	///   Gets the number of consecutive times the glyph should be drawn.
	/// </summary>
	public byte Repeat { get; private init; }

	/// <summary>
	///   Gets a value describing the category or characteristics of this glyph.
	/// </summary>
	public ushort Flags { get; private init; } // This is a TextServer.GraphemeFlag value.

	/// <summary>
	///   Gets the offset to the glyph's origin from the baseline.
	/// </summary>
	public Vector2 Offset { get; private init; }

	/// <summary>
	///   Gets the distance to the next glyph along the baseline.
	/// </summary>
	public float Advance { get; private init; }

	/// <summary>
	///   Gets the <see cref="TextServer"/> font resource used for the glyph.
	/// </summary>
	public Rid Font { get; private init; }

	/// <summary>
	///   Gets the size of the <see cref="Font"/>, in pixels.
	/// </summary>
	public ushort FontSize { get; private init; }

	/// <summary>
	///   Gets the index of the glyph in the source <see cref="Font"/>, if applicable.
	/// </summary>
	public int Index { get; private init; }

	/// <summary>
	///   Gets the texture resource associated to the icon.
	/// </summary>
	public Texture2D? IconTexture { get; private init; }

	/// <summary>
	///   Gets the size of the rectangle used for drawing the <see cref="IconTexture"/>.
	/// </summary>
	public Vector2 IconSize { get; private init; }

	/// <summary>
	///   Gets the group of style properties associated to this glyph.
	/// </summary>
	public GlyphStyle Style { get; private init; }

	internal static Glyph CreateChar(Dictionary gl, GlyphStyle style)
	{
		return new Glyph
		{
			Start = (int)gl["start"],
			End   = (int)gl["end"],

			Count  = (byte)gl["count"],
			Repeat = (byte)gl["repeat"],
			Flags  = (ushort)gl["flags"],

			Offset  = (Vector2)gl["offset"],
			Advance = (float)gl["advance"],

			Font     = (Rid)gl["font_rid"],
			FontSize = (ushort)gl["font_size"],
			Index    = (ushort)gl["index"],
			Style    = style,
		};
	}

	internal static Glyph CreateIcon(Dictionary gl, GlyphStyle style, Texture2D tex, Rect2 rect)
	{
		return new Glyph
		{
			Start = (int)gl["start"],
			End   = (int)gl["end"],

			Count  = 1,
			Repeat = (byte)gl["repeat"],
			Flags  = (ushort)(TextServer.GraphemeFlag.Valid | TextServer.GraphemeFlag.EmbeddedObject),

			Offset  = rect.Position,
			Advance = (float)gl["advance"],

			IconTexture = tex,
			IconSize    = rect.Size,
			Style       = style,
		};
	}
}
