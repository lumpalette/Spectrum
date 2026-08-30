using Godot;
using System;

namespace Espejismo.Core.RichText.Effects;

/// <summary>
///   A text effect that moves the text around a circle.
/// </summary>
/// <remarks>
///   <b>Parameters:</b>
///   <list type="bullet">
///     <item>
///       <term>Radius (<c>radius</c>)</term>
///       <description>The distance the text is displaced from its origin, in pixels.</description>
///     </item>
///     <item>
///       <term>Frequency (<c>freq</c>)</term>
///       <description>The angular velocity, in radians/s.</description>
///     </item>
///     <item>
///       <term>Spacing (<c>spacing</c>)</term>
///       <description>Angular offset between one glyph and the next, in radians.</description>
///     </item>
///   </list>
/// </remarks>
[GlobalClass, Tool]
public sealed partial class TornadoEffect : TextEffect
{
	[Export]
	private float _radius = 3.5f;
	[Export]
	private float _frequency = 4f;
	[Export]
	private float _spacing = 0.4f;

	/// <inheritdoc/>
	public override bool Process(ref GlyphTransform trans)
	{
		var angle = (trans.ElapsedTime * _frequency) + (trans.LinePosition * _spacing);

		var x = (float)Math.Sin(angle) * _radius;
		var y = (float)Math.Cos(angle) * _radius;

		trans.Offset += new Vector2(x, y);

		return true;
	}

	/// <inheritdoc/>
	public override TextEffect Setup(ReadOnlySpan<TagAttribute> attributes)
	{
		if (!attributes.TryGetValue("radius", out float radius))
		{
			radius = _radius;
		}

		if (!attributes.TryGetValue("freq", out float freq))
		{
			freq = _frequency;
		}

		if (!attributes.TryGetValue("spacing", out float spacing))
		{
			spacing = _spacing;
		}

		if (radius != _radius || freq != _frequency || spacing != _spacing)
		{
			return new TornadoEffect
			{
				_radius = radius,
				_frequency = freq,
				_spacing = spacing
			};
		}

		return this;
	}
}
