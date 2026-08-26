using Espejismo.Core.RichText.Effects;
using Godot;
using System;

namespace Espejismo.Core.RichText.Tags;

/// <summary>
///   A text tag that changes the visual effect of a specific segment of text.
/// </summary>
/// <remarks>
///   <b>Attributes:</b> Varies (depends on the specific effect).
/// </remarks>
[GlobalClass, Tool]
public sealed partial class EffectTag : TextTag
{
	/// <inheritdoc/>
	public override bool IsVoid => false;

	/// <summary>
	///   Gets the effect applied by the tag, configured through the editor.
	/// </summary>
	[Export]
	public TextEffect? Effect { get; private set; }

	/// <inheritdoc/>
	public override bool Begin(TextBuilder builder, ReadOnlySpan<TagAttribute> attributes)
	{
		if (Effect is null)
		{
			return false;
		}

		var current = builder.TopStyle.Effect;
		var setup = (attributes.Length > 0) ? Effect.Setup(attributes) : Effect;
		
		builder.PushStyle(builder.TopStyle with { Effect = CompositeEffect.Combine(current, setup) });
		return true;
	}

	/// <inheritdoc/>
	public override void End(TextBuilder builder)
	{
		builder.PopStyle();
	}
}
