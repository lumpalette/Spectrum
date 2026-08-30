using Godot;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Espejismo.Core.RichText;

/// <summary>
///   Represents a set of style properties that serves as a template for creating <see cref="TextStyle"/> instances.
/// </summary>
[GlobalClass, Tool]
public partial class StyleTemplate : TextResource
{
	private StyleTemplate()
	{
	}

	/// <summary>
	///   Gets the font family used for the text.
	/// </summary>
	[ExportGroup("Typography")]
	[Export, NotNull]
	public FontFamily? Font
	{
		get;
		private set
		{
			if (field != value)
			{
				field = value;
				EmitChanged();
			}
		}
	}

	/// <summary>
	///   Gets the style variant applied to the text. Defaults to <see cref="FontStyle.Regular"/>.
	/// </summary>
	[Export]
	public FontStyle FontStyle
	{
		get;
		private set
		{
			if (field != value)
			{
				field = value;
				EmitChanged();
			}
		}
	}

	/// <summary>
	///   Gets the size of the text, in pixels. Defaults to 8px.
	/// </summary>
	[Export(PropertyHint.Range, "1,512,suffix:px")]
	public ushort FontSize
	{
		get;
		private set
		{
			if (field != value)
			{
				field = value;
				EmitChanged();
			}
		}
	} = 8;

	/// <summary>
	///   Gets the color tint of the text. Defaults to white.
	/// </summary>
	[Export]
	public Color Color
	{
		get;
		private set
		{
			if (field != value)
			{
				field = value;
				EmitChanged();
			}
		}
	} = Colors.White;

	/// <summary>
	///   Gets the additional space added between letters and lines of text, represented as a 2D vector. Defaults to
	///   <c>(0,8)</c>.
	/// </summary>
	[Export]
	public Vector2I Spacing
	{
		get;
		private set
		{
			if (field != value)
			{
				field = value;
				EmitChanged();
			}
		}
	} = new(0, 8);

	/// <summary>
	///   Gets the visual effect applied to the text.
	/// </summary>
	[ExportGroup("Effects")]
	[Export]
	public TextEffect? Effect
	{
		get;
		private set
		{
			if (field != value)
			{
				field = value;
				EmitChanged();
			}
		}
	}

	/// <summary>
	///   Gets the size for the shadow effect, in pixels.
	/// </summary>
	[ExportGroup("Shadow", prefix: "Shadow")]
	[Export]
	public ushort ShadowSize
	{
		get;
		private set
		{
			if (field != value)
			{
				field = value;
				EmitChanged();
			}
		}
	}

	/// <summary>
	///   Gets the color for the shadow effect. Defaults to black.
	/// </summary>
	[Export]
	public Color ShadowColor
	{
		get;
		private set
		{
			if (field != value)
			{
				field = value;
				EmitChanged();
			}
		}
	} = Colors.Black;

	/// <summary>
	///   Gets the displacement for the shadow effect. relative to the main text. Defaults to <c>(1,1)</c>.
	/// </summary>
	[Export]
	public Vector2 ShadowOffset
	{
		get;
		private set
		{
			if (field != value)
			{
				field = value;
				EmitChanged();
			}
		}
	} = Vector2.One;

	/// <summary>
	///   Gets the size for the text outline, in pixels. Defaults to 4px.
	/// </summary>
	[ExportGroup("Outline", prefix: "Outline")]
	[Export]
	public ushort OutlineSize
	{
		get;
		private set
		{
			if (field != value)
			{
				field = value;
				EmitChanged();
			}
		}
	} = 4;

	/// <summary>
	///   Gets the color for the text outline. Defaults to black.
	/// </summary>
	[Export]
	public Color OutlineColor
	{
		get;
		private set
		{
			if (field != value)
			{
				field = value;
				EmitChanged();
			}
		}
	} = Colors.Black;

	/// <summary>
	///   Creates a new <see cref="TextStyle"/> based on the data of this template.
	/// </summary>
	/// <returns>
	///   The created <see cref="TextStyle"/>, fully set.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	///   Thrown if <see cref="Font"/> is <see langword="null"/>.
	/// </exception>
	public TextStyle Create()
	{
		return CreateFrom(default);
	}

	/// <summary>
	///   Creates a copy of the specified <see cref="TextStyle"/>, replacing any unset property with the data from this
	///   template.
	/// </summary>
	/// <param name="style">
	///   The style to copy; its properties will take precedence over the ones defined by the template.
	/// </param>
	/// <returns>
	///   The created <see cref="TextStyle"/>, fully set.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	///   Thrown if <see cref="Font"/> is <see langword="null"/>.
	/// </exception>
	public TextStyle CreateFrom(in TextStyle style)
	{
		if (Font is null)
		{
			throw new InvalidOperationException("Template's font was not specified from the editor");
		}

		return new TextStyle
		{
			Font = style.Font ?? Font,
			FontSize = style.FontSize ?? FontSize,
			FontStyle = style.FontStyle ?? FontStyle,
			Color = style.Color ?? Color,
			Effect = style.Effect ?? Effect,
			Spacing = style.Spacing ?? Spacing,
			ShadowSize = style.ShadowSize ?? ShadowSize,
			ShadowColor = style.ShadowColor ?? ShadowColor,
			ShadowOffset = style.ShadowOffset ?? ShadowOffset,
			OutlineSize = style.OutlineSize ?? OutlineSize,
			OutlineColor = style.OutlineColor ?? OutlineColor
		};
	}
}
