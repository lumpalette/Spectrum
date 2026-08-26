using Espejismo.Core.RichText;
using Godot;
using System;

namespace Espejismo.UI;

[GlobalClass, Tool]
public partial class TextRenderer : Control
{
	private Text? _shaped;
	private double _elapsedTime;

	[Export(PropertyHint.MultilineText)]
	public string Text
	{
		get;
		set
		{
			ArgumentNullException.ThrowIfNull(value, nameof(value));

			if (field != value)
			{
				field = value;
				UpdateShaped(parse: true);
			}
		}
	} = string.Empty;

	[Export]
	public StyleTemplate? StyleTemplate
	{
		get;
		set
		{
			if (field != value)
			{
				field?.Changed -= OnStylePropertyChanged;
				field = value;
				field?.Changed += OnStylePropertyChanged;

				UpdateShaped(parse: false);
			}
		}
	}

	[Export]
	public HorizontalAlignment HorizontalAlignment
	{
		get;
		set
		{
			if (field != value)
			{
				field = value;
				UpdateShaped(parse: false);
			}
		}
	}

	[Export]
	public VerticalAlignment VerticalAlignment
	{
		get;
		set
		{
			if (field != value)
			{
				// Vertical alignment doesn't exist in that layer.
				field = value;
				QueueRedraw();
			}
		}
	}

	/// <inheritdoc/>
	public override void _Ready()
	{
		if (_shaped is null)
		{
			UpdateShaped(parse: true);
		}
	}

	/// <inheritdoc/>
	public override void _Process(double delta)
	{
		_elapsedTime += delta;

		if (_shaped?.HasEffects == true)
		{
			QueueRedraw();
		}
	}

	/// <inheritdoc/>
	public override void _Draw()
	{
		if (_shaped is null)
		{
			return;
		}
	}

	/// <inheritdoc/>
	public override void _Notification(int what)
	{
		if (what == NotificationResized)
		{
			UpdateShaped(parse: false);
		}
	}

	private TextStyle GetStyle()
	{
		if (StyleTemplate is null)
		{
			return default;
		}

		if (StyleTemplate.Font is null)
		{
			GD.PushWarning($"{Name}: StyleTemplate's font is missing; the default style will be used.");
			return default;
		}

		return StyleTemplate.Create();
	}

	private void UpdateShaped(bool parse)
	{
		var style = GetStyle();

		if (parse || _shaped is null)
		{
			_shaped = Core.RichText.Text.Parse(Text, style);
		}

		_shaped.Style = style;
		_shaped.Width = Size.X;
		_shaped.Alignment = HorizontalAlignment;

		QueueRedraw();
	}

	private void OnStylePropertyChanged()
	{
		UpdateShaped(parse: false);
	}

	private void GetVerticalMetrics(out float startY, out float lineGap)
	{
		startY = 0f;
		lineGap = 0f;

		var contentSize = _shaped.ContentSize;
	}
}
