using System;

namespace Espejismo.Core.RichText;

/// <summary>
///   Represents a name-value string attribute associated with a <see cref="TextTag"/>.
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public readonly struct TagAttribute
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
	private readonly string _source;
	private readonly int _nameStart;
	private readonly int _nameLength;
	private readonly int _valueStart;
	private readonly int _valueLength;

	internal TagAttribute(string source, int nameStart, int nameLength, int valueStart, int valueLength)
	{
		_source = source;
		_nameStart = nameStart;
		_nameLength = nameLength;
		_valueStart = valueStart;
		_valueLength = valueLength;
	}

	/// <summary>
	///   Gets the name of the attribute.
	/// </summary>
	/// <remarks>
	///   An empty name indicates that the current instance is a <b>main attribute</b>, the implicit attribute
	///   specified directly after the tag name (e.g. <c>aqua</c> in <c>"&lt;color=aqua&gt;"</c>), with no explicit
	///   attribute name of its own.
	/// </remarks>
	public ReadOnlySpan<char> Name => _source.AsSpan(_nameStart, _nameLength);

	/// <summary>
	///   Gets the value of the attribute.
	/// </summary>
	public ReadOnlySpan<char> Value => _source.AsSpan(_valueStart, _valueLength);

	/// <summary>
	///   Gets a value indicating whether the attribute was specified in a tag.
	/// </summary>
	public bool IsDefined => _source is not null;

	/// <summary>
	///   Gets a value indicating whether the attribute is the main, implicit attribute of a tag.
	/// </summary>
	public bool IsMain => IsNamed(string.Empty);

	/// <summary>
	///   Indicates whether the attribute has the specified name.
	/// </summary>
	/// <param name="name">
	///   The name to check.
	/// </param>
	/// <returns>
	///   <see langword="true"/> if the attribute's name matches <paramref name="name"/>; otherwise,
	///   <see langword="false"/>.
	/// </returns>
	public bool IsNamed(ReadOnlySpan<char> name)
	{
		return Name.SequenceEqual(name);
	}
}
