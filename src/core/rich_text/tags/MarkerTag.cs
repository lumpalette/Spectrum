using Godot;
using System;

namespace Espejismo.Core.RichText.Tags;

/// <summary>
///   A self-closing text tag that inserts a <see cref="TextMarker"/> at the tag's position.
/// </summary>
/// <remarks>
///   <b>Attributes:</b> Varies (depends on the marker's purpose).
/// </remarks>
[GlobalClass, Tool]
public sealed partial class MarkerTag : TextTag
{
	/// <inheritdoc/>
	public override bool IsVoid => true;

	/// <summary>
	///   Gets the name of the marker, configured through the editor.
	/// </summary>
	[Export]
	public string MarkerName { get; private set; } = string.Empty;

	/// <inheritdoc/>
	public override bool Begin(TextBuilder builder, ReadOnlySpan<TagAttribute> attributes)
	{
		builder.AppendMarker(MarkerName, attributes);
		return true;
	}
}
