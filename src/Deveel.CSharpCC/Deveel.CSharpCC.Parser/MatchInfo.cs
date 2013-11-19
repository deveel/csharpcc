using System;

namespace Deveel.CSharpCC.Parser {
	public class MatchInfo {
		public static int laLimit;
		internal int[] match = new int[laLimit];
		internal int firstFreeLoc;

		public static void reInit() {
			laLimit = 0;
		}
	}
}