using Godot;
using System;

namespace Espejismo.Core.RichText.Tags;

/// <summary>
///   A text tag that changes the entire styling of a specific segment of text.
/// </summary>
/// <remarks>
///   <b>Attributes:</b>
///   <list type="bullet">
///     <item>
///       <term><c>&lt;main&gt;</c></term>
///       <description>
///         Identifier for the <see cref="StyleTemplate"/> to use, as defined in <see cref="TextConfig"/>.
///       </description>
///     </item>
///   </list>
/// </remarks>
[GlobalClass, Tool]
public sealed partial class StyleTag : TextTag
{
	/// <inheritdoc/>
	public override bool IsVoid => false;

	/// <inheritdoc/>
	public override bool Begin(TextBuilder builder, ReadOnlySpan<TagAttribute> attributes)
	{
		if (attributes.TryGetValue("<main>", out ReadOnlySpan<char> id)
			&& TextConfig.Styles.TryGetResource(id, out var template))
		{
			builder.PushStyle(template.Create());
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
