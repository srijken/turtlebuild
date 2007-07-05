using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace QQn.TurtleUtils.Streams
{
	class MultiStreamItemHeader
	{
		long _offset;
		long _length;
		short _itemType;

		public MultiStreamItemHeader(Stream stream)
			: this(new QQnBinaryReader(stream))
		{
		}

		public MultiStreamItemHeader()
		{
		}

		public const int ItemSize = (1 + 8 + 4 + 2) + 1; // 1 more than length of fields

		internal MultiStreamItemHeader(QQnBinaryReader reader)
		{
			byte version = reader.ReadByte();
			
			if (version == 1)
			{
				_offset = reader.ReadInt64();
				_length = reader.ReadUInt32(); // As uint
				_itemType = reader.ReadInt16();
			}
			else if (version == 2)
			{
				// Define some format which allows +4GB substream
				// When this is used we will need some more padding space; but it probably will never be written anyway
				// At least we can read them with this version

				_offset = reader.ReadInt64();
				_length = reader.ReadInt64(); // As long
				_itemType = reader.ReadInt16();
			}
			else
				throw new InvalidOperationException();
		}

		public void WriteTo(Stream stream)
		{
			WriteTo(new QQnBinaryWriter(stream));
		}

		internal void WriteTo(QQnBinaryWriter writer)
		{
			if (_length < uint.MaxValue)
			{
				writer.Write((byte)1);
				writer.Write(_offset);
				writer.Write((uint)_length); // As UInt32
				writer.Write(_itemType);
			}
			else
			{
				writer.Write((byte)2);
				writer.Write(_offset);
				writer.Write(_length); // As long
				writer.Write(_itemType);

				//throw new NotSupportedException("Big chance on buffer overflows on substreams greater than 4GB; Please review before enabling");
				// If only 1 in 4 streams is version 2 we are ok 
			}
		}

		public long Offset
		{
			get { return _offset; }
			set { _offset = value; }
		}

		public long Length
		{
			get { return _length; }
			set { _length = value; }
		}
		
		public short ItemType
		{
			get { return _itemType; }
			set { _itemType = value; }
		}

		public const short TypeMask = 0xFFF;
		public const short GZippedFlag = 0x1000;
		public const short AssuredFlag = 0x2000;

	}
}
