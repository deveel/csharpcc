using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Deveel.CSharpCC.Parser;

namespace SimpleParserApp
{
    class Program
    {
        static void Main(string[] args) {
	        string line;
	        while ((line = Console.In.ReadLine()) == null)
		        continue;

	        var parser = new SimpleParser(new StringReader(line));
	        parser.Input();
        }
    }
}
