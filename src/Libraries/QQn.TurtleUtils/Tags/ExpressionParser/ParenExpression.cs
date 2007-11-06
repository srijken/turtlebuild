using System;
using System.Collections.Generic;
using System.Text;

namespace QQn.TurtleUtils.Tags.ExpressionParser
{
	class ParenExpression : UnaryExpression
	{
		public ParenExpression(TagToken token, TagExpression inner)
			: base(token, inner)
		{
		}
	}
}