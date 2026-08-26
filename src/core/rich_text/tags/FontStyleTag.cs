using Godot;
using System;

namespace Espejismo.Core.RichText;

/// <summary>
///   A text tag that changes the font style of a specific segment of text.
/// </summary>
/// <remarks>
///   <b>Attributes:</b> None.
/// </remarks>
[GlobalClass, Tool]
public sealed partial class FontStyleTag : TextTag
{
	/// <inheritdoc/>
	public override bool IsVoid => false;

	/// <summary>
	///   Gets the style variation applied by the tag, configured through the editor.
	/// </summary>
	[Export]
	public FontStyle FontStyle { get; private set; }
	
	/// <inheritdoc/>
	public override bool Begin(TextBuilder builder, ReadOnlySpan<TagAttribute> attributes)
	{
		var style = builder.TopStyle;

		builder.PushStyle(style with { FontStyle = (style.FontStyle ?? FontStyle.Regular) | FontStyle });
		return true;
	}

	/// <inheritdoc/>
	public override void End(TextBuilder builder)
	{
		builder.PopStyle();
	}
}
