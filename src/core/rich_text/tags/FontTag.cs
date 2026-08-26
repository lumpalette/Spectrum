using Godot;
using System;

namespace Espejismo.Core.RichText.Tags;

/// <summary>
///   Represents a tag that changes the font family of a specific segment of text.
/// </summary>
/// <remarks>
///   <b>Attributes:</b>
///   <list type="bullet">
///     <item>
///       <term><c>[&lt;main&gt;]</c></term>
///       <description>
///         Identifier for the new <see cref="FontFamily"/>, as defined in <see cref="TextConfig"/>.
///       </description>
///     </item>
///     <item>
///       <term><c>[size]</c></term>
///       <description>The size of the font, in pixels. Must be greater than 0.</description>
///     </item>
///   </list>
/// </remarks>
[GlobalClass, Tool]
public sealed partial class FontTag : TextTag
{
	/// <inheritdoc/>
	public override bool IsVoid => false;

	/// <inheritdoc/>
	public override bool Begin(TextBuilder builder, ReadOnlySpan<TagAttribute> attributes)
	{
		var style = builder.TopStyle;

		var font = style.Font;
		var size = style.FontSize;

		if (attributes.TryGetValue("<main>", out ReadOnlySpan<char> id)
			&& TextConfig.Fonts.TryGetResource(id, out var pfont))
		{
			font = pfont;
		}

		if (attributes.TryGetValue("size", out ushort psize))
		{
			size = psize;
		}

		if (style.Font != font || style.FontSize != size)
		{
			builder.PushStyle(style with { Font = font, FontSize = size });
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
