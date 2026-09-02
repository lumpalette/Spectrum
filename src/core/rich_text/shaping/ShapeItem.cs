using Godot;

namespace Espejismo.Core.RichText.Shaping;

internal readonly record struct ItemRun(string Text, TextStyle Style);

internal readonly record struct ItemIcon(Texture2D Texture, InlineAlignment Alignment, Vector2 Size, TextStyle Style);

internal readonly record struct ItemMarker(string Name, TagAttribute[] Attributes);

internal readonly record struct ItemBreak;

internal readonly record struct ItemAlign(HorizontalAlignment? Alignment);

// Union-like struct that holds every type of data associated to a shape item used by the shaping engine.
internal readonly struct ShapeItem
{
	private ShapeItem(ShapeItemType type) => Type = type;

	public ShapeItem(ItemRun run) : this(ShapeItemType.Run) => Run = run;

	public ShapeItem(ItemIcon icon) : this(ShapeItemType.Icon) => Icon = icon;

	public ShapeItem(ItemMarker marker) : this(ShapeItemType.Marker) => Marker = marker;

	public ShapeItem(ItemBreak br) : this(ShapeItemType.Break) => Break = br;

	public ShapeItem(ItemAlign align) : this(ShapeItemType.Align) => Align = align;

	public ShapeItemType Type { get; }

	public ItemRun? Run { get; }

	public ItemIcon? Icon { get; }

	public ItemMarker? Marker { get; }

	public ItemBreak? Break { get; }

	public ItemAlign? Align { get; }
}
