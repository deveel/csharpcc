using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Deveel.CSharpCC.Parser {
	public class Choice : Expansion {
		private readonly IList<Expansion> choices = new List<Expansion>();

		public Choice() {
		}

		public Choice(Token token) {
			Line = token.beginLine;
			Column = token.beginColumn;
		}

		public Choice(Expansion expansion) {
			Line = expansion.Line;
			Column = expansion.Column;
			choices.Add(expansion);
		}

		public IList<Expansion> Choices {
			get { return choices; }
		}

		public override StringBuilder Dump(int indent, IList alreadyDumped) {
			StringBuilder sb = base.Dump(indent, alreadyDumped);
			if (alreadyDumped.Contains(this))
				return sb;

			alreadyDumped.Add(this);
			foreach (var next in Choices) {
				sb.AppendLine()
					.Append(next.Dump(indent + 1, alreadyDumped));
			}
			return sb;

		}
	}
}