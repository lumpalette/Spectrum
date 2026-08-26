using Godot;
using System;

namespace Espejismo.Core.RichText.Tags;

/// <summary>
///   A text tag that changes the horizontal alignment of a text paragraph.
/// </summary>
/// <remarks>
///   <b>Attributes:</b> None.
/// </remarks>
[GlobalClass, Tool]
public sealed partial class AlignmentTag : TextTag
{
	/// <inheritdoc/>
	public override bool IsVoid => false;

	/// <summary>
	///   Gets the type of alignment applied by the tag, configured through the editor.
	/// </summary>
	[Export]
	public HorizontalAlignment Alignment { get; private set; }

	/// <inheritdoc/>
	public override bool Begin(TextBuilder builder, ReadOnlySpan<TagAttribute> attributes)
	{
		builder.PushAlignment(Alignment);
		return true;
	}

	/// <inheritdoc/>
	public override void End(TextBuilder builder)
	{
		builder.PopAlignment();
	}
}
