using Godot;

namespace Espejismo.Core.RichText.Shaping;

// A container of shaped text that shares the same alignment.
internal struct Paragraph
{
	public int Start { get; set; }

	public int Length { get; set; }

	public HorizontalAlignment Alignment { get; set; }
}
