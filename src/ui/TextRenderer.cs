using Espejismo.Core.RichText;
using Godot;
using System;

namespace Espejismo.UI;

/// <summary>
///   Represents a control for displaying formatted rich-text strings using HTML-like syntax.
/// </summary>
[GlobalClass, Tool]
public partial class TextRenderer : Control
{
	private readonly TextServer _TS = TextServerManager.GetPrimaryInterface();

	private Text? _shaped;
	private double _elapsedTime;

	/// <summary>
	///   Gets or sets the formatted rich-text string to display on the screen.
	/// </summary>
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

	/// <summary>
	///   Gets or sets the style template to apply to the text.
	/// </summary>
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

	/// <summary>
	///   Gets or sets the horizontal alignment of the text, relative to the control's container.
	/// </summary>
	[ExportGroup("Alignment")]
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
	
	/// <summary>
	///   Gets or sets the vertical alignment of the text, relative to the control's container.
	/// </summary>
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

	/// <summary>
	///   Gets the <see cref="TextMarker"/> instances embedded into the text.
	/// </summary>
	public ReadOnlySpan<TextMarker> Markers
	{
		get
		{
			if (_shaped is null)
			{
				UpdateShaped(parse: true);
			}
			
			return _shaped!.Markers;
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

		GetStartY(out var startY, out var lineGap);

		var canvas = GetCanvasItem();
		var position = new Vector2(0f, startY);
		var lines = _shaped.Lines;

		for (var i = 0; i < lines.Length; i++)
		{
			ref readonly var line = ref lines[i];

			position.X = GetLineOffset(line);
			position.Y += line.Ascent;

			if (i > 0)
			{
				position.Y += lineGap;
			}

			RenderLine(line, canvas, position);

			position.Y += line.Descent + line.Leading;
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

		if (StyleTemplate.Font.Regular is null)
		{
			GD.PushWarning($"{Name}: Regular font from StyleTemplate's font family is missing; the default style will be used.");
			return default;
		}

		return StyleTemplate.Create();
	}

	private void RenderLine(in LineSpan line, Rid canvas, Vector2 position)
	{
		if (line.Alignment == HorizontalAlignment.Center)
		{
			position.X -= line.Width / 2f;
		}
		else if (line.Alignment == HorizontalAlignment.Right)
		{
			position.X -= line.Width;
		}
		
		position.X = MathF.Round(position.X);
		
		for (var i = 0; i < line.Length; i++)
		{
			ref readonly var g = ref line[i];

			if (RenderGlyph(g, canvas, position, i, line.Length) != GlyphVisibility.Omitted)
			{
				position.X += g.Advance * g.Repeat;
			}
		}
	}

	private GlyphVisibility RenderGlyph(in Glyph g, Rid canvas, Vector2 position, int linePosition, int lineLength)
	{
		position += g.Offset;

		var index = g.Index;
		var color = g.Style.Color;
		var sColor = g.Style.ShadowColor;
		var oColor = g.Style.OutlineColor;
		
		if (g.Style.Effect is not null)
		{
			// los efectos de texto enriquecido son clave.
			var trans = new GlyphTransform(g, _elapsedTime, linePosition, lineLength)
			{
				Index = index,
				Color = color,
				ShadowColor = sColor,
				OutlineColor = oColor
			};

			g.Style.Effect.Process(ref trans);

			if (trans.Visibility != GlyphVisibility.Visible)
			{
				return trans.Visibility;
			}

			position += trans.Offset;
			index = trans.Index;
			color = trans.Color;
			sColor = trans.ShadowColor;
			oColor = trans.OutlineColor;
		}

		for (var i = 0; i < g.Repeat; i++)
		{
			// Draw regular character.
			if (g.IsChar)
			{
				if (g.Style.ShadowSize > 0 && sColor.A > 0f)
				{
					_TS.FontDrawGlyph(g.Font, canvas, g.FontSize, position + g.Style.ShadowOffset, index, sColor);
				}

				if (g.Style.OutlineSize > 0 && oColor.A > 0f)
				{
					_TS.FontDrawGlyphOutline(g.Font, canvas, g.FontSize, g.Style.OutlineSize, position, index, oColor);
				}

				_TS.FontDrawGlyph(g.Font, canvas, g.FontSize, position, index, color);
				continue;
			}
			
			// Draw static icon.
			if (g.IconTexture is not null)
			{
				DrawTextureRect(g.IconTexture, new Rect2(position, g.IconSize), tile: false, color);
				continue;
			}
			
			// Draw invalid glyph in a meaningful way.
			_TS.DrawHexCodeBox(canvas, g.FontSize, position, index, color);

			position.X += g.Advance;
		}

		return GlyphVisibility.Visible;
	}

	private void GetStartY(out float startY, out float lineGap)
	{
		startY = 0f;
		lineGap = 0f;

		if (_shaped is null)
		{
			return;
		}

		var contentSize = _shaped.ContentSize;
		var numberOfLines = _shaped.Lines.Length;

		switch (VerticalAlignment)
		{
			case VerticalAlignment.Center:
				startY = (Size.Y - contentSize.Y) / 2f;
				break;
			case VerticalAlignment.Bottom:
				startY = Size.Y - contentSize.Y;
				break;
			case VerticalAlignment.Fill:
				if (numberOfLines > 1)
				{
					lineGap = (Size.Y - contentSize.Y) / (numberOfLines - 1f);
				}
				break;
		}
	}

	private float GetLineOffset(in LineSpan line)
	{
		if (line.Alignment == HorizontalAlignment.Center)
		{
			return Size.X / 2f;
		}

		if (line.Alignment == HorizontalAlignment.Right)
		{
			return Size.X;
		}

		return 0f;
	}

	private void OnStylePropertyChanged()
	{
		UpdateShaped(parse: false);
	}
}
