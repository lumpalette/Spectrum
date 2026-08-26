using Godot;
using System;

namespace Espejismo.Core.RichText.Tags;

/// <summary>
///   A text tag that changes the shadow properties of a specific segment of text.
/// </summary>
/// <remarks>
///   <b>Attributes:</b>
///   <list type="bullet">
///     <item>
///       <term><c>[size]</c></term>
///       <description>The size of the shadow effect, in pixels.</description>
///     </item>
///     <item>
///       <term><c>[color]</c></term>
///       <description>
///         The color of the shadow effect, which can be either the name of one of the colors in the <see cref="Colors"/>
///         class, case-insensitive, or a 3, 4, 6 or 8-digit HTML color code, optionally prefixed by a '#' character.
///       </description>
///     </item>
///     <item>
///       <term><c>[offset]</c></term>
///       <description>
///         The displacement of the shadow effect relative to the text's position, formatted as <c>X,Y</c>.
///       </description>
///     </item>
///   </list>
/// </remarks>
[GlobalClass, Tool]
public sealed partial class ShadowTag : TextTag
{
	/// <inheritdoc/>
	public override bool IsVoid => false;

	/// <inheritdoc/>
	public override bool Begin(TextBuilder builder, ReadOnlySpan<TagAttribute> attributes)
	{
		var style = builder.TopStyle;
		
		var size = style.ShadowSize;
		var color = style.ShadowColor;
		var offset = style.ShadowOffset;
		
		if (attributes.TryGetValue("size", out ushort psize))
		{
			size = psize;
		}

		if (attributes.TryGetValue("color", out Color pcolor))
		{
			color = pcolor;
		}

		if (attributes.TryGetValue("offset", sep: ',', out Vector2 poffset))
		{
			offset = poffset;
		}

		if (style.ShadowSize != size || style.ShadowColor != color || style.ShadowOffset != offset)
		{
			builder.PushStyle(builder.TopStyle with { ShadowSize = size, ShadowColor = color, ShadowOffset = offset });
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
