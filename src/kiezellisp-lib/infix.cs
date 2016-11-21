#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
	using System;

	public class Infix
	{
		#region Fields

		public int Index;
		public string InfixStr;
		public string[] Opers = 
			{
				"(",
				")",
				"[",
				"]",
				",",

				"?",
				":",

				"+",
				"-",
				"*",
				"/",
				"%",
				"**",

				"&",
				"|",
				"^",
				"~",
				">>",
				"<<",

				"&&",
				"||",
				"!",

				"==",
				"/=",
				"<",
				">",
				"<=",
				">=",

				"="
			};
		public Vector Tokens;

		#endregion Fields

		#region Constructors

		public Infix(string str)
		{
			InfixStr = str;
			Tokens = Scanner(str);
		}

		#endregion Constructors

		#region Private Properties

		private object NextToken
		{
			get
			{
				if (Index + 1 < Tokens.Count)
				{
					return Tokens[Index + 1];
				}
				else
				{
					return null;
				}
			}
		}

		private object Token
		{
			get
			{
				if (Index < Tokens.Count)
				{
					return Tokens[Index];
				}
				else
				{
					return null;
				}
			}
		}

		#endregion Private Properties

		#region Private Methods

		private void Accept()
		{
			++Index;
		}

		private object Compile()
		{
			return CompileAssignment();
		}

		private object CompileAdd()
		{
			// add-expr: mul-expr
			// add-expr: add-expr add-op mul-expr
			// add-op: '+' | '-'

			return CompileList(CompileMul, "+", "-");
		}

		private object CompileAnd()
		{
			// and-expr: not-expr
			// and-expr: and-expr 'and' not-expr
			return CompileList(CompileBitOr, "&&");
		}

		private Vector CompileArgs(string terminator)
		{
			var args = new Vector();

			while (true)
			{
				args.Add(Compile());
				if (!Runtime.Equal(Token, ","))
				{
					break;
				}
				Accept();
			}

			if (!Runtime.Equal(Token, terminator))
			{
				throw new LispException("Missing {0} in infix expression: {1}", terminator, InfixStr);
			}

			Accept();

			return args;
		}

		private object CompileAssignment()
		{
			var node1 = CompileTernary();
			if (Runtime.Equals(Token, "="))
			{
				Accept();
				var node2 = CompileAssignment();
				return Runtime.MakeList(Symbols.Setf, node1, node2);
			}
			else
			{
				return node1;
			}
		}

		private object CompileAtom()
		{
			var x = Token;
			Accept();
			return x;
		}

		private object CompileBinary(CompileHelper helper, params string[] opers)
		{
			return CompileBinary(helper, helper, opers);
		}

		private object CompileBinary(CompileHelper leftHelper, CompileHelper rightHelper, params string[] opers)
		{
			var node1 = leftHelper();

			var oper = Runtime.Find(Token, opers);

			if (oper != null)
			{
				Accept();
				var node2 = rightHelper();
				return Runtime.MakeList(GetLispOperator(oper), node1, node2);
			}

			return node1;
		}

		private object CompileBitAnd()
		{
			return CompileList(CompileEq, "&");
		}

		private object CompileBitOr()
		{
			return CompileList(CompileBitXor, "|");
		}

		private object CompileBitXor()
		{
			return CompileList(CompileBitAnd, "^");
		}

		private object CompileEq()
		{
			// eq-expr: add-expr
			// eq-expr: add-expr eq-op add-expr
			// eq-op: = '=' | '/=' | '>' | '<' | '<=' | '>='
			return CompileTests(CompileUneq, "==", "/=");
		}

		private object CompileList(CompileHelper helper, params string[] opers)
		{
			var list = new Vector();

			CompileList(list, helper, opers);

			// Return the shortest code.
			if (list.Count == 0)
			{
				return null;
			}
			else if (list.Count == 1)
			{
				return list[0];
			}
			else
			{
				object code = list[0];
				for (var i = 1; i < list.Count; i += 2)
				{
					var lispOper = GetLispOperator(list[i]);
					if (SupportsMany(lispOper) && code is Cons && Runtime.First(code) == lispOper)
					{
						code = Runtime.Append((Cons)code, Runtime.MakeList(list[i + 1]));
					}
					else
					{
						code = Runtime.MakeList(lispOper, code, list[i + 1]);
					}
				}
				return code;
			}
		}

		private void CompileList(Vector list, CompileHelper helper, params string[] opers)
		{
			var node1 = helper();

			if (node1 == null)
			{
				return;
			}

			list.Add(node1);

			if (Tokens.Count != 0)
			{
				var oper = Runtime.Find(Token, opers);

				if (oper != null)
				{
					Accept();
					list.Add(oper);
					CompileList(list, helper, opers);
				}
			}
		}

		private object CompileMul()
		{
			// mul-expr: unary-expr
			// mul-expr: mul-expr mul-op unary-expr
			// mul-op: '*' | '/' | '%'

			return CompileList(CompileUnary, "*", "/", "%");
		}

		private object CompileOr()
		{
			// or-expr: and-expr
			// or-expr: or-expr 'or' and-expr
			return CompileList(CompileAnd, "||");
		}

		private object CompilePostfix()
		{
			// postfix-expr: primary-expr
			// postfix-expr: identifier '(' expr-list ')'
			// postfix-expr: '(' expr ')'
			if (Token is Symbol && Runtime.Equal(NextToken, "("))
			{
				var func = Token;
				Accept();
				Accept();
				var args = CompileArgs(")");
				return Runtime.MakeListStar(func, args);
			}
			else if (Token is Symbol && Runtime.Equal(NextToken, "["))
			{
				var arr = Token;
				Accept();
				Accept();
				var args = CompileArgs("]");
				return Runtime.MakeListStar(Symbols.GetElt, arr, args);
			}
			else if (Runtime.Equal(Token, "("))
			{
				Accept();
				var args = CompileArgs(")");
				if (args.Count == 1)
				{
					return args[0];
				}
				else
				{
					return Runtime.MakeListStar(Symbols.Do, args);
				}
			}
			else
			{
				return CompileAtom();
			}
		}

		private object CompilePow()
		{
			return CompileBinary(CompilePostfix, CompileUnary, "**");
		}

		private object CompileShift()
		{
			return CompileBinary(CompileAdd, "<<", ">>");
		}

		private object CompileTernary()
		{
			var node1 = CompileOr();
			if (Runtime.Equals(Token, "?"))
			{
				Accept();
				var node2 = CompileTernary();
				if (!Runtime.Equals(Token, ":"))
				{
					throw new LispException("Invalid ternary expression in: {0}", InfixStr);
				}
				Accept();
				var node3 = CompileTernary();
				return Runtime.MakeList(Symbols.If, node1, node2, node3);
			}
			else
			{
				return node1;
			}
		}

		private object CompileTests(CompileHelper helper, params string[] opers)
		{
			var list = new Vector();

			CompileList(list, helper, opers);

			// Return the shortest code.
			if (list.Count == 0)
			{
				return null;
			}
			else if (list.Count == 1)
			{
				return list[0];
			}
			else
			{
				// 3, 5, 8 etc: a==b==c becomes (and (= a b) (= b c))
				var list2 = new Vector();

				for (var i = 0; i + 1 < list.Count; i += 2)
				{
					var lispOper = GetLispOperator(list[i + 1]);
					list2.Add(Runtime.MakeList(lispOper, list[i], list[i + 2]));
				}

				if (list2.Count == 1)
				{
					return list2[0];
				}
				else
				{
					return Runtime.MakeListStar(Symbols.And, list2);
				}
			}
		}

		private object CompileUnary()
		{
			// unary-expr: postfix-expr
			// unary-expr: unary-op unary-expr
			// unary-op: '-' '!' '~'

			var str = Token as string;

			if (str != null)
			{
				switch (str)
				{
					case "(":
					{
						// fall thru into compilePostFix.
						break;
					}
					case "-":
					{
						Accept();
						var node = CompileUnary();
						return Runtime.MakeList(Runtime.FindSymbol("-"), node);
					}
					case "~":
					{
						Accept();
						var node = CompileUnary();
						return Runtime.MakeList(Runtime.FindSymbol("bit-not"), node);
					}
					case "!":
					{
						Accept();
						var node = CompileUnary();
						return Runtime.MakeList(Runtime.FindSymbol("not"), node);
					}
					default:
					{
						throw new LispException("Invalid operator {0} in infix expression: {1}", str, InfixStr);
					}
				}
			}

			return CompilePow();
		}

		private object CompileUneq()
		{
			// eq-expr: add-expr
			// eq-expr: add-expr eq-op add-expr
			return CompileTests(CompileShift, "<", ">", ">=", "<=");
		}

		private Symbol GetLispOperator(object oper)
		{
			switch ((string)oper)
			{
				case "==":
				{
					return Symbols.Equality;
				}
				case "||":
				{
					return Symbols.Or;
				}
				case "&&":
				{
					return Symbols.And;
				}
				case "!":
				{
					return Symbols.Not;
				}
				case "|":
				{
					return Symbols.BitOr;
				}
				case "&":
				{
					return Symbols.BitAnd;
				}
				case "~":
				{
					return Symbols.BitNot;
				}
				case "^":
				{
					return Symbols.BitXor;
				}
				case "**":
				{
					return Symbols.Pow;
				}
				case "<<":
				{
					return Symbols.BitShiftLeft;
				}
				case ">>":
				{
					return Symbols.BitShiftRight;
				}

				default:
				{
					return Runtime.FindSymbol((string)oper);
				}
			}
		}

		private bool SupportsMany(Symbol oper)
		{
			switch (oper.Name)
			{
				case "or":
				case "and":
				case "+":
				case "-":
				case "*":
				case "/":
				{
					return true;
				}
				default:
				{
					return false;
				}
			}
		}

		#endregion Private Methods

		#region Public Methods

		public static object CompileString(string str)
		{
			var compiler = new Infix(str);
			return compiler.Compile();
		}

		public Vector Scanner(string infix)
		{
			var v = new Vector();
			var index = 0;
			while (index < infix.Length)
			{
				int pos = index;
				char ch = infix[index];

				if (Char.IsWhiteSpace(ch))
				{
					// Skip white space
					++index;
				}
				else if (Char.IsDigit(ch))
				{
					// Numbers can have decimal point.
					// No commas allowed since this would break function parameter list syntax.
					while (index < infix.Length)
					{
						ch = infix[index];

						// Same as below. If there are letters, this will give runtime error.
						if (!(Char.IsLetterOrDigit(ch) || ch == '.' || ch == '_'))
						{
							break;
						}

						++index;
					}
					var number = infix.Substring(pos, index - pos);
					v.Add(number.ParseNumber());
				}
				else if (Char.IsLetter(ch) || ch == '_')
				{
					// ident

					while (index < infix.Length)
					{
						ch = infix[index];

						if (!(Char.IsLetterOrDigit(ch) || ch == '_'))
						{
							break;
						}

						++index;
					}

					var ident = infix.Substring(pos, index - pos);
					v.Add(Runtime.FindSymbol(ident));
				}
				else if (index + 1 < infix.Length)
				{
					var oper = infix.Substring(index, 2);
					if (Runtime.Find(oper, Opers) == null)
					{
						oper = infix.Substring(index, 1);
						if (Runtime.Find(oper, Opers) == null)
						{
							throw new LispException("Invalid operator {0} in infix expression: {1}", oper, infix);
						}
					}
					index += oper.Length;
					v.Add(oper);
				}
				else
				{
					var oper = infix.Substring(index, 1);
					if (Runtime.Find(oper, Opers) == null)
					{
						throw new LispException("Invalid operator {0} in infix expression: {1}", oper, infix);
					}
					index += oper.Length;
					v.Add(oper);
				}
			}

			return v;
		}

		#endregion Public Methods

		#region Other

		private delegate object CompileHelper();

		#endregion Other
	}
}