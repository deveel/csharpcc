/* Copyright (c) 2012-2014, Deveel
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 *     * Redistributions of source code must retain the above copyright notice,
 *       this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Sun Microsystems, Inc. nor the names of its
 *       contributors may be used to endorse or promote products derived from
 *       this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

/*
 * This file contains the grammar and actions that describe
 * JavaCCParser.  When passed as input to JavaCCParser it generates
 * another copy of itself.  Hence JavaCCParser may be modified by
 * modifying this file, and therefore this file is to be considered
 * the master version of JavaCCParser.
 */

options {
  CSHARP_UNICODE_ESCAPE = true;
  STATIC=false;
  VISIBILITY_INTERNAL = true;
}

PARSER_BEGIN(CSharpCCParser)

namespace Deveel.CSharpCC.Parser;

using System;
using System.Collections;
using System.Collections.Generic;

class CSharpCCParser {

  string parserTypeName;
  bool processingTypeUnit = false;
  int typeNesting = 0;

  int inLocalLA = 0;
  bool inAction = false;
  bool jumpPatched = false;

  private bool NotTailOfExpansionUnit() {
    Token t;
    t = GetToken(1);
    if (t.kind == BIT_OR || 
		t.kind == COMMA || 
		t.kind == RPAREN || 
		t.kind == RBRACE || 
		t.kind == RBRACKET) 
		return false;
    return true;
  }

   public class ModifierSet
   {
     /* Definitions of the bits in the modifiers field.  */
     public const int PUBLIC = 0x0001;
     public const int PROTECTED = 0x0002;
     public const int PRIVATE = 0x0004;
     public const int ABSTRACT = 0x0008;
     public const int STATIC = 0x0010;
     public const int READONLY = 0x0020;
	 public const int INTERNAL = 0x0040;
     public const int EXTERN = 0x0080;
     public const int TRANSIENT = 0x0100;
     public const int VOLATILE = 0x0200;

     public bool isPublic(int modifiers)
     {
       return (modifiers & PUBLIC) != 0;
     }

     public bool isProtected(int modifiers)
     {
       return (modifiers & PROTECTED) != 0;
     }

     public bool isPrivate(int modifiers)
     {
       return (modifiers & PRIVATE) != 0;
     }

	 public bool isInternal(int modifier) {
		 return (modifier & INTERNAL) != 0;
	 }

     public bool isStatic(int modifiers)
     {
       return (modifiers & STATIC) != 0;
     }

     public bool isAbstract(int modifiers)
     {
       return (modifiers & ABSTRACT) != 0;
     }

     public bool isReadOnly(int modifiers)
     {
       return (modifiers & READONLY) != 0;
     }

     public bool isExtern(int modifiers)
     {
       return (modifiers & EXTERN) != 0;
     }

     public bool isTransient(int modifiers)
      {
       return (modifiers & TRANSIENT) != 0;
     }

     public bool isVolatile(int modifiers)
     {
       return (modifiers & VOLATILE) != 0;
     }

     static int removeModifier(int modifiers, int mod)
     {
        return modifiers & ~mod;
     }
   }


}

PARSER_END(CSharpCCParser)

TOKEN_MGR_DECLS :
{
   int[] beginLine = new int[10];
   int[] beginCol = new int[10];
   int depth = 0;
   int size = 10;

   void SaveBeginLineCol(int l, int c)
   {
      if (depth == size)
      {
         size += 5;
         int[] tmpbeginLine = new int[size];
         int[] tmpbeginCol = new int[size];

         Array.Copy(beginLine, 0, beginLine = tmpbeginLine, 0, depth);
         Array.Copy(beginCol, 0, beginCol = tmpbeginCol, 0, depth);
      }

      beginLine[depth] = l;
      beginCol[depth] = c;
      depth++;
   }

   void RestoreBeginLineCol()
   {
      depth--;
      input_stream.AdjustBeginLineColumn(beginLine[depth], beginCol[depth]);
   }
}


/**********************************************
 * THE CSHARPCC TOKEN SPECIFICATION STARTS HERE *
 **********************************************/

/* CSHARPCC RESERVED WORDS: These are the only tokens in CSharpCC but not in C# */

TOKEN :
{
// "options" is no longer reserved (see issue 126).
//  < _OPTIONS: "options" >
  < _LOOKAHEAD: "LOOKAHEAD" >
| < _IGNORE_CASE: "IGNORE_CASE" >
| < _PARSER_BEGIN: "PARSER_BEGIN" >
| < _PARSER_END: "PARSER_END" >
| < _CODE: "CODE" >
| < _TOKEN: "TOKEN" >
| < _SPECIAL_TOKEN: "SPECIAL_TOKEN" >
| < _MORE: "MORE" >
| < _SKIP: "SKIP" >
| < _TOKEN_MGR_DECLS: "TOKEN_MGR_DECLS" >
| < _EOF: "EOF" >
}

/*
 * The remainder of the tokens are exactly (except for the removal of tokens
 * containing ">>" and "<<") as in the Java grammar and must be diff equivalent
 * (again with the exceptions above) to it.
 */

/* WHITE SPACE */

SKIP :
{
  " "
| "\t"
| "\n"
| "\r"
| "\f"
| "/*@egen*/" : AFTER_EGEN

}

<AFTER_EGEN> SKIP :
{
  <~[]> { RestoreBeginLineCol(); input_stream.Backup(1); } : DEFAULT
}

/* COMMENTS */

MORE :
{
  "//" : IN_SINGLE_LINE_COMMENT
|
  <"/**" ~["/"]> { input_stream.Backup(1); } : IN_FORMAL_COMMENT
|
  "/*" : IN_MULTI_LINE_COMMENT
|
  "/*@bgen(csTree"
     {
        SaveBeginLineCol(input_stream.BeginLine,
                         input_stream.BeginColumn);
     } : IN_MULTI_LINE_COMMENT
}

<IN_SINGLE_LINE_COMMENT>
SPECIAL_TOKEN :
{
  <SINGLE_LINE_COMMENT: "\n" | "\r" | "\r\n" > : DEFAULT
}

<IN_FORMAL_COMMENT>
SPECIAL_TOKEN :
{
  <FORMAL_COMMENT: "*/" > : DEFAULT
}

<IN_MULTI_LINE_COMMENT>
SPECIAL_TOKEN :
{
  <MULTI_LINE_COMMENT: "*/" > : DEFAULT
}

<IN_SINGLE_LINE_COMMENT,IN_FORMAL_COMMENT,IN_MULTI_LINE_COMMENT>
MORE :
{
  < ~[] >
}

/* JAVA RESERVED WORDS AND LITERALS */

TOKEN :
{
  < ABSTRACT: "abstract" >
| < BASE: "base" >
| < BOOL: "bool" >
| < BREAK: "break" >
| < BYTE: "byte" >
| < CASE: "case" >
| < CATCH: "catch" >
| < CHAR: "char" >
| < CLASS: "class" >
| < CONST: "const" >
| < CONTINUE: "continue" >
| < _DEFAULT: "default" >
| < DO: "do" >
| < DOUBLE: "double" >
| < ELSE: "else" >
| < ENUM: "enum" >
| <EXTERN: "extern" >
| < FALSE: "false" >
| < FINALLY: "finally" >
| < FLOAT: "float" >
| < FOR: "for" >
| < GOTO: "goto" >
| < IF: "if" >
| < INT: "int" >
| < INTERFACE: "interface" >
| < INTERNAL: "internal" >
| < IS: "is" >
| < LOCK: "lock" >
| < LONG: "long" >
| < NAMESPACE: "namespace" >
| < NEW: "new" >
| < NULL: "null" >
| < PRIVATE: "private" >
| < PROTECTED: "protected" >
| < PUBLIC: "public" >
| <READONLY: "readonly" >
| < RETURN: "return" >
| < SHORT: "short" >
| < STATIC: "static" >
| < SWITCH: "switch" >
| < THIS: "this" >
| < THROW: "throw" >
| < TRANSIENT: "transient" >
| < TRUE: "true" >
| < TRY: "try" >
| < USING: "using" >
| < VAR: "var" >
| < VOID: "void" >
| < VOLATILE: "volatile" >
| < WHILE: "while" >
}

/* LITERALS */

TOKEN :
{
  < INTEGER_LITERAL:
        <DECIMAL_LITERAL> (["l","L"])?
      | <HEX_LITERAL> (["l","L"])?
      | <OCTAL_LITERAL> (["l","L"])?
  >
|
  < #DECIMAL_LITERAL: ["1"-"9"] (["0"-"9"])* >
|
  < #HEX_LITERAL: "0" ["x","X"] (["0"-"9","a"-"f","A"-"F"])+ >
|
  < #OCTAL_LITERAL: "0" (["0"-"7"])* >
|
  < FLOATING_POINT_LITERAL:
        <DECIMAL_FLOATING_POINT_LITERAL>
      | <HEXADECIMAL_FLOATING_POINT_LITERAL>
  >
|
  < #DECIMAL_FLOATING_POINT_LITERAL:
        (["0"-"9"])+ "." (["0"-"9"])* (<DECIMAL_EXPONENT>)? (["f","F","d","D"])?
      | "." (["0"-"9"])+ (<DECIMAL_EXPONENT>)? (["f","F","d","D"])?
      | (["0"-"9"])+ <DECIMAL_EXPONENT> (["f","F","d","D"])?
      | (["0"-"9"])+ (<DECIMAL_EXPONENT>)? ["f","F","d","D"]
  >
|
  < #DECIMAL_EXPONENT: ["e","E"] (["+","-"])? (["0"-"9"])+ >
|
  < #HEXADECIMAL_FLOATING_POINT_LITERAL:
        "0" ["x", "X"] (["0"-"9","a"-"f","A"-"F"])+ (".")? <HEXADECIMAL_EXPONENT> (["f","F","d","D"])?
      | "0" ["x", "X"] (["0"-"9","a"-"f","A"-"F"])* "." (["0"-"9","a"-"f","A"-"F"])+ <HEXADECIMAL_EXPONENT> (["f","F","d","D"])?
  >
|
  < #HEXADECIMAL_EXPONENT: ["p","P"] (["+","-"])? (["0"-"9"])+ >
|
  < CHARACTER_LITERAL:
      "'"
      (   (~["'","\\","\n","\r"])
        | ("\\"
            ( ["n","t","b","r","f","\\","'","\""]
            | ["0"-"7"] ( ["0"-"7"] )?
            | ["0"-"3"] ["0"-"7"] ["0"-"7"]
            )
          )
      )
      "'"
  >
|
  < STRING_LITERAL:
      "\""
      (   (~["\"","\\","\n","\r"])
        | ("\\"
            ( ["n","t","b","r","f","\\","'","\""]
            | ["0"-"7"] ( ["0"-"7"] )?
            | ["0"-"3"] ["0"-"7"] ["0"-"7"]
            )
          )
      )*
      "\""
  >
}

/* SEPARATORS */

TOKEN :
{
  < LPAREN: "(" >
| < RPAREN: ")" >
| < LBRACE: "{" >
| < RBRACE: "}" >
| < LBRACKET: "[" >
| < RBRACKET: "]" >
| < SEMICOLON: ";" >
| < COMMA: "," >
| < DOT: "." >
}

/* OPERATORS */

TOKEN :
{
  < ASSIGN: "=" >
//| < GT: ">" >
| < LT: "<" >
| < BANG: "!" >
| < TILDE: "~" >
| < HOOK: "?" >
| < COLON: ":" >
| < EQ: "==" >
| < LE: "<=" >
| < GE: ">=" >
| < NE: "!=" >
| < SC_OR: "||" >
| < SC_AND: "&&" >
| < INCR: "++" >
| < DECR: "--" >
| < PLUS: "+" >
| < MINUS: "-" >
| < STAR: "*" >
| < SLASH: "/" >
| < BIT_AND: "&" >
| < BIT_OR: "|" >
| < XOR: "^" >
| < REM: "%" >
| < PLUSASSIGN: "+=" >
| < MINUSASSIGN: "-=" >
| < STARASSIGN: "*=" >
| < SLASHASSIGN: "/=" >
| < ANDASSIGN: "&=" >
| < ORASSIGN: "|=" >
| < XORASSIGN: "^=" >
| < REMASSIGN: "%=" >
}

/* >'s need special attention due to generics syntax. */
TOKEN :
{
 < RSIGNEDSHIFT: ">>" >
  {
     matchedToken.kind = GT;
     ((Token.GTToken)matchedToken).realKind = CSharpCCParserConstants.RSIGNEDSHIFT;
     input_stream.Backup(1);
     matchedToken.image = ">";
  }
| < GT: ">" >
}


/************************************************
 * THE CSHARPCC GRAMMAR SPECIFICATION STARTS HERE *
 ************************************************/

void charpcc_input() :
	{
	  String id1, id2;
	  CSharpCCParserInternals.initialize();
	}
{
  csharpcc_options()
  "PARSER_BEGIN" "(" id1=identifier()
	{
	  CSharpCCParserInternals.addcuname(id1);
	}
                 ")"
	{
	  processingTypeUnit = true;
	  parserTypeName = id1;
	}
  CompilationUnit()
	{
	  processingTypeUnit = false;
	}
  "PARSER_END" "(" id2=identifier()
	{
	  CSharpCCParserInternals.compare(GetToken(0), id1, id2);
	}
               ")"
  ( production() )+
  <EOF>
}

void csharpcc_options() :
{}
{
  [ LOOKAHEAD({ GetToken(1).image.Equals("options") }) <IDENTIFIER>  "{" ( option_binding() )* "}" ]
	{
	  Options.Normalize();
	}
}

void option_binding() :
	{
	  String option_name;
	  int int_val;
	  bool bool_val;
	  String string_val;
	  Token t = GetToken(1);
	}
{
  ( <IDENTIFIER> | "LOOKAHEAD" | "IGNORE_CASE" | "static" )
	{
	  option_name = t.image;
	}
  "="
  (
    int_val = IntegerLiteral()
	{
	  Options.SetInputFileOption(t, GetToken(0), option_name, int_val);
	}
  |
    bool_val = BooleanLiteral()
	{
	  Options.SetInputFileOption(t, GetToken(0), option_name, bool_val);
	}
  |
    string_val = StringLiteral()
	{
	  Options.SetInputFileOption(t, GetToken(0), option_name, string_val);
	}
  )
  ";"
}

void production() :
{}
{
  LOOKAHEAD(1)
  /*
   * Since CODE is both a CSharpCC reserved word and a C# identifier,
   * we need to give preference to "cscode_production" over
   * "bnf_production".
   */
  cscode_production()
|
  LOOKAHEAD(1)
  /*
   * Since SKIP, TOKEN, etc. are both JavaCC reserved words and Java
   * identifiers, we need to give preference to "regular_expression_production"
   * over "bnf_production".
   */
  regular_expr_production()
|
  LOOKAHEAD(1)
  /*
   * Since TOKEN_MGR_DECLS is both a CSharpCC reserved word and a C# identifier,
   * we need to give preference to "token_manager_decls" over
   * "bnf_production".
   */
  token_manager_decls()
|
  bnf_production()
}

void cscode_production() :
	{
	  CodeProduction p = new CodeProduction();
	  String lhs;
	  Token t = GetToken(1);
	  p.FirstToken = t;
	  p.Line = t.beginLine;
	  p.Column = t.beginColumn;
	}
{
  "CODE"
  AccessModifier(p)
  ResultType(p.ReturnTypeTokens)
  lhs=identifier()	{ p.Lhs = lhs; }
  FormalParameters(p.ParameterTokens)
  Block(p.CodeTokens)
	{
	  p.LastToken = GetToken(0);
	  CSharpCCParserInternals.addproduction(p);
	}
}

void bnf_production() :
	{
	  BnfProduction p = new BnfProduction();
	  Container c = new Container();
	  Token t = GetToken(1);
	  p.FirstToken = t;
	  String lhs;
	  p.Line =t.beginLine;
	  p.Column =t.beginColumn;
	  jumpPatched = false;
	}
{
  AccessModifier(p)
  ResultType(p.ReturnTypeTokens)
  lhs=identifier()  { p.Lhs = lhs; }
  FormalParameters(p.ParameterTokens)
  ":"
  Block(p.DeclarationTokens)
  "{" expansion_choices(c) t="}"
	{
	  p.LastToken = t;
	  p.IsJumpPatched = jumpPatched;
	  CSharpCCParserInternals.production_addexpansion(p, (Expansion)(c.member));
	  CSharpCCParserInternals.addproduction(p);
	}
}

void AccessModifier(NormalProduction p) :
	{
	  Token t = null;
	}
{
	( t = "public" | t = "protected" | t = "private" | t = "internal")?
	{
	  if(t != null){
	    p.AccessModifier = t.image;
	  }
	}
}

void regular_expr_production() :
	{
	  TokenProduction p = new TokenProduction();
	  List<String> states;
	  Token t = p.FirstToken = GetToken(1);
	  p.Line = t.beginLine;
	  p.Column = t.beginColumn;
	}
{
	{
	  // set p.lexStates assuming there is no state spec.
	  // and then override if necessary.
	  p.LexStates = new String[] {"DEFAULT"};
	}
  [
    LOOKAHEAD(2) "<" "*" ">"
	{
	  p.LexStates = null;
	}
  |
    "<"
	{
	  states = new List<String>();
	}
      t=<IDENTIFIER>
	{
	  states.Add(t.image);
	}
      ( "," t=<IDENTIFIER>
	{
	  states.Add(t.image);
	}
      )*
    ">"
	{
	  p.LexStates = states.ToArray();
	}
  ]
  regexpr_kind(p)
	{
	  if (p.Kind != TokenProduction.TOKEN && Options.getUserTokenManager()) {
	    CSharpCCErrors.Warning(GetToken(0), "Regular expression is being treated as if it were a TOKEN since option USER_TOKEN_MANAGER has been set to true.");
	  }
	}
  [
    "[" t="IGNORE_CASE" "]"
	{
	  p.IgnoreCase = true;
	  if (Options.getUserTokenManager()) {
	    CSharpCCErrors.Warning(t, "Ignoring \"IGNORE_CASE\" specification since option USER_TOKEN_MANAGER has been set to true.");
	  }
	}
  ]
  ":"
  "{" regexpr_spec(p) ( "|" regexpr_spec(p) )* t="}"
	{
	  p.LastToken = t;
	  CSharpCCParserInternals.addregexpr(p);
	}
}

void token_manager_decls() :
	{
	  IList<Token> decls = new List<Token>();
	  Token t;
	}
{
  t="TOKEN_MGR_DECLS" ":"
  ClassOrInterfaceBody(false, decls)
	{
	  CSharpCCParserInternals.add_token_manager_decls(t, decls);
	}
}

void regexpr_kind(TokenProduction p) :
{}
{
  "TOKEN"
	{
	  p.Kind = TokenProduction.TOKEN;
	}
|
  "SPECIAL_TOKEN"
	{
	  p.Kind = TokenProduction.SPECIAL;
	}
|
  "SKIP"
	{
	  p.Kind = TokenProduction.SKIP;
	}
|
  "MORE"
	{
	  p.Kind = TokenProduction.MORE;
	}
}

void regexpr_spec(TokenProduction p) :
	{
	  Container c = new Container();
	  Action act = new Action();
	  Token t = null;
	  RegExprSpec res = new RegExprSpec();
	}
{
  regular_expression(c)
	{
	  res.RegularExpression = (RegularExpression)c.member;
	  res.RegularExpression.TokenProductionContext = p;
	}
  [
	{
	  t = GetToken(1);
	}
    Block(act.ActionTokens)
	{
	  if (Options.getUserTokenManager()) {
	    CSharpCCErrors.Warning(t, "Ignoring action in regular expression specification since option USER_TOKEN_MANAGER has been set to true.");
	  }
	  if (res.RegularExpression.IsPrivate) {
	    CSharpCCErrors.ParseError(t, "Actions are not permitted on private (#) regular expressions.");
	  }
	}
  ]
  [ ":" t=<IDENTIFIER>
	{
	  res.NextState = t.image;
	  if (res.RegularExpression.IsPrivate) {
	    CSharpCCErrors.ParseError(t, "Lexical state changes are not permitted after private (#) regular expressions.");
	  }
	}
  ]
	{
	  res.Action = act;
	  res.NextStateToken = t;
	  p.RegexSpecs.Add(res);
	}
}

void expansion_choices(Container c1) :
	{
	  bool morethanone = false;
	  Choice ch = null; // unnecessary initialization to make compiler happy!
	  Container c2 = new Container();
	}
{
  expansion(c1)
  ( "|" expansion(c2)
	{
	  if (morethanone) {
	    ch.Choices.Add((Expansion) c2.member);
	    ((Expansion)c2.member).Parent = ch;
	  } else {
	    morethanone = true;
	    ch = new Choice((Expansion)c1.member);
	    ((Expansion)c1.member).Parent = ch;
	    ch.Choices.Add((Expansion)c2.member);
	    ((Expansion)c2.member).Parent = ch;
	  }
	}
  )*
	{
	  if (morethanone) {
	    c1.member = ch;
	  }
	}
}

void expansion(Container c1) :
	{
	  Sequence seq = new Sequence();
	  Container c2 = new Container();
	  Lookahead la = new Lookahead();
	  Token t = GetToken(1);
	  seq.Line = t.beginLine;
	  seq.Column=t.beginColumn;
	  la.Line = t.beginLine;
	  la.Column =t.beginColumn;
	}
{
	{
	  la.Amount = Options.getLookahead();
	  la.Expansion = null;
	  la.IsExplicit = false;
	}
  ( LOOKAHEAD(1)
    t="LOOKAHEAD" "(" la=local_lookahead() ")"
	{
	  if (inLocalLA != 0 && la.Amount != 0) {
	    CSharpCCErrors.Warning(t, "Only semantic lookahead specifications within other lookahead specifications is considered.  Syntactic lookahead is ignored.");
	  }
	}
  )?
	{
	  seq.Units.Add(la);
	}
  ( LOOKAHEAD(0, { NotTailOfExpansionUnit() } )
    expansion_unit(c2)
	{
	  seq.Units.Add((Lookahead)c2.member);
	  ((Expansion)c2.member).Parent = seq;
	  ((Expansion)c2.member).Ordinal = seq.Units.Count-1;
	}
  )+
	{
	  if (la.Expansion == null) {
	    la.Expansion = seq;
	  }
	  c1.member = seq;
	}
}

Lookahead local_lookahead() :
	{
	  Lookahead la = new Lookahead();
	  la.IsExplicit = true;
	  Token t = GetToken(1);
	  la.Line = t.beginLine;
	  la.Column = t.beginColumn;
	  la.Expansion = null;
	  Container c = new Container();
	  bool commaAtEnd = false, emptyLA = true;
	  int laAmount;
	  inLocalLA++;
	}
{
  [
    /*
     * The lookahead of 1 is to turn off the warning message that lets
     * us know that an expansion choice can also start with an integer
     * literal because a primary expression can do the same.  But we
     * know that this is what we want.
     */
    LOOKAHEAD(1)
    laAmount = IntegerLiteral()
	{
	  emptyLA = false;
	  la.Amount = laAmount;
	}
  ]
  [ LOOKAHEAD(0, { !emptyLA && (GetToken(1).kind != RPAREN) } )
    ","
	{
	  commaAtEnd = true;
	}
  ]
  [ LOOKAHEAD(0, { GetToken(1).kind != RPAREN && GetToken(1).kind != LBRACE } )
    expansion_choices(c)
	{
	  emptyLA = false; commaAtEnd = false;
	  la.Expansion = (Expansion)c.member;
	}
  ]
  [ LOOKAHEAD(0, { !emptyLA && !commaAtEnd && (GetToken(1).kind != RPAREN) } )
    ","
	{
	  commaAtEnd = true;
	}
  ]
  [ LOOKAHEAD(0, { emptyLA || commaAtEnd } )
    "{" Expression(la.ActionTokens) "}"
	{
	  if (emptyLA) {
	    la.Amount = 0;
	  }
	}
  ]
	{
	  inLocalLA--;
	  return la;
	}
}

void expansion_unit(Container c) :
	{
	  String name;
	  IList<Token> lhsTokens = new List<Token>();
	  NonTerminal nt;
	  Action act;
	  Token t;
	  Lookahead la;
	}
{
  LOOKAHEAD(1)
  /*
   * We give this priority over primary expressions which use LOOKAHEAD as the
   * name of its identifier.
   */
  t="LOOKAHEAD" "(" la=local_lookahead() ")"
	{
	  // Now set the la_expansion field of la with a dummy
	  // expansion (we use EOF).
	  la.Expansion =new REndOfFile();
	  // Create a singleton choice with an empty action.
	  Choice ch = new Choice(t);
	  Sequence seq = new Sequence(t, la);
	  la.Parent = seq; la.Ordinal = 0;
	  act = new Action();
	  act.Line = t.beginLine;
	  act.Column = t.beginColumn;
	  seq.Units.Add(act);
	  act.Parent = seq; act.Ordinal = 1;
	  ch.Choices.Add(seq);
	  seq.Parent = ch; seq.Ordinal = 0;
	  if (la.Amount != 0) {
	    if (la.ActionTokens.Count != 0) {
	      CSharpCCErrors.Warning(t, "Encountered LOOKAHEAD(...) at a non-choice location.  Only semantic lookahead will be considered here.");
	    } else {
	      CSharpCCErrors.Warning(t, "Encountered LOOKAHEAD(...) at a non-choice location.  This will be ignored.");
	    }
	  }
	  c.member = ch;
	}
|
	{
	  act = new Action();
	  t = GetToken(1);
	  act.Line = t.beginLine;
	  act.Column = t.beginColumn;
	  inAction = true;
	}
  Block(act.ActionTokens)
	{
	  inAction = false;
	  if (inLocalLA != 0) {
	    CSharpCCErrors.Warning(t, "Action within lookahead specification will be ignored.");
	  }
	  c.member = act;
	}
|
  t="[" expansion_choices(c) "]"
	{
	  c.member = new ZeroOrOne(t, (Expansion)c.member);
	}
|
	{
	  Container expch = new Container();
	  List<Token> types = new List<Token>();
	  List<Token> ids = new List<Token>();
	  List<Token> catchblks = new List<Token>();
	  List<Token> finallyblk = null;
	  List<Token> vec = new List<Token>();
	  Token t0;
	}
  t0="try" "{" expansion_choices(expch) "}"
  ( "catch" "(" Name(vec) t=<IDENTIFIER> ")"
	{
	  types.AddRange(vec);
	  ids.Add(t);
	  vec = new List<Token>();
	  inAction = true;
	}
    Block(vec)
	{
	  inAction = false;
	  catchblks.AddRange(vec);
	  vec = new List<Token>();
	}
  )*
  [
	{
	  inAction = true;
	}
    "finally" Block(vec)
	{
	  inAction = false;
	  finallyblk = vec;
	}
  ]
	{
	  CSharpCCParserInternals.makeTryBlock(t0, c, expch, types, ids, catchblks, finallyblk);
	}
|
  LOOKAHEAD(
    identifier()
  |
    StringLiteral()
  |
    "<"
  |
    PrimaryExpression() "="
  )
  [
    LOOKAHEAD(PrimaryExpression() "=")
	{
	  Token first = GetToken(1);
	}
    PrimaryExpression()
	{
	  Token last = GetToken(0);
	}
    "="
	{
	  t = first;
	  while (true) {
	    lhsTokens.Add(t);
	    if (t == last) break;
	    t = t.next;
	  }
	}
  ]
  (
	LOOKAHEAD( identifier() "(")
	{
	  nt = new NonTerminal();
	  t = GetToken(1);
	  nt.Line = t.beginLine;
	  nt.Column = t.beginColumn;
	  nt.LhsTokens = lhsTokens;
	}
   name=identifier() Arguments(nt.ArgumentTokens)
	{
	  nt.Name = name;
	  c.member = nt;
	}
  |
    regular_expression(c)
	{
	  ((RegularExpression)(c.member)).LhsTokens = lhsTokens;
	  CSharpCCParserInternals.add_inline_regexpr((RegularExpression)(c.member));
	}
	[ "." t=<IDENTIFIER> { ((RegularExpression)(c.member)).RhsToken = t; } ]
  )
|
  t="(" expansion_choices(c) ")"
  (  "+" { c.member = new OneOrMore(t, (Expansion)c.member); }
   | "*" { c.member = new ZeroOrMore(t, (Expansion)c.member); }
   | "?" { c.member = new ZeroOrOne(t, (Expansion)c.member); }
  )?
}

void regular_expression(Container c) :
	{
	  REndOfFile ef;
	  String image;
	  bool private_rexp = false;
	  Token t = GetToken(1);
	}
{
  image=StringLiteral()
	{
	  c.member = new RStringLiteral(t, image);
	}
|
  LOOKAHEAD(3)
	{
	  image = "";
	}
  < LANGLE: "<" >
  [
    [ "#"
	{
	  private_rexp = true;
	}
    ]
    image=identifier() ":"
  ]
  complex_regular_expression_choices(c) < RANGLE: ">" >
	{
	  RegularExpression re;
	  if (c.member is RJustName) {
	    RSequence seq = new RSequence();
	    seq.Units.Add((RegularExpression)c.member);
	    re = seq;
	  } else {
	    re = (RegularExpression)c.member;
	  }
	  re.Label = image;
	  re.IsPrivate = private_rexp;
	  re.Line = t.beginLine;
	  re.Column = t.beginColumn;
	  c.member = re;
	}
|
  LOOKAHEAD(2)
  "<" image=identifier() ">"
	{
	  c.member = new RJustName(t, image);
	}
|
  "<" "EOF" ">"
	{
	  ef = new REndOfFile();
	  ef.Line = t.beginLine;
	  ef.Column = t.beginColumn;
	  ef.Ordinal = 0;
	  c.member = ef;
	}
}

void complex_regular_expression_choices(Container c1) :
	{
	  bool morethanone = false;
	  RChoice ch = null; // unnecessary initialization to make Java compiler happy!
	  Container c2 = new Container();
	}
{
  complex_regular_expression(c1)
  ( "|" complex_regular_expression(c2)
	{
	  if (morethanone) {
	    ch.Choices.Add((RegularExpression)c2.member);
	  } else {
	    morethanone = true;
	    ch = new RChoice();
	    ch.Line = ((RegularExpression)c1.member).Line;
	    ch.Column = ((RegularExpression)c1.member).Column;
	    ch.Choices.Add((RegularExpression)c1.member);
	    ch.Choices.Add((RegularExpression)c2.member);
	  }
	}
  )*
	{
	  if (morethanone) {
	    c1.member = ch;
	  }
	}
}

void complex_regular_expression(Container c1) :
	{
	  int count = 0;
	  RSequence seq = null; // unnecessary initialization to make compiler happy!
	  Container c2 = new Container();
	}
{
  ( complex_regular_expression_unit(c2)
	{
	  count++;
	  if (count == 1) {
	    c1.member = c2.member; // if count does not go beyond 1, we are done.
	  } else if (count == 2) { // more than 1, so create a sequence.
	    seq = new RSequence();
	    seq.Line = ((RegularExpression)c1.member).Line;
	    seq.Column = ((RegularExpression)c1.member).Column;
	    seq.Units.Add((RegularExpression)c1.member);
	    seq.Units.Add((RegularExpression)c2.member);
	  } else {
	    seq.Units.Add((RegularExpression)c2.member);
	  }
	}
  )+
	{
	  if (count > 1) {
	    c1.member = seq;
	  }
	}
}

void complex_regular_expression_unit(Container c) :
	{
	  String image;
	  Token t = GetToken(1);
          int r1 = 0, r2 = -1;
          bool hasMax = false;
	}
{
  image=StringLiteral()
	{
	  c.member = new RStringLiteral(t, image);
	}
|
  "<" image=identifier() ">"
	{
	  c.member = new RJustName(t, image);
	}
|
  character_list(c)
|
  "(" complex_regular_expression_choices(c) ")"
  (  "+"
	{
	  c.member = new ROneOrMore(t, (RegularExpression)c.member);
	}
   | "*"
	{
	  c.member = new RZeroOrMore(t, (RegularExpression)c.member);
	}
   | "?"
	{
	  RZeroOrOne zorexp = new RZeroOrOne();
	  zorexp.Line = t.beginLine;
	  zorexp.Column = t.beginColumn;
	  zorexp.RegularExpression = (RegularExpression)c.member;
	  c.member = zorexp;
	}
   | "{" r1 = IntegerLiteral()
         [ "," { hasMax = true; } [ r2 = IntegerLiteral() ] ]
     "}"
	{
	  RRepetitionRange rrrexp = new RRepetitionRange();
	  rrrexp.Line = t.beginLine;
	  rrrexp.Column = t.beginColumn;
	  rrrexp.Min = r1;
	  rrrexp.Max = r2;
        rrrexp.HasMax = hasMax;
	  rrrexp.RegularExpression = (RegularExpression)c.member;
	  c.member = rrrexp;
	}
  )?
}

void character_list(Container c1) :
	{
	  RCharacterList chlist = new RCharacterList();
	  Token t = GetToken(1);
	  chlist.Line = t.beginLine;
	  chlist.Column = t.beginColumn;
	  Container c2 = new Container();
	}
{
  ["~"
	{
	  chlist.Negated = true;
	}
  ]
  "[" [ character_descriptor(c2)
	{
	  chlist.Descriptors.Add(c2.member);
	}
        ( "," character_descriptor(c2)
	{
	  chlist.Descriptors.Add(c2.member);
	}
        )*
      ]
  "]"
	{
	  c1.member = chlist;
	}
}

void character_descriptor(Container c) :
	{
	  char c1, c2 = ' '; // unnecessary initialization to make compiler happy!
	  bool isrange = false;
	  String imageL, imageR;
	  Token t = GetToken(1);
	}
{
  imageL=StringLiteral()
	{
	  c1 = CSharpCCParserInternals.character_descriptor_assign(GetToken(0), imageL);
	}
  [ "-" imageR=StringLiteral()
	{
	  isrange = true;
	  c2 = CSharpCCParserInternals.character_descriptor_assign(GetToken(0), imageR, imageL);
	}
  ]
	{
	  if (isrange) {
	    CharacterRange cr = new CharacterRange();
	    cr.Line = t.beginLine;
	    cr.Column = t.beginColumn;
        cr.Left =c1;
        cr.Right = c2;
	    c.member = cr;
	  } else {
	    SingleCharacter sc = new SingleCharacter();
	    sc.Line = t.beginLine;
	    sc.Column = t.beginColumn;
	    sc.Character = c1;
	    c.member = sc;
	  }
	}
}

String identifier() :
	{
	  Token t;
	}
{
  t=<IDENTIFIER>
	{
	  return t.image;
	}
}

/**********************************************
 * THE C# GRAMMAR SPECIFICATION STARTS HERE *
 **********************************************/

/*
 * The C# grammar is modified to use sequences of tokens
 * for the missing tokens - those that include "<<" and ">>".
 */

/*
 * The following production defines Java identifiers - it
 * includes the reserved words of JavaCC also.
 */

Token CSharpIdentifier() :
{}
{
(
  <IDENTIFIER>
| "LOOKAHEAD"
| "IGNORE_CASE"
| "PARSER_BEGIN"
| "PARSER_END"
| "JAVACODE"
| "TOKEN"
| "SPECIAL_TOKEN"
| "MORE"
| "SKIP"
| "TOKEN_MGR_DECLS"
| "EOF"
)
	{
	  Token retval = GetToken(0);
	  retval.kind = IDENTIFIER;
	  return retval;
	}
}

/*
 * Program structuring syntax follows.
 */

void CompilationUnit() :
/*
 * The <EOF> is deleted since the compilation unit is embedded
 * within grammar code.
 */
	{
	  CSharpCCParserInternals.set_initial_cu_token(GetToken(1));
	}
{
  [ LOOKAHEAD( ( Annotation() )* "namespace" ) NamespaceDeclaration() ]
  ( UsingDeclaration() )*
  ( TypeDeclaration() )*
	{
	  CSharpCCParserInternals.insertionpointerrors(GetToken(1));
	}
}

void NamespaceDeclaration() :
{}
{
  "namespace" Name(null) ";"
}

void UsingDeclaration() :
{}
{
  "using" Name(null) [ "=" Name(null) ]";"
}

/*
 * Modifiers. We match all modifiers in a single rule to reduce the chances of
 * syntax errors for simple modifier mistakes. It will also enable us to give
 * better error messages.
 */

int Modifiers():
{
   int modifiers = 0;
}
{
 (
  LOOKAHEAD(2)
  (
   "public" { modifiers |= ModifierSet.PUBLIC; }
  |
   "static" { modifiers |= ModifierSet.STATIC; }
  |
   "protected" { modifiers |= ModifierSet.PROTECTED; }
  |
   "private" { modifiers |= ModifierSet.PRIVATE; }
  |
	  "internal" { modifiers |= ModifierSet.INTERNAL; }
  |
   "readonly" { modifiers |= ModifierSet.READONLY; }
  |
   "abstract" { modifiers |= ModifierSet.ABSTRACT; }
  |
   "extern" { modifiers |= ModifierSet.EXTERN; }
  |
   "transient" { modifiers |= ModifierSet.TRANSIENT; }
  |
   "volatile" { modifiers |= ModifierSet.VOLATILE; }
  |
   Annotation()
  )
 )*

 {
    return modifiers;
 }
}

/*
 * Declaration syntax follows.
 */
void TypeDeclaration():
{
   int modifiers;
}
{
  ";"
|
  modifiers = Modifiers()
  (
     ClassOrInterfaceDeclaration(modifiers, null)
   |
     EnumDeclaration(modifiers)
  )
}


void ClassOrInterfaceDeclaration(int modifiers, IList tokens):
{
	  typeNesting++;
	  Token t;
	  bool is_parser_class = false;
	  bool isInterface = false;
  if (tokens == null)
    tokens = new ArrayList();
}
{
	( "class" | "interface" { isInterface = true; } )
  t=<IDENTIFIER>
  [ TypeParameters() ]
  [ InheritList() ]
	{
	  if (t.image.Equals(parserTypeName) && typeNesting == 1 && processingTypeUnit) {
	    is_parser_class = true;
	    CSharpCCParserInternals.setinsertionpoint(GetToken(1), 1);
	  }
	}
  ClassOrInterfaceBody(isInterface, null)
	{
	  if (is_parser_class) {
	    CSharpCCParserInternals.setinsertionpoint(GetToken(0), 2);
	  }
	  typeNesting--;
	}
}

void InheritList() :{
}
{
	":" ClassOrInterfaceType()
		( "," ClassOrInterfaceType() )*
}

void EnumDeclaration(int modifiers):
{}
{
  "enum" <IDENTIFIER>
  [ InheritList() ]
  EnumBody()
}

void EnumBody():
{}
{
   "{"
   [ EnumConstant() ( LOOKAHEAD(2) "," EnumConstant() )* ]
   "}"
}

void EnumConstant():
{}
{
	<IDENTIFIER> [ Arguments(null) ]
}

void TypeParameters():
{}
{
   "<" TypeParameter() ( "," TypeParameter() )* ">"
}

void TypeParameter():
{}
{
   ["out" | "in"] <IDENTIFIER>
}

void ClassOrInterfaceBody(bool isInterface, IList<Token> tokens):
/*
 * Parsing this fills "tokens" with all tokens of the block
 * excluding the braces at each end.
 */
	{
	  Token first, last;
	  if (tokens == null)
	    tokens = new List<Token>();
	}
{
  "{"
	{
	  first = GetToken(1);
	}
  ( ClassOrInterfaceBodyDeclaration(isInterface) )*
	{
	  last = GetToken(0);
	}
  "}"
	{
	  if (last.next != first) { // i.e., this is not an empty sequence
	    Token t = first;
	    while (true) {
	      tokens.Add(t);
	      if (t == last) break;
	      t = t.next;
	    }
	  }
	}
}

void ClassOrInterfaceBodyDeclaration(bool isInterface):
{
   int modifiers;
}
{
  LOOKAHEAD(2)
  Initializer()
  {
     if (isInterface)
        throw new ParseException("An interface cannot have initializers");
  }
|
  modifiers = Modifiers() // Just get all the modifiers out of the way. If you want to do
              // more checks, pass the modifiers down to the member
  (
      ClassOrInterfaceDeclaration(modifiers, null)
    |
      EnumDeclaration(modifiers)
    |
      LOOKAHEAD( [ TypeParameters() ] <IDENTIFIER> "(" )
      ConstructorDeclaration()
    |
      LOOKAHEAD( Type() ( "[" "]" )* <IDENTIFIER> ( "," | "=" | ";" ) )
      FieldDeclaration(modifiers)
    |
      MethodDeclaration(modifiers)
  )
|
  ";"
}

void FieldDeclaration(int modifiers):
{}
{
  // Modifiers are already matched in the caller
  Type() ( "[" "]" )* VariableDeclarator() ( "," VariableDeclarator() )* ";"
}

void VariableDeclarator():
{}
{
  VariableDeclaratorId() [ "=" VariableInitializer() ]
}

void VariableDeclaratorId():
{}
{
  <IDENTIFIER>
}

void VariableInitializer():
{}
{
  ArrayInitializer()
|
  Expression(null)
}

void ArrayInitializer():
{}
{
  "{" [ VariableInitializer() ( LOOKAHEAD(2) "," VariableInitializer() )* ] [ "," ] "}"
}

void MethodDeclaration(int modifiers):
{}
{
  // Modifiers already matched in the caller!
  [ TypeParameters() ]
  ResultType(null)
  MethodDeclarator()
  ( Block(null) | ";" )
}

void MethodDeclarator():
{}
{
  <IDENTIFIER> FormalParameters(null)
}

void FormalParameters(IList<Token> tokens) :
/*
 * Parsing this fills "tokens" with all tokens of the formal
 * parameters excluding the parentheses at each end.
 */
	{
	  Token first, last;
	  if (tokens == null)
	    tokens = new List<Token>();
	}
{
  "("
	{
	  first = GetToken(1);
	}
  [ FormalParameter() ( "," FormalParameter() )* ]
	{
	  last = GetToken(0);
	}
  ")"
	{
	  if (last.next != first) { // i.e., this is not an empty sequence
	    Token t = first;
	    while (true) {
	      tokens.Add(t);
	      if (t == last) break;
	      t = t.next;
	    }
	  }
	}
}

void FormalParameter():
{}
{
  ["params"] Type() VariableDeclaratorId()
}

void ConstructorDeclaration():
{}
{
  [ TypeParameters() ]
  // Modifiers matched in the caller
  <IDENTIFIER> FormalParameters(null)
  "{"
    [ LOOKAHEAD(ExplicitConstructorInvocation()) ExplicitConstructorInvocation() ]
    ( BlockStatement() )*
  "}"
}

void ExplicitConstructorInvocation():
{}
{
  LOOKAHEAD(":" "this" Arguments(null))
  ":" "this" Arguments(null)
|
  ":" "base" Arguments(null)
}

void Initializer():
{}
{
  [ "static" ] Block(null)
}


/*
 * Type, name and expression syntax follows.
 */

void Type():
{}
{
   LOOKAHEAD(2) ReferenceType()
 |
   PrimitiveType()
}

void ReferenceType():
{}
{
   PrimitiveType() ( LOOKAHEAD(2) "[" "]" )+
  |
   ( ClassOrInterfaceType() ) ( LOOKAHEAD(2) "[" "]" )*
}

void ClassOrInterfaceType():
{}
{
  <IDENTIFIER> [ LOOKAHEAD(2) TypeArguments() ]
  ( LOOKAHEAD(2) "." <IDENTIFIER> [ LOOKAHEAD(2) TypeArguments() ] )*
}

void TypeArguments():
{}
{
   "<" TypeArgument() ( "," TypeArgument() )* ">"
}

void TypeArgument():
{}
{
   ReferenceType()
}


void PrimitiveType():
{}
{
  "bool"
|
  "char"
|
  "byte"
|
  "short"
|
  "int"
|
  "long"
|
  "float"
|
  "double"
}


void ResultType(IList<Token> tokens) :
	{
	  Token first = GetToken(1);
	  if (tokens == null)
	    tokens = new List<Token>();
	}
{
(
  "void"
|
  Type()
)
	{
	  Token last = GetToken(0);
	  Token t = first;
	  while (true) {
	    tokens.Add(t);
	    if (t == last) break;
	    t = t.next;
	  }
	}
}

void Name(IList<Token> tokens) :
/*
 * A lookahead of 2 is required below since "Name" can be followed
 * by a ".*" when used in the context of an "ImportDeclaration".
 */
	{
	  if (tokens == null)
	    tokens = new List<Token>();
	  Token first = GetToken(1);
	}
{
  CSharpIdentifier()
  ( LOOKAHEAD(2) "." CSharpIdentifier()
  )*
	{
	  Token last = GetToken(0);
	  Token t = first;
	  while (true) {
	    tokens.Add(t);
	    if (t == last) break;
	    t = t.next;
	  }
	}
}


void NameList():
{}
{
  Name(null) ( "," Name(null) )*
}


/*
 * Expression syntax follows.
 */

void Expression(IList tokens) :
/*
 * This expansion has been written this way instead of:
 *   Assignment() | ConditionalExpression()
 * for performance reasons.
 * However, it is a weakening of the grammar for it allows the LHS of
 * assignments to be any conditional expression whereas it can only be
 * a primary expression.  Consider adding a semantic predicate to work
 * around this.
 */
	{
	  Token first = GetToken(1);
	  if (tokens == null)
	    tokens = new ArrayList();
	}
{
  ConditionalExpression()
  [
	LOOKAHEAD(2)
    AssignmentOperator() Expression(null)
  ]
	{
	  Token last = GetToken(0);
	  Token t = first;
	  while (true) {
	    tokens.Add(t);
	    if (t == last) break;
	    t = t.next;
	  }
	}
}


void AssignmentOperator():
{}
{
  "=" | "*=" | "/=" | "%=" | "+=" | "-=" | "<<=" | ">>=" | "&=" | "^=" | "|="
}

void ConditionalExpression():
{}
{
  ConditionalOrExpression() [ "?" Expression(null) ":" Expression(null) ]
}

void ConditionalOrExpression():
{}
{
  ConditionalAndExpression() ( "||" ConditionalAndExpression() )*
}

void ConditionalAndExpression():
{}
{
  InclusiveOrExpression() ( "&&" InclusiveOrExpression() )*
}

void InclusiveOrExpression():
{}
{
  ExclusiveOrExpression() ( "|" ExclusiveOrExpression() )*
}

void ExclusiveOrExpression():
{}
{
  AndExpression() ( "^" AndExpression() )*
}

void AndExpression():
{}
{
  EqualityExpression() ( "&" EqualityExpression() )*
}

void EqualityExpression():
{}
{
  IsExpression() ( ( "==" | "!=" ) IsExpression() )*
}

void IsExpression():
{}
{
  RelationalExpression() [ "is" Type() ]
}

void RelationalExpression():
{}
{
  ShiftExpression() ( ( "<" | ">" | "<=" | ">=" ) ShiftExpression() )*
}

void ShiftExpression():
{}
{
  AdditiveExpression() ( ( "<<" | RSIGNEDSHIFT() ) AdditiveExpression() )*
}

void AdditiveExpression():
{}
{
  MultiplicativeExpression() ( ( "+" | "-" ) MultiplicativeExpression() )*
}

void MultiplicativeExpression():
{}
{
  UnaryExpression() ( ( "*" | "/" | "%" ) UnaryExpression() )*
}

void UnaryExpression():
{}
{
  ( "+" | "-" ) UnaryExpression()
|
  PreIncrementExpression()
|
  PreDecrementExpression()
|
  UnaryExpressionNotPlusMinus()
}

void PreIncrementExpression():
{}
{
  "++" PrimaryExpression()
}

void PreDecrementExpression():
{}
{
  "--" PrimaryExpression()
}

void UnaryExpressionNotPlusMinus():
{}
{
  ( "~" | "!" ) UnaryExpression()
|
  LOOKAHEAD( CastLookahead() )
  CastExpression()
|
  PostfixExpression()
}

// This production is to determine lookahead only.  The LOOKAHEAD specifications
// below are not used, but they are there just to indicate that we know about
// this.
void CastLookahead():
{}
{
  LOOKAHEAD(2)
  "(" PrimitiveType()
|
  LOOKAHEAD("(" Type() "[")
  "(" Type() "[" "]"
|
  "(" Type() ")" ( "~" | "!" | "(" | <IDENTIFIER> | "this" | "base" | "new" | Literal() )
}

void PostfixExpression():
{}
{
  PrimaryExpression() [ "++" | "--" ]
}

void CastExpression():
{}
{
  LOOKAHEAD("(" PrimitiveType())
  "(" Type() ")" UnaryExpression()
|
  "(" Type() ")" UnaryExpressionNotPlusMinus()
}

void PrimaryExpression():
{}
{
  PrimaryPrefix() ( LOOKAHEAD(2) PrimarySuffix() )*
}

void MemberSelector():
{}
{
  "." TypeArguments() <IDENTIFIER>
}

void PrimaryPrefix():
{}
{
  Literal()
|
  "this"
|
  "base" "." <IDENTIFIER>
|
  "(" Expression(null) ")"
|
  AllocationExpression()
|
  Name(null)
}

void PrimarySuffix():
{}
{
  LOOKAHEAD(2)
  "." AllocationExpression()
|
  LOOKAHEAD(3)
  MemberSelector()
|
  "[" Expression(null) "]"
|
  "." <IDENTIFIER>
|
  Arguments(null)
}

void Literal():
{}
{
  <INTEGER_LITERAL>
|
  <FLOATING_POINT_LITERAL>
|
  <CHARACTER_LITERAL>
|
  <STRING_LITERAL>
|
  BooleanLiteral()
|
  NullLiteral()
}

int IntegerLiteral() :
{}
{
  <INTEGER_LITERAL>
	{
	  try {
	    return Int32.Parse(token.image);
	  } catch (FormatException e) {
	    throw new InvalidOperationException();
	  }
	}
}

bool BooleanLiteral() :
{}
{
  "true"
	{
	  return true;
	}
|
  "false"
	{
	  return false;
	}
}

String StringLiteral() :
	{
	  Token t;
	}
{
  t=<STRING_LITERAL>
	{
	  return CSharpCCParserInternals.remove_escapes_and_quotes(t, t.image);
	}
}

void NullLiteral() :
{}
{
  "null"
}

void Arguments(IList<Token> tokens) :
/*
 * Parsing this fills "tokens" with all tokens of the arguments
 * excluding the parentheses at each end.
 */
	{
	  Token first, last;
	  if (tokens == null)
	    tokens = new List<Token>();
	}
{
  "("
	{
	  first = GetToken(1);
	}
  [ ArgumentList() ]
	{
	  last = GetToken(0);
	}
  ")"
	{
	  if (last.next != first) { // i.e., this is not an empty sequence
	    Token t = first;
	    while (true) {
	      tokens.Add(t);
	      if (t == last) break;
	      t = t.next;
	    }
	  }
	}
}

void ArgumentList():
{}
{
  Expression(null) ( "," Expression(null) )*
}

void AllocationExpression():
{}
{
  LOOKAHEAD(2)
  "new" PrimitiveType() ArrayDimsAndInits()
|
  "new" ClassOrInterfaceType() [ TypeArguments() ]
    (
      ArrayDimsAndInits()
    |
      Arguments(null) [ ClassOrInterfaceBody(false, null) ]
    )
}

/*
 * The third LOOKAHEAD specification below is to parse to PrimarySuffix
 * if there is an expression between the "[...]".
 */
void ArrayDimsAndInits():
{}
{
  LOOKAHEAD(2)
  ( LOOKAHEAD(2) "[" Expression(null) "]" )+ ( LOOKAHEAD(2) "[" "]" )*
|
  ( "[" "]" )+ ArrayInitializer()
}


/*
 * Statement syntax follows.
 */

void Statement():
{}
{
  LOOKAHEAD(2)
  LabeledStatement()
|
  Block(null)
|
  EmptyStatement()
|
  StatementExpression() ";"
|
  SwitchStatement()
|
  IfStatement()
|
  WhileStatement()
|
  DoStatement()
|
  ForStatement()
|
  BreakStatement()
|
  ContinueStatement()
|
  GoToStatement()
|
  ReturnStatement()
|
  ThrowStatement()
|
 LockStatement()
|
  TryStatement()
}

void LabeledStatement():
{}
{
  <IDENTIFIER> ":" Statement()
}

void Block(IList<Token> tokens) :
/*
 * Parsing this fills "tokens" with all tokens of the block
 * excluding the braces at each end.
 */
	{
	  Token first, last;
	  if (tokens == null)
	    tokens = new List<Token>();
	}
{
  "{"
	{
	  first = GetToken(1);
	}
  ( BlockStatement() )*
	{
	  last = GetToken(0);
	}
  "}"
	{
	  if (last.next != first) { // i.e., this is not an empty sequence
	    Token t = first;
	    while (true) {
	      tokens.Add(t);
	      if (t == last) break;
	      t = t.next;
	    }
	  }
	}
}

void BlockStatement():
{}
{
  LOOKAHEAD( Modifiers() Type() <IDENTIFIER> )
  LocalVariableDeclaration() ";"
|
  Statement()
|
  ClassOrInterfaceDeclaration(0, null)
}

void LocalVariableDeclaration():
{}
{
  Modifiers() Type() VariableDeclarator() ( "," VariableDeclarator() )*
}

void EmptyStatement():
{}
{
  ";"
}

void StatementExpression():
/*
 * The last expansion of this production accepts more than the legal
 * Java expansions for StatementExpression.  This expansion does not
 * use PostfixExpression for performance reasons.
 */
{}
{
  PreIncrementExpression()
|
  PreDecrementExpression()
|
  PrimaryExpression()
  [
    "++"
  |
    "--"
  |
    AssignmentOperator() Expression(null)
  ]
}

void SwitchStatement():
{}
{
  "switch" "(" Expression(null) ")" "{"
    ( SwitchLabel() ( BlockStatement() )* )*
  "}"
}

void SwitchLabel():
{}
{
  "case" Expression(null) ":"
|
  "default" ":"
}

void IfStatement():
/*
 * The disambiguating algorithm of JavaCC automatically binds dangling
 * else's to the innermost if statement.  The LOOKAHEAD specification
 * is to tell JavaCC that we know what we are doing.
 */
{}
{
  "if" "(" Expression(null) ")" Statement() [ LOOKAHEAD(1) "else" Statement() ]
}

void WhileStatement():
{}
{
  "while" "(" Expression(null) ")" Statement()
}

void DoStatement():
{}
{
  "do" Statement() "while" "(" Expression(null) ")" ";"
}

void ForStatement():
{}
{
  "for" "("

  (
      LOOKAHEAD(Type() <IDENTIFIER> ":")
      Type() <IDENTIFIER> ":" Expression(null)
    |
     [ ForInit() ] ";" [ Expression(null) ] ";" [ ForUpdate() ]
  )

  ")" Statement()
}

void ForInit():
{}
{
  LOOKAHEAD( Type() <IDENTIFIER> )
  LocalVariableDeclaration()
|
  StatementExpressionList()
}

void StatementExpressionList():
{}
{
  StatementExpression() ( "," StatementExpression() )*
}

void ForUpdate():
{}
{
  StatementExpressionList()
}

void BreakStatement():
{}
{
  "break" ";"
}

void ContinueStatement():
{}
{
  "continue" ";"
}

void GoToStatement():
{}
{
	"goto" ";"
}

void ReturnStatement() :
	{
	  Token t;
	}
{
  t="return"
	{
	  // Add if statement to prevent subsequent code generated
	  // from being dead code.
	  if (inAction) {
	    t.image = "{if (true) return";
	    jumpPatched = true;
	  }
	}
  [
    Expression(null)
  ]
  t=";"
	{
	  // Add closing brace for above if statement.
	  if (inAction) {
	    t.image = ";}";
	  }
	}
}

void ThrowStatement() :
	{
	  Token t;
	}
{
  t="throw"
	{
	  // Add if statement to prevent subsequent code generated
	  // from being dead code.
	  if (inAction) {
	    t.image = "{if (true) throw";
	    jumpPatched = true;
	  }
	}
  Expression(null)
  t=";"
	{
	  // Add closing brace for above if statement.
	  if (inAction) {
	    t.image = ";}";
	  }
	}
}

void LockStatement():
{}
{
  "lock" "(" Expression(null) ")" Block(null)
}

void TryStatement():
/*
 * Semantic check required here to make sure that at least one
 * finally/catch is present.
 */
{}
{
  "try" Block(null)
  ( "catch" "(" FormalParameter() ")" Block(null) )*
  [ "finally" Block(null) ]
}

/* We use productions to match >> and > so that we can keep the
 * type declaration syntax with generics clean
 */


void RSIGNEDSHIFT():
{}
{
  ( LOOKAHEAD({ GetToken(1).kind == GT &&
                ((Token.GTToken)GetToken(1)).realKind == CSharpCCParserConstants.RSIGNEDSHIFT} )
  ">" ">"
  )
}

/* Annotation syntax follows. */

void Annotation():
{}
{
   LOOKAHEAD( "[" Name(null) "(" ( <IDENTIFIER> "=" | ")" ))
   NormalAnnotation()
 |
   LOOKAHEAD( "[" Name(null) "(" )
   SingleMemberAnnotation()
}

void NormalAnnotation():
{}
{
   "[" Name(null) [ "(" [ MemberValuePairs() ] ")" ] "]"
}

void SingleMemberAnnotation():
{}
{
  "[" Name(null) [ "(" MemberValue() ")" ] "]"
}

void MemberValuePairs():
{}
{
   MemberValuePair() ( "," MemberValuePair() )*
}

void MemberValuePair():
{}
{
    <IDENTIFIER> "=" MemberValue()
}

void MemberValue():
{}
{
   Annotation()
 |
   MemberValueArrayInitializer()
 |
   ConditionalExpression()
}

void  MemberValueArrayInitializer():
{}
{
  "{" MemberValue() ( LOOKAHEAD(2) "," MemberValue() )* [ "," ] "}"
}


void DefaultValue():
{}
{
  "default" MemberValue()
}


/* IDENTIFIERS */

TOKEN :
{
  < IDENTIFIER: <LETTER> (<PART_LETTER>)* >
|
  < #LETTER:
      [  // all chars for which Char.IsIdentifierStart is true
         "$",
         "A"-"Z",
         "_",
         "a"-"z",
         "\u00a2"-"\u00a5",
         "\u00aa",
         "\u00b5",
         "\u00ba",
         "\u00c0"-"\u00d6",
         "\u00d8"-"\u00f6",
         "\u00f8"-"\u021f",
         "\u0222"-"\u0233",
         "\u0250"-"\u02ad",
         "\u02b0"-"\u02b8",
         "\u02bb"-"\u02c1",
         "\u02d0"-"\u02d1",
         "\u02e0"-"\u02e4",
         "\u02ee",
         "\u037a",
         "\u0386",
         "\u0388"-"\u038a",
         "\u038c",
         "\u038e"-"\u03a1",
         "\u03a3"-"\u03ce",
         "\u03d0"-"\u03d7",
         "\u03da"-"\u03f3",
         "\u0400"-"\u0481",
         "\u048c"-"\u04c4",
         "\u04c7"-"\u04c8",
         "\u04cb"-"\u04cc",
         "\u04d0"-"\u04f5",
         "\u04f8"-"\u04f9",
         "\u0531"-"\u0556",
         "\u0559",
         "\u0561"-"\u0587",
         "\u05d0"-"\u05ea",
         "\u05f0"-"\u05f2",
         "\u0621"-"\u063a",
         "\u0640"-"\u064a",
         "\u0671"-"\u06d3",
         "\u06d5",
         "\u06e5"-"\u06e6",
         "\u06fa"-"\u06fc",
         "\u0710",
         "\u0712"-"\u072c",
         "\u0780"-"\u07a5",
         "\u0905"-"\u0939",
         "\u093d",
         "\u0950",
         "\u0958"-"\u0961",
         "\u0985"-"\u098c",
         "\u098f"-"\u0990",
         "\u0993"-"\u09a8",
         "\u09aa"-"\u09b0",
         "\u09b2",
         "\u09b6"-"\u09b9",
         "\u09dc"-"\u09dd",
         "\u09df"-"\u09e1",
         "\u09f0"-"\u09f3",
         "\u0a05"-"\u0a0a",
         "\u0a0f"-"\u0a10",
         "\u0a13"-"\u0a28",
         "\u0a2a"-"\u0a30",
         "\u0a32"-"\u0a33",
         "\u0a35"-"\u0a36",
         "\u0a38"-"\u0a39",
         "\u0a59"-"\u0a5c",
         "\u0a5e",
         "\u0a72"-"\u0a74",
         "\u0a85"-"\u0a8b",
         "\u0a8d",
         "\u0a8f"-"\u0a91",
         "\u0a93"-"\u0aa8",
         "\u0aaa"-"\u0ab0",
         "\u0ab2"-"\u0ab3",
         "\u0ab5"-"\u0ab9",
         "\u0abd",
         "\u0ad0",
         "\u0ae0",
         "\u0b05"-"\u0b0c",
         "\u0b0f"-"\u0b10",
         "\u0b13"-"\u0b28",
         "\u0b2a"-"\u0b30",
         "\u0b32"-"\u0b33",
         "\u0b36"-"\u0b39",
         "\u0b3d",
         "\u0b5c"-"\u0b5d",
         "\u0b5f"-"\u0b61",
         "\u0b85"-"\u0b8a",
         "\u0b8e"-"\u0b90",
         "\u0b92"-"\u0b95",
         "\u0b99"-"\u0b9a",
         "\u0b9c",
         "\u0b9e"-"\u0b9f",
         "\u0ba3"-"\u0ba4",
         "\u0ba8"-"\u0baa",
         "\u0bae"-"\u0bb5",
         "\u0bb7"-"\u0bb9",
         "\u0c05"-"\u0c0c",
         "\u0c0e"-"\u0c10",
         "\u0c12"-"\u0c28",
         "\u0c2a"-"\u0c33",
         "\u0c35"-"\u0c39",
         "\u0c60"-"\u0c61",
         "\u0c85"-"\u0c8c",
         "\u0c8e"-"\u0c90",
         "\u0c92"-"\u0ca8",
         "\u0caa"-"\u0cb3",
         "\u0cb5"-"\u0cb9",
         "\u0cde",
         "\u0ce0"-"\u0ce1",
         "\u0d05"-"\u0d0c",
         "\u0d0e"-"\u0d10",
         "\u0d12"-"\u0d28",
         "\u0d2a"-"\u0d39",
         "\u0d60"-"\u0d61",
         "\u0d85"-"\u0d96",
         "\u0d9a"-"\u0db1",
         "\u0db3"-"\u0dbb",
         "\u0dbd",
         "\u0dc0"-"\u0dc6",
         "\u0e01"-"\u0e30",
         "\u0e32"-"\u0e33",
         "\u0e3f"-"\u0e46",
         "\u0e81"-"\u0e82",
         "\u0e84",
         "\u0e87"-"\u0e88",
         "\u0e8a",
         "\u0e8d",
         "\u0e94"-"\u0e97",
         "\u0e99"-"\u0e9f",
         "\u0ea1"-"\u0ea3",
         "\u0ea5",
         "\u0ea7",
         "\u0eaa"-"\u0eab",
         "\u0ead"-"\u0eb0",
         "\u0eb2"-"\u0eb3",
         "\u0ebd",
         "\u0ec0"-"\u0ec4",
         "\u0ec6",
         "\u0edc"-"\u0edd",
         "\u0f00",
         "\u0f40"-"\u0f47",
         "\u0f49"-"\u0f6a",
         "\u0f88"-"\u0f8b",
         "\u1000"-"\u1021",
         "\u1023"-"\u1027",
         "\u1029"-"\u102a",
         "\u1050"-"\u1055",
         "\u10a0"-"\u10c5",
         "\u10d0"-"\u10f6",
         "\u1100"-"\u1159",
         "\u115f"-"\u11a2",
         "\u11a8"-"\u11f9",
         "\u1200"-"\u1206",
         "\u1208"-"\u1246",
         "\u1248",
         "\u124a"-"\u124d",
         "\u1250"-"\u1256",
         "\u1258",
         "\u125a"-"\u125d",
         "\u1260"-"\u1286",
         "\u1288",
         "\u128a"-"\u128d",
         "\u1290"-"\u12ae",
         "\u12b0",
         "\u12b2"-"\u12b5",
         "\u12b8"-"\u12be",
         "\u12c0",
         "\u12c2"-"\u12c5",
         "\u12c8"-"\u12ce",
         "\u12d0"-"\u12d6",
         "\u12d8"-"\u12ee",
         "\u12f0"-"\u130e",
         "\u1310",
         "\u1312"-"\u1315",
         "\u1318"-"\u131e",
         "\u1320"-"\u1346",
         "\u1348"-"\u135a",
         "\u13a0"-"\u13f4",
         "\u1401"-"\u166c",
         "\u166f"-"\u1676",
         "\u1681"-"\u169a",
         "\u16a0"-"\u16ea",
         "\u1780"-"\u17b3",
         "\u17db",
         "\u1820"-"\u1877",
         "\u1880"-"\u18a8",
         "\u1e00"-"\u1e9b",
         "\u1ea0"-"\u1ef9",
         "\u1f00"-"\u1f15",
         "\u1f18"-"\u1f1d",
         "\u1f20"-"\u1f45",
         "\u1f48"-"\u1f4d",
         "\u1f50"-"\u1f57",
         "\u1f59",
         "\u1f5b",
         "\u1f5d",
         "\u1f5f"-"\u1f7d",
         "\u1f80"-"\u1fb4",
         "\u1fb6"-"\u1fbc",
         "\u1fbe",
         "\u1fc2"-"\u1fc4",
         "\u1fc6"-"\u1fcc",
         "\u1fd0"-"\u1fd3",
         "\u1fd6"-"\u1fdb",
         "\u1fe0"-"\u1fec",
         "\u1ff2"-"\u1ff4",
         "\u1ff6"-"\u1ffc",
         "\u203f"-"\u2040",
         "\u207f",
         "\u20a0"-"\u20af",
         "\u2102",
         "\u2107",
         "\u210a"-"\u2113",
         "\u2115",
         "\u2119"-"\u211d",
         "\u2124",
         "\u2126",
         "\u2128",
         "\u212a"-"\u212d",
         "\u212f"-"\u2131",
         "\u2133"-"\u2139",
         "\u2160"-"\u2183",
         "\u3005"-"\u3007",
         "\u3021"-"\u3029",
         "\u3031"-"\u3035",
         "\u3038"-"\u303a",
         "\u3041"-"\u3094",
         "\u309d"-"\u309e",
         "\u30a1"-"\u30fe",
         "\u3105"-"\u312c",
         "\u3131"-"\u318e",
         "\u31a0"-"\u31b7",
         "\u3400"-"\u4db5",
         "\u4e00"-"\u9fa5",
         "\ua000"-"\ua48c",
         "\uac00"-"\ud7a3",
         "\uf900"-"\ufa2d",
         "\ufb00"-"\ufb06",
         "\ufb13"-"\ufb17",
         "\ufb1d",
         "\ufb1f"-"\ufb28",
         "\ufb2a"-"\ufb36",
         "\ufb38"-"\ufb3c",
         "\ufb3e",
         "\ufb40"-"\ufb41",
         "\ufb43"-"\ufb44",
         "\ufb46"-"\ufbb1",
         "\ufbd3"-"\ufd3d",
         "\ufd50"-"\ufd8f",
         "\ufd92"-"\ufdc7",
         "\ufdf0"-"\ufdfb",
         "\ufe33"-"\ufe34",
         "\ufe4d"-"\ufe4f",
         "\ufe69",
         "\ufe70"-"\ufe72",
         "\ufe74",
         "\ufe76"-"\ufefc",
         "\uff04",
         "\uff21"-"\uff3a",
         "\uff3f",
         "\uff41"-"\uff5a",
         "\uff65"-"\uffbe",
         "\uffc2"-"\uffc7",
         "\uffca"-"\uffcf",
         "\uffd2"-"\uffd7",
         "\uffda"-"\uffdc",
         "\uffe0"-"\uffe1",
         "\uffe5"-"\uffe6"
      ]
  >
|
  < #PART_LETTER:
      [  // all chars for which Char.IsIdentifierPart is true
         "\u0000"-"\u0008",
         "\u000e"-"\u001b",
         "$",
         "0"-"9",
         "A"-"Z",
         "_",
         "a"-"z",
         "\u007f"-"\u009f",
         "\u00a2"-"\u00a5",
         "\u00aa",
         "\u00b5",
         "\u00ba",
         "\u00c0"-"\u00d6",
         "\u00d8"-"\u00f6",
         "\u00f8"-"\u021f",
         "\u0222"-"\u0233",
         "\u0250"-"\u02ad",
         "\u02b0"-"\u02b8",
         "\u02bb"-"\u02c1",
         "\u02d0"-"\u02d1",
         "\u02e0"-"\u02e4",
         "\u02ee",
         "\u0300"-"\u034e",
         "\u0360"-"\u0362",
         "\u037a",
         "\u0386",
         "\u0388"-"\u038a",
         "\u038c",
         "\u038e"-"\u03a1",
         "\u03a3"-"\u03ce",
         "\u03d0"-"\u03d7",
         "\u03da"-"\u03f3",
         "\u0400"-"\u0481",
         "\u0483"-"\u0486",
         "\u048c"-"\u04c4",
         "\u04c7"-"\u04c8",
         "\u04cb"-"\u04cc",
         "\u04d0"-"\u04f5",
         "\u04f8"-"\u04f9",
         "\u0531"-"\u0556",
         "\u0559",
         "\u0561"-"\u0587",
         "\u0591"-"\u05a1",
         "\u05a3"-"\u05b9",
         "\u05bb"-"\u05bd",
         "\u05bf",
         "\u05c1"-"\u05c2",
         "\u05c4",
         "\u05d0"-"\u05ea",
         "\u05f0"-"\u05f2",
         "\u0621"-"\u063a",
         "\u0640"-"\u0655",
         "\u0660"-"\u0669",
         "\u0670"-"\u06d3",
         "\u06d5"-"\u06dc",
         "\u06df"-"\u06e8",
         "\u06ea"-"\u06ed",
         "\u06f0"-"\u06fc",
         "\u070f"-"\u072c",
         "\u0730"-"\u074a",
         "\u0780"-"\u07b0",
         "\u0901"-"\u0903",
         "\u0905"-"\u0939",
         "\u093c"-"\u094d",
         "\u0950"-"\u0954",
         "\u0958"-"\u0963",
         "\u0966"-"\u096f",
         "\u0981"-"\u0983",
         "\u0985"-"\u098c",
         "\u098f"-"\u0990",
         "\u0993"-"\u09a8",
         "\u09aa"-"\u09b0",
         "\u09b2",
         "\u09b6"-"\u09b9",
         "\u09bc",
         "\u09be"-"\u09c4",
         "\u09c7"-"\u09c8",
         "\u09cb"-"\u09cd",
         "\u09d7",
         "\u09dc"-"\u09dd",
         "\u09df"-"\u09e3",
         "\u09e6"-"\u09f3",
         "\u0a02",
         "\u0a05"-"\u0a0a",
         "\u0a0f"-"\u0a10",
         "\u0a13"-"\u0a28",
         "\u0a2a"-"\u0a30",
         "\u0a32"-"\u0a33",
         "\u0a35"-"\u0a36",
         "\u0a38"-"\u0a39",
         "\u0a3c",
         "\u0a3e"-"\u0a42",
         "\u0a47"-"\u0a48",
         "\u0a4b"-"\u0a4d",
         "\u0a59"-"\u0a5c",
         "\u0a5e",
         "\u0a66"-"\u0a74",
         "\u0a81"-"\u0a83",
         "\u0a85"-"\u0a8b",
         "\u0a8d",
         "\u0a8f"-"\u0a91",
         "\u0a93"-"\u0aa8",
         "\u0aaa"-"\u0ab0",
         "\u0ab2"-"\u0ab3",
         "\u0ab5"-"\u0ab9",
         "\u0abc"-"\u0ac5",
         "\u0ac7"-"\u0ac9",
         "\u0acb"-"\u0acd",
         "\u0ad0",
         "\u0ae0",
         "\u0ae6"-"\u0aef",
         "\u0b01"-"\u0b03",
         "\u0b05"-"\u0b0c",
         "\u0b0f"-"\u0b10",
         "\u0b13"-"\u0b28",
         "\u0b2a"-"\u0b30",
         "\u0b32"-"\u0b33",
         "\u0b36"-"\u0b39",
         "\u0b3c"-"\u0b43",
         "\u0b47"-"\u0b48",
         "\u0b4b"-"\u0b4d",
         "\u0b56"-"\u0b57",
         "\u0b5c"-"\u0b5d",
         "\u0b5f"-"\u0b61",
         "\u0b66"-"\u0b6f",
         "\u0b82"-"\u0b83",
         "\u0b85"-"\u0b8a",
         "\u0b8e"-"\u0b90",
         "\u0b92"-"\u0b95",
         "\u0b99"-"\u0b9a",
         "\u0b9c",
         "\u0b9e"-"\u0b9f",
         "\u0ba3"-"\u0ba4",
         "\u0ba8"-"\u0baa",
         "\u0bae"-"\u0bb5",
         "\u0bb7"-"\u0bb9",
         "\u0bbe"-"\u0bc2",
         "\u0bc6"-"\u0bc8",
         "\u0bca"-"\u0bcd",
         "\u0bd7",
         "\u0be7"-"\u0bef",
         "\u0c01"-"\u0c03",
         "\u0c05"-"\u0c0c",
         "\u0c0e"-"\u0c10",
         "\u0c12"-"\u0c28",
         "\u0c2a"-"\u0c33",
         "\u0c35"-"\u0c39",
         "\u0c3e"-"\u0c44",
         "\u0c46"-"\u0c48",
         "\u0c4a"-"\u0c4d",
         "\u0c55"-"\u0c56",
         "\u0c60"-"\u0c61",
         "\u0c66"-"\u0c6f",
         "\u0c82"-"\u0c83",
         "\u0c85"-"\u0c8c",
         "\u0c8e"-"\u0c90",
         "\u0c92"-"\u0ca8",
         "\u0caa"-"\u0cb3",
         "\u0cb5"-"\u0cb9",
         "\u0cbe"-"\u0cc4",
         "\u0cc6"-"\u0cc8",
         "\u0cca"-"\u0ccd",
         "\u0cd5"-"\u0cd6",
         "\u0cde",
         "\u0ce0"-"\u0ce1",
         "\u0ce6"-"\u0cef",
         "\u0d02"-"\u0d03",
         "\u0d05"-"\u0d0c",
         "\u0d0e"-"\u0d10",
         "\u0d12"-"\u0d28",
         "\u0d2a"-"\u0d39",
         "\u0d3e"-"\u0d43",
         "\u0d46"-"\u0d48",
         "\u0d4a"-"\u0d4d",
         "\u0d57",
         "\u0d60"-"\u0d61",
         "\u0d66"-"\u0d6f",
         "\u0d82"-"\u0d83",
         "\u0d85"-"\u0d96",
         "\u0d9a"-"\u0db1",
         "\u0db3"-"\u0dbb",
         "\u0dbd",
         "\u0dc0"-"\u0dc6",
         "\u0dca",
         "\u0dcf"-"\u0dd4",
         "\u0dd6",
         "\u0dd8"-"\u0ddf",
         "\u0df2"-"\u0df3",
         "\u0e01"-"\u0e3a",
         "\u0e3f"-"\u0e4e",
         "\u0e50"-"\u0e59",
         "\u0e81"-"\u0e82",
         "\u0e84",
         "\u0e87"-"\u0e88",
         "\u0e8a",
         "\u0e8d",
         "\u0e94"-"\u0e97",
         "\u0e99"-"\u0e9f",
         "\u0ea1"-"\u0ea3",
         "\u0ea5",
         "\u0ea7",
         "\u0eaa"-"\u0eab",
         "\u0ead"-"\u0eb9",
         "\u0ebb"-"\u0ebd",
         "\u0ec0"-"\u0ec4",
         "\u0ec6",
         "\u0ec8"-"\u0ecd",
         "\u0ed0"-"\u0ed9",
         "\u0edc"-"\u0edd",
         "\u0f00",
         "\u0f18"-"\u0f19",
         "\u0f20"-"\u0f29",
         "\u0f35",
         "\u0f37",
         "\u0f39",
         "\u0f3e"-"\u0f47",
         "\u0f49"-"\u0f6a",
         "\u0f71"-"\u0f84",
         "\u0f86"-"\u0f8b",
         "\u0f90"-"\u0f97",
         "\u0f99"-"\u0fbc",
         "\u0fc6",
         "\u1000"-"\u1021",
         "\u1023"-"\u1027",
         "\u1029"-"\u102a",
         "\u102c"-"\u1032",
         "\u1036"-"\u1039",
         "\u1040"-"\u1049",
         "\u1050"-"\u1059",
         "\u10a0"-"\u10c5",
         "\u10d0"-"\u10f6",
         "\u1100"-"\u1159",
         "\u115f"-"\u11a2",
         "\u11a8"-"\u11f9",
         "\u1200"-"\u1206",
         "\u1208"-"\u1246",
         "\u1248",
         "\u124a"-"\u124d",
         "\u1250"-"\u1256",
         "\u1258",
         "\u125a"-"\u125d",
         "\u1260"-"\u1286",
         "\u1288",
         "\u128a"-"\u128d",
         "\u1290"-"\u12ae",
         "\u12b0",
         "\u12b2"-"\u12b5",
         "\u12b8"-"\u12be",
         "\u12c0",
         "\u12c2"-"\u12c5",
         "\u12c8"-"\u12ce",
         "\u12d0"-"\u12d6",
         "\u12d8"-"\u12ee",
         "\u12f0"-"\u130e",
         "\u1310",
         "\u1312"-"\u1315",
         "\u1318"-"\u131e",
         "\u1320"-"\u1346",
         "\u1348"-"\u135a",
         "\u1369"-"\u1371",
         "\u13a0"-"\u13f4",
         "\u1401"-"\u166c",
         "\u166f"-"\u1676",
         "\u1681"-"\u169a",
         "\u16a0"-"\u16ea",
         "\u1780"-"\u17d3",
         "\u17db",
         "\u17e0"-"\u17e9",
         "\u180b"-"\u180e",
         "\u1810"-"\u1819",
         "\u1820"-"\u1877",
         "\u1880"-"\u18a9",
         "\u1e00"-"\u1e9b",
         "\u1ea0"-"\u1ef9",
         "\u1f00"-"\u1f15",
         "\u1f18"-"\u1f1d",
         "\u1f20"-"\u1f45",
         "\u1f48"-"\u1f4d",
         "\u1f50"-"\u1f57",
         "\u1f59",
         "\u1f5b",
         "\u1f5d",
         "\u1f5f"-"\u1f7d",
         "\u1f80"-"\u1fb4",
         "\u1fb6"-"\u1fbc",
         "\u1fbe",
         "\u1fc2"-"\u1fc4",
         "\u1fc6"-"\u1fcc",
         "\u1fd0"-"\u1fd3",
         "\u1fd6"-"\u1fdb",
         "\u1fe0"-"\u1fec",
         "\u1ff2"-"\u1ff4",
         "\u1ff6"-"\u1ffc",
         "\u200c"-"\u200f",
         "\u202a"-"\u202e",
         "\u203f"-"\u2040",
         "\u206a"-"\u206f",
         "\u207f",
         "\u20a0"-"\u20af",
         "\u20d0"-"\u20dc",
         "\u20e1",
         "\u2102",
         "\u2107",
         "\u210a"-"\u2113",
         "\u2115",
         "\u2119"-"\u211d",
         "\u2124",
         "\u2126",
         "\u2128",
         "\u212a"-"\u212d",
         "\u212f"-"\u2131",
         "\u2133"-"\u2139",
         "\u2160"-"\u2183",
         "\u3005"-"\u3007",
         "\u3021"-"\u302f",
         "\u3031"-"\u3035",
         "\u3038"-"\u303a",
         "\u3041"-"\u3094",
         "\u3099"-"\u309a",
         "\u309d"-"\u309e",
         "\u30a1"-"\u30fe",
         "\u3105"-"\u312c",
         "\u3131"-"\u318e",
         "\u31a0"-"\u31b7",
         "\u3400"-"\u4db5",
         "\u4e00"-"\u9fa5",
         "\ua000"-"\ua48c",
         "\uac00"-"\ud7a3",
         "\uf900"-"\ufa2d",
         "\ufb00"-"\ufb06",
         "\ufb13"-"\ufb17",
         "\ufb1d"-"\ufb28",
         "\ufb2a"-"\ufb36",
         "\ufb38"-"\ufb3c",
         "\ufb3e",
         "\ufb40"-"\ufb41",
         "\ufb43"-"\ufb44",
         "\ufb46"-"\ufbb1",
         "\ufbd3"-"\ufd3d",
         "\ufd50"-"\ufd8f",
         "\ufd92"-"\ufdc7",
         "\ufdf0"-"\ufdfb",
         "\ufe20"-"\ufe23",
         "\ufe33"-"\ufe34",
         "\ufe4d"-"\ufe4f",
         "\ufe69",
         "\ufe70"-"\ufe72",
         "\ufe74",
         "\ufe76"-"\ufefc",
         "\ufeff",
         "\uff04",
         "\uff10"-"\uff19",
         "\uff21"-"\uff3a",
         "\uff3f",
         "\uff41"-"\uff5a",
         "\uff65"-"\uffbe",
         "\uffc2"-"\uffc7",
         "\uffca"-"\uffcf",
         "\uffd2"-"\uffd7",
         "\uffda"-"\uffdc",
         "\uffe0"-"\uffe1",
         "\uffe5"-"\uffe6",
         "\ufff9"-"\ufffb"
      ]
  >
}
