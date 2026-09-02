using Godot;

namespace Espejismo.Core.RichText;

/// <summary>
///   Represents a single laid-out line of text, defined as a range of glyphs from a source <see cref="Text"/>.
/// </summary>
/// <remarks>
///   This struct only contains the line's boundaries and metrics; use <see cref="Text.Glyphs"/> along with
///   <see cref="Start"/> and <see cref="Length"/> to access the glyphs in the line.
/// </remarks>
public readonly struct LineLayout
{
	internal LineLayout(
		int start,
		int length,
		float width,
		float ascent,
		float descent,
		float leading,
		HorizontalAlignment alignment)
	{
		Start = start;
		Length = length;

		Width = width;
		Ascent = ascent;
		Descent = descent;
		Leading = leading;

		Alignment = alignment;
	}

	/// <summary>
	///   Gets the index of the first glyph on the line within the source <see cref="Text.Glyphs"/>.
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
}
