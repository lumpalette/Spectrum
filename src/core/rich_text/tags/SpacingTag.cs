using Godot;
using System;

namespace Espejismo.Core.RichText.Tags;

/// <summary>
///   A text tag that changes the letter or line spacing of a specific segment of text.
/// </summary>
/// <remarks>
///   <b>Attributes:</b>
///   <list type="bullet">
///     <item>
///       <term><c>[&lt;main&gt;]</c></term>
///       <description>
///         The spacing to apply, formatted as <c>X,Y</c>. Takes precedence before the <c>x</c> and <c>y</c>
///         attributes.
///       </description>
///     </item>
///     <item>
///       <term><c>[x]</c></term>
///       <description>
///         Extra space added between letters, in pixels, and can be negative. Only applied when the
///         <c>&lt;main&gt;</c> attribute is not specified.
///       </description>
///     </item>
///     <item>
///       <term><c>[y]</c></term>
///       <description>
///         Extra space added between lines of text, in pixels, and can be negative. Only applied when the
///         <c>&lt;main&gt;</c> attribute is not specified.
///       </description>
///     </item>
///   </list>
/// </remarks>
[GlobalClass, Tool]
public sealed partial class SpacingTag : TextTag
{
	/// <inheritdoc/>
	public override bool IsVoid => false;

	/// <inheritdoc/>
	public override bool Begin(TextBuilder builder, ReadOnlySpan<TagAttribute> attributes)
	{
		var style = builder.TopStyle;
		var spacing = style.Spacing;

		if (attributes.TryGetValue("<main>", sep: ',', out Vector2 pspacing))
		{
			spacing = (Vector2I)pspacing;
		}
		else
		{
			if (attributes.TryGetValue("x", out int x))
			{
				spacing = (spacing ?? Vector2I.Zero) with { X = x };
			}

			if (attributes.TryGetValue("y", out int y))
			{
				spacing = (spacing ?? Vector2I.Zero) with { Y = y };
			}
		}

		if (style.Spacing != spacing)
		{
			builder.PushStyle(style with { Spacing = spacing });
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
