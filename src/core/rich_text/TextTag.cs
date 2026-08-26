using Godot;
using System;

namespace Espejismo.Core.RichText;

/// <summary>
///   Serves as the base class for defining the behaviour of a rich-text tag during parsing.
/// </summary>
[GlobalClass, Tool]
public abstract partial class TextTag : TextResource
{
	/// <summary>
	///   Gets a value indicating whether the tag is considered a void element, that is, an element that cannot have
	///   any child nodes.
	/// </summary>
	public abstract bool IsVoid { get; }

	/// <summary>
	///   Called when an element begins, before its children are processed.
	/// </summary>
	/// <remarks>
	///   Use this method to implement whatever effect the element represents, such as pushing a new style onto
	///   <paramref name="builder"/>. Any resource the element may require (fonts, textures, etc.) should be resolved
	///   through the static <see cref="TextConfig"/> API.
	/// </remarks>
	/// <param name="builder">
	///   The working text state.
	/// </param>
	/// <param name="attributes">
	///   The attributes associated to the tag.
	/// </param>
	/// <returns>
	///   <see langword="true"/> if <see cref="End"/> should be called once the element's children have been processed;
	///   otherwise, <see langword="false"/>.
	/// </returns>
	public abstract bool Begin(TextBuilder builder, ReadOnlySpan<TagAttribute> attributes);

	/// <summary>
	///   Called when an element ends, after its children have been processed.
	/// </summary>
	/// <remarks>
	/// <para>
	///   Use this method to revert any effects done to the <paramref name="builder"/> during the <see cref="Begin"/>
	///   method. 
	/// </para>
	/// <para>
	///   The method is only called if the corresponding call to <see cref="Begin"/> returned <see langword="true"/>.
	/// </para>
	/// </remarks>
	/// <param name="builder">
	///   The working text state.
	/// </param>
#pragma warning disable CA1716 // Identifiers should not match keywords
	public virtual void End(TextBuilder builder)
	{
	}
#pragma warning restore CA1716 // Identifiers should not match keywords
}
