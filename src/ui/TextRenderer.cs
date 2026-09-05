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
			if (field == value)
			{
				return;
			}

			field?.Changed -= OnStylePropertyChanged;
			field = value;
			field?.Changed += OnStylePropertyChanged;

			UpdateShaped(parse: false);
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
	///   Gets or sets the number of characters to render. If set to -1, all characters are rendered.
	/// </summary>
	/// <value>
	///   A 32-bit signed integer in the range [-1,128000].
	/// </value>
	[ExportGroup("Displayed text")]
	[Export(PropertyHint.Range, "-1,128000,suffix:chrs")]
	public int VisibleCharacters
	{
		get;
		set
		{
			if (value < -1)
			{
				value = -1;
			}

			// Apparently, 128,000 is the maximum number of chars allowed in one shaped text.
			if (value > 128_000)
			{
				value = 128_000;
			}

			if (field == value)
			{
				return;
			}

			field = value;

			if (TrimBeforeShaping)
			{
				UpdateShaped(parse: true);
			}
			else
			{
				QueueRedraw();
			}
		}
	} = -1;

	/// <summary>
	///   Gets a value indicating whether trimmed characters from the <see cref="VisibleCharacters"/> cutoff should be
	///   excluded or included in the shaping process.
	/// </summary>
	/// <value>
	///   <see langword="true"/> if trimmed characters should be excluded from shaping; <see langword="false"/> if they
	///   should be included.
	/// </value>
	[Export]
	public bool TrimBeforeShaping
	{
		get;
		set
		{
			if (field == value)
			{
				return;
			}

			field = value;

			if (VisibleCharacters != -1)
			{
				UpdateShaped(parse: true);
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
		if (_shaped is null or { Length: 0 })
		{
			return;
		}

		var canvas = GetCanvasItem();
		var position = new Vector2 { Y = GetVerticalOffset(out var lineGap) };

		var clusterVisible = true;

		foreach (ref readonly var line in _shaped.Lines)
		{
			// TextServer.font_draw_glyph() starts drawing from the baseline, so we have to account for that.
			position.Y += line.Ascent;
			position.X = GetLineOffset(line);

			for (var i = 0; i < line.Length; i++)
			{
				ref readonly var g = ref _shaped.Glyphs[line.Start + i];

				// Glyph visibility is only determined at the start of its cluster.
				if (g.Count > 0)
				{
					clusterVisible = VisibleCharacters == -1 || g.Start < VisibleCharacters;
				}

				if (clusterVisible)
				{
					var visibility = RenderGlyph(g, canvas, position, i, line.Length);

					if (visibility == GlyphVisibility.Omitted)
					{
						continue;
					}
				}

				position.X += g.Advance * g.Repeat;
			}

			position.Y += line.Descent + line.Leading + lineGap;
		}
	}

	/// <inheritdoc/>
	public override void _Notification(int what)
	{
		if (what == NotificationResized)
		{
			UpdateShaped(parse: false);
		}
		else if (what == NotificationPredelete)
		{
			_shaped?.Dispose();
			_shaped = null;
		}

		base._Notification(what);
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

	private void UpdateShaped(bool parse)
	{
		var style = GetStyle();

		if (parse || _shaped is null)
		{
			_shaped?.Dispose();
			_shaped = Core.RichText.Text.Parse(Text, style, TrimBeforeShaping ? VisibleCharacters : -1);
		}

		_shaped.Style = style;
		_shaped.Width = Size.X;
		_shaped.Alignment = HorizontalAlignment;
		
		QueueRedraw();
	}

	private float GetVerticalOffset(out float lineGap)
	{
		lineGap = 0f;

		var contentHeight = _shaped!.ContentSize.Y;
		var numberOfLines = _shaped!.Lines.Length;

		switch (VerticalAlignment)
		{
			case VerticalAlignment.Center:
				return (Size.Y - contentHeight) / 2f;
			case VerticalAlignment.Bottom:
				return Size.Y - contentHeight;
			case VerticalAlignment.Fill:
				if (numberOfLines > 0)
				{
					lineGap = Math.Max(0f, Size.Y - contentHeight) / (numberOfLines - 1);
				}
				return 0f;
		}

		return 0f;
	}

	private float GetLineOffset(in LineLayout line)
	{
		if (line.Alignment == HorizontalAlignment.Center)
		{
			return (Size.X - line.Width) / 2f;
		}
 
		if (line.Alignment == HorizontalAlignment.Right)
		{
			return Size.X - line.Width;
		}
 
		return 0f;
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
			var flags = (TextServer.GraphemeFlag)g.Flags;

			if (flags.HasFlag(TextServer.GraphemeFlag.EmbeddedObject))
			{
				// Text icon.
				DrawTextureRect(g.IconTexture, new Rect2(position, g.IconSize), tile: false, color);
			}
			else if (!flags.HasFlag(TextServer.GraphemeFlag.Valid))
			{
				// Invalid glyph (missing from every fallback font).
				_TS.DrawHexCodeBox(canvas, g.FontSize, position, index, color);
			}
			else if (!flags.HasFlag(TextServer.GraphemeFlag.Space))
			{
				// Non-whitespace character.
				if (g.Style.ShadowSize > 0 && sColor.A > 0f)
				{
					_TS.FontDrawGlyph(g.Font, canvas, g.FontSize, position + g.Style.ShadowOffset, index, sColor);
				}

				if (g.Style.OutlineSize > 0 && oColor.A > 0f)
				{
					_TS.FontDrawGlyphOutline(g.Font, canvas, g.FontSize, g.Style.OutlineSize, position, index, oColor);
				}

				_TS.FontDrawGlyph(g.Font, canvas, g.FontSize, position, index, color);
			}

			position.X += g.Advance;
		}

		return GlyphVisibility.Visible;
	}

	private void OnStylePropertyChanged()
	{
		UpdateShaped(parse: false);
	}
}
