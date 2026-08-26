using Godot;
using System;
using System.IO;

namespace Espejismo.Core.RichText;

/// <summary>
///   Provides static, read-only access to the global configuration used by the rich-text system.
/// </summary>
[GlobalClass, Tool]
public partial class TextConfig : TextResource
{
	/// <summary>
	///   The resource path where the configuration resource file is located
	/// </summary>
	public const string Path = "res://src/core/rich_text/config.tres";

	[ExportGroup("Defaults")]
	[Export]
	private StyleTemplate? _defaultStyle;
	[Export]
	private float _fauxBoldThickness;
	[Export]
	private float _fauxItalicSlant;

	[ExportGroup("Markup")]
	[Export]
	private Godot.Collections.Dictionary<string, TextTag> _tags = [];

	[ExportGroup("Resources")]
	[Export]
	private Godot.Collections.Dictionary<string, StyleTemplate> _styles = [];
	[Export]
	private Godot.Collections.Dictionary<string, FontFamily> _fonts = [];
	[Export]
	private Godot.Collections.Dictionary<string, Texture2D> _icons = [];

	/// <summary>
	///   Gets the style template used as a last fallback for unset <see cref="TextStyle"/> properties.
	/// </summary>
	public static StyleTemplate DefaultStyle => Active._defaultStyle!;

	/// <summary>
	///   Gets the stroke thickness used to synthesize bold text when a font lacks a dedicated bold variant, in pixels.
	/// </summary>
	public static float FauxBoldThickness => Active._fauxBoldThickness;

	/// <summary>
	///   Gets the shear angle used to synthesize italic text when a font lacks a dedicated italic variant, in radians.
	/// </summary>
	public static float FauxItalicSlant => Active._fauxItalicSlant;

	/// <summary>
	///   Gets a map containing the <see cref="TextTag"/> instances available when parsing text.
	/// </summary>
	public static ResourceMap<TextTag> Tags
	{
		get
		{
#if TOOLS
			return new ResourceMap<TextTag>(Active._tags);
#else
			field ??= new ResourceMap<TextTag>(Active._tags);
			return field;
#endif
		}
	}

	/// <summary>
	///   Gets the collection of <see cref="StyleTemplate"/> resources defined through the editor.
	/// </summary>
	public static ResourceMap<StyleTemplate> Styles
	{
		get
		{
#if TOOLS
			return new ResourceMap<StyleTemplate>(Active._styles);
#else
			field ??= new ResourceMap<StyleTemplate>(Active._styles);
			return field;
#endif
		}
	}

	/// <summary>
	///   Gets the collection of <see cref="FontFamily"/> resources defined through the editor.
	/// </summary>
	public static ResourceMap<FontFamily> Fonts
	{
		get
		{
#if TOOLS
			return new ResourceMap<FontFamily>(Active._fonts);
#else
			field ??= new ResourceMap<FontFamily>(Active._fonts);
			return field;
#endif
		}
	}

	/// <summary>
	///   Gets the collection of <see cref="Texture2D"/> resources defined through the editor.
	/// </summary>
	public static ResourceMap<Texture2D> Icons
	{
		get
		{
#if TOOLS
			return new ResourceMap<Texture2D>(Active._icons);
#else
			field ??= new ResourceMap<Texture2D>(Active._icons);
			return field;
#endif
		}
	}

	private static TextConfig Active
	{
		get
		{
#if TOOLS
			return LoadFromPath();
#else
			field ??= LoadFromPath(ResourceLoader.CacheMode.Reuse);
			return field;
#endif
		}
	}

	private static TextConfig LoadFromPath()
	{
		if (!ResourceLoader.Exists(Path))
		{
			throw new FileNotFoundException($"TextConfig file at path '{Path}' was not found");
		}

		var config = GD.Load<TextConfig>(Path);

		if (config._defaultStyle is null)
		{
			throw new InvalidOperationException($"Default style template is missing. TextConfig file path: '{Path}'");
		}

		if (config._defaultStyle.Font is null)
		{
			throw new InvalidOperationException($"Font from the default style template is missing. TextConfig file path: '{Path}'");
		}

		return config;
	}
}
