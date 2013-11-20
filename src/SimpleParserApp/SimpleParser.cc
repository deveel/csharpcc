options{
	STATIC=false;
}

PARSER_BEGIN(SimpleParser)
namespace Deveel.CSharpCC.Parser;
			
using System;
			
public class SimpleParser {
}
			
PARSER_END(SimpleParser)
			
TOKEN: {
< READ: "read" > |
< AND: "and" > |
< PRINT: "print" >
}
			
SKIP: {
" " |
"\t"
}
			
MORE: {
"/*" : IN_MULTI_LINE_COMMENT
}
			
<IN_MULTI_LINE_COMMENT>
SPECIAL_TOKEN: {
<MULTI_LINE_COMMENT: "*/" > : DEFAULT
}
			
TOKEN: {
< STRING_LITERAL: "'" ( "''" | "\\" ["a"-"z", "\\", "%", "_", "'"] | ~["'","\\"] )* "'" >
}
			
void Input() :
{ Token t; string line; }
{
"READ" "AND" "PRINT" t = <STRING_LITERAL> { line = t.Image; }
{ Console.Out.WriteLine(line); }
}
