using Godot;

namespace Espejismo.Core.RichText.Shaping;

// A container of shaped text that shares the same alignment.
internal readonly struct Paragraph
{
	public int Start { get; init; }

	public int Length { get; init; }

	public HorizontalAlignment Alignment { get; init; }

	// Whether the shaped buffer has any content (runs, icons or markers).
	public bool HasContent { get; init; }
}
