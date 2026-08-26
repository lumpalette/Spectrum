using Godot;
using System;

namespace Espejismo.Core.RichText.Tags;

/// <summary>
///   A text tag that changes the outline properties of a specific segment of text.
/// </summary>
/// <remarks>
///   <b>Attributes:</b>
///   <list type="bullet">
///     <item>
///       <term><c>[size]</c></term>
///       <description>The size of the text outline, in pixels.</description>
///     </item>
///     <item>
///       <term><c>[color]</c></term>
///       <description>
///         The color of the text outline, which can be either the name of one of the colors in the <see cref="Colors"/>
///         class, case-insensitive, or a 3, 4, 6 or 8-digit HTML color code, optionally prefixed by a '#' character.
///       </description>
///     </item>
///   </list>
/// </remarks>
[GlobalClass, Tool]
public sealed partial class OutlineTag : TextTag
{
	/// <inheritdoc/>
	public override bool IsVoid => false;

	/// <inheritdoc/>
	public override bool Begin(TextBuilder builder, ReadOnlySpan<TagAttribute> attributes)
	{
		var style = builder.TopStyle;

		var size = style.OutlineSize;
		var color = style.OutlineColor;

		if (attributes.TryGetValue("size", out ushort psize))
		{
			size = psize;
		}

		if (attributes.TryGetValue("color", out Color pcolor))
		{
			color = pcolor;
		}

		if (style.OutlineSize != size || style.OutlineColor != color)
		{
			builder.PushStyle(style with { OutlineSize = size, OutlineColor = color });
			return true;
		}

		return false;
	}

	/// <inheritdoc/>
	public override void End(TextBuilder builder)
	{
		builder.PopStyle();
	}
}
