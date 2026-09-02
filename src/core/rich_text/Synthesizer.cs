using Espejismo.Core.RichText.Parsing;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Espejismo.Core.RichText;

// One-shot interpreter that walks through a Document to produce a sequence of shape items.
// The result is stored in the specified TextBuilder.
internal struct Synthesizer(Document document, TextBuilder builder)
{
	private readonly StringBuilder _accumulatedText = new();

	private AttributeArray _attributes;

	public void Read()
	{
		WalkBranch(rootIndex: 0);
	}

	private void WalkBranch(int rootIndex)
	{
		var entityBuffer = (stackalloc char[2]);
		var childIndex = document.Nodes[rootIndex].ChildIndex;

		while (childIndex != -1 && !builder.IsExhausted)
		{
			var child = document.Nodes[childIndex];

			switch (child.Type)
			{
				case NodeType.Element:
					FlushText();
					WalkChildren(child, childIndex);
					break;

				case NodeType.Text:
					_accumulatedText.Append(document.Source, child.ValueStart, child.ValueLength);
					break;

				case NodeType.CharacterEntity:
					if (child.Entity.TryEncodeToUtf16(entityBuffer, out var charsWritten))
					{
						_accumulatedText.Append(entityBuffer[..charsWritten]);
					}
					break;
			}

			childIndex = child.SiblingIndex;
		}

		FlushText();
	}

	private readonly void FlushText()
	{
		if (_accumulatedText.Length == 0)
		{
			return;
		}

		var lines = _accumulatedText.ToString().Split('\n');

		for (var i = 0; i < lines.Length; i++)
		{
			if (i > 0)
			{
				builder.AppendBreak();
			}

			builder.AppendText(lines[i]);
		}

		_accumulatedText.Clear();
	}

	private void WalkChildren(in Node node, int nodeIndex)
	{
		var name = document.Source.AsSpan(node.ValueStart, node.ValueLength);

		if (!TextConfig.Tags.TryGetResource(name, out var tag))
		{
			WalkBranch(nodeIndex);
			return;
		}

		var attrs = ConvertAttributeRange(node.AttributeStart, node.AttributeCount);
		var success = tag.Begin(builder, attrs);

		if (node.ChildIndex != -1 && !tag.IsVoid)
		{
			WalkBranch(nodeIndex);
		}

		if (success)
		{
			tag.End(builder);
		}
	}

	[UnscopedRef]
	private ReadOnlySpan<TagAttribute> ConvertAttributeRange(int start, int length)
	{
		for (var i = 0; i < length; i++)
		{
			var attr = document.Attributes[start + i];

			_attributes[i] = new TagAttribute(
				document.Source,
				attr.NameStart,
				attr.NameLength,
				attr.ValueStart,
				attr.ValueLength);
		}

		return _attributes[..length];
	}

	[InlineArray(Tokenizer.MaxAttributes)]
	private struct AttributeArray
	{
		private TagAttribute _item;
	}
}
