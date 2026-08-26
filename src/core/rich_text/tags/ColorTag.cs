using Godot;
using System;

namespace Espejismo.Core.RichText.Tags;

/// <summary>
///   A text tag that changes the color of a specific segment of text.
/// </summary>
/// <remarks>
///   <b>Attributes:</b>
///   <list type="bullet">
///     <item>
///       <term><c>&lt;main&gt;</c></term>
///       <description>
///         The color to apply, which can be either the name of one of the colors in the <see cref="Colors"/> class,
///         case-insensitive, or a 3, 4, 6 or 8-digit HTML color code, optionally prefixed by a '#' character.
///       </description>
///     </item>
///   </list>
/// </remarks>
[GlobalClass, Tool]
public sealed partial class ColorTag : TextTag
{
	/// <inheritdoc/>
	public override bool IsVoid => false;

	/// <inheritdoc/>
	public override bool Begin(TextBuilder builder, ReadOnlySpan<TagAttribute> attributes)
	{
		if (!attributes.TryGetValue("<main>", out Color value))
		{
			return false;
		}

		builder.PushStyle(builder.TopStyle with { Color = value });
		return true;
	}

	/// <inheritdoc/>
	public override void End(TextBuilder builder)
	{
		builder.PopStyle();
	}
}
