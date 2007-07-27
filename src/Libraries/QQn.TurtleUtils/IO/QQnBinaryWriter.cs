using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace QQn.TurtleUtils.IO
{
	class QQnBinaryWriter : BinaryWriter
	{
		public QQnBinaryWriter(Stream output)
			: base(output, Encoding.UTF8)
		{
		}

		public void WriteSmartInt(int value)
		{
			Write7BitEncodedInt(value);
		}

		public void WriteByteArray(byte[] value)
		{
			WriteSmartInt(value.Length);
			base.Write(value);
		}
	}
}