using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Deveel.CSharpCC.Parser {
	public class TryBlock : Expansion {
		public Expansion Expansion { get; internal set; }

		public IList<Token> Ids { get; internal set; }

		public IList<Token> CatchBlocks { get; internal set; }

		public IList<Token> FinallyBlocks { get; internal set; }

		public IList<Token> Types { get; internal set; }

		public override StringBuilder Dump(int indent, IList alreadyDumped) {
			StringBuilder sb = base.Dump(indent, alreadyDumped);
			if (alreadyDumped.Contains(this))
				return sb;
			alreadyDumped.Add(this);
			sb.AppendLine()
				.Append(Expansion.Dump(indent + 1, alreadyDumped));
			return sb;
		}
	}
}