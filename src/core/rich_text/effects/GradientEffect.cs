using Godot;
using System;

namespace Espejismo.Core.RichText.Effects;

/// <summary>
///   A text effect that cycles the text's color through a gradient over time.
/// </summary>
/// <remarks>
///   <b>Parameters:</b>
///   <list type="bullet">
///     <item>
///       <term>Frequency (<c>freq</c>)</term>
///       <description>How many times the gradient repeats across the line, per unit of line progress.</description>
///     </item>
///     <item>
///       <term>Speed (<c>speed</c>)</term>
///       <description>The rate at which the gradient scrolls over time, in cycles/s.</description>
///     </item>
///   </list>
/// </remarks>
[GlobalClass, Tool]
public sealed partial class GradientEffect : TextEffect
{
	[Export]
	private Gradient? _gradient;
	[Export]
	private float _frequency = 0.2f;
	[Export]
	private float _speed = 0.4f;

	/// <inheritdoc/>
	public override bool Process(ref GlyphTransform trans)
	{
		if (_gradient is null)
		{
			return false;
		}

		var pos = (trans.LinePosition * _frequency) + (trans.ElapsedTime * _speed);
		var mod = (float)Mathf.PosMod(pos, 1f);
		
		trans.Color = _gradient.Sample(mod);

		return true;
	}

	/// <inheritdoc/>
	public override TextEffect Setup(ReadOnlySpan<TagAttribute> attributes)
	{
		if (!attributes.TryGetValue("freq", out float freq))
		{
			freq = _frequency;
		}

		if (!attributes.TryGetValue("speed", out float speed))
		{
			speed = _speed;
		}

		if (freq != _frequency || speed != _speed)
		{
			return new GradientEffect
			{
				_gradient = _gradient,
				_frequency = freq,
				_speed = speed
			};
		}

		return this;
	}
}
