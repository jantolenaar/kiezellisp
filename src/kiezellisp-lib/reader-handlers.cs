// Copyright (C) Jan Tolenaar. See the file LICENSE for details.
using System.Numerics;
using System.Text.RegularExpressions;

namespace Kiezel
{
    public partial class Runtime
    {
        public static string[] StringPatterns = new string[]
        {
            @"\<\%(.*?)\%\>",
            @"\<\!(.*?)\!\>",
            @"\$\{(.*?)\}",
        };
        public static Regex InterpolationStringPatterns = new Regex("|".Join(StringPatterns), RegexOptions.Singleline);

        public static bool EvalFeatureExpr(object expr)
        {
            if (expr == null)
            {
                return false;
            }
            else if (expr is bool)
            {
                return (bool)expr;
            }
            else if (expr is Symbol)
            {
                return HasFeature(((Symbol)expr).Name);
            }
            else if (expr is Cons)
            {
                var list = (Cons)expr;
                var oper = First(list) as Symbol;
                if (oper != null)
                {
                    if (oper.Name == "and")
                    {
                        return SeqBase.Every(EvalFeatureExpr, list.Cdr);
                    }
                    else if (oper.Name == "or")
                    {
                        return SeqBase.Any(EvalFeatureExpr, list.Cdr);
                    }
                    else if (oper.Name == "not")
                    {
                        return !EvalFeatureExpr(Second((Cons)expr));
                    }
                }
            }

            throw new LispException("Invalid feature expression");
        }

        public static object ParseInterpolateString(string s)
        {
            int pos = 0;
            Vector code = new Vector();

//            if ( s.StartsWith( "~/" ) )
//            {
//                code.Add( FindSymbol( "shell:$home" ) );
//                pos = 1;
//            }

            for (var match = InterpolationStringPatterns.Match(s, pos); match.Success; match = match.NextMatch())
            {
                var left = match.Index;
                var sideEffect = false;
                var script = "";

                if (left != pos)
                {
                    code.Add(s.Substring(pos, left - pos));
                }

                pos = match.Index + match.Length;

                if (match.Groups[1].Success)
                {
                    // <%...%> or <%=...%>
                    script = match.Groups[1].Value.Trim();
                    if (script.StartsWith("="))
                    {
                        script = script.Substring(1);
                        sideEffect = false;
                    }
                    else
                    {
                        sideEffect = true;
                    }
                }
                else if (match.Groups[2].Success)
                {
                    // <!...!> or <!=...!>
                    script = match.Groups[2].Value.Trim();
                    if (script.StartsWith("="))
                    {
                        script = script.Substring(1);
                        sideEffect = false;
                    }
                    else
                    {
                        sideEffect = true;
                    }
                }
                else if (match.Groups[3].Success)
                {
                    // ${...}
                    script = match.Groups[3].Value;
                    sideEffect = false;
                }

                script = script.Trim();

                if (script.Length > 0)
                {
                    if (sideEffect)
                    {
                        if (script[0] != '(')
                        {
                            // must have function call to have side effect
                            script = "(with-output-to-string ($stdout) (" + script + "))";
                        }
                        else
                        {
                            script = "(with-output-to-string ($stdout) " + script + ")";
                        }
                    }
                    var statements = new LispReader(script).ReadAll();

                    if (statements.Count > 1)
                    {
                        code.Add(new Cons(Symbols.Do, Runtime.AsList(statements)));
                    }
                    else
                    {
                        code.AddRange(statements);
                    }
                }
            }

            if (code.Count == 0)
            {
                return s;
            }
            else
            {
                if (pos < s.Length)
                {
                    code.Add(s.Substring(pos, s.Length - pos));
                }

                return new Cons(Symbols.Str, Runtime.AsList(code));
            }
        }

        public static object ReadBlockCommentHandler(LispReader stream, string ch, int arg)
        {
            // Nested comments are allowed.
            stream.ReadBlockComment("#|", "|#");
            return VOID.Value;
        }

        public static object ReadCharacterHandler(LispReader stream, string ch, int arg)
        {
            stream.UnreadChar();
            var name = Runtime.MakeString(stream.Read());
            var chr = Runtime.DecodeCharacterName(name);
            return chr;
        }

        public static object ReadCommaHandler(LispReader stream, char ch)
        {
            var code = stream.PeekChar();

            if (code == '@' || code == '.')
            {
                // Destructive splicing is handled as ordinary splicing
                stream.ReadChar();
                return MakeList(Symbols.UnquoteSplicing, stream.Read());
            }
            else
            {
                return MakeList(Symbols.Unquote, stream.Read());
            }
        }

        public static object ReadComplexNumberHandler(LispReader stream, string ch, int arg)
        {
            if (stream.ReadChar() != '(')
            {
                throw stream.MakeScannerException("Invalid #c expression");
            }
            var nums = stream.ReadDelimitedList(")");
            int count = Runtime.Length(nums);
            double real = count >= 1 ? Runtime.AsDouble(nums.Car) : 0;
            double imag = count >= 2 ? Runtime.AsDouble(nums.Cdr.Car) : 0;
            return new Complex(real, imag);
        }

        public static object ReadExecuteHandler(LispReader stream, string ch, int arg)
        {
            var expr = stream.Read();
            if (stream.Scanning)
            {
                return expr;
            }
            var readEval = Runtime.GetDynamic(Symbols.ReadEval);
            if (readEval == null)
            {
                readEval = false; //stream.loading;
            }
            if (!Runtime.ToBool(readEval))
            {
                throw stream.MakeScannerException("Invalid use of '#.' (prohibited by $read-eval variable)");
            }
            var value = Eval(expr);
            return value;
        }

        public static object ReadExprCommentHandler(LispReader stream, string ch, int arg)
        {
            stream.ReadSuppressed();
            return VOID.Value;
        }

        public static object ReadInfixHandler(LispReader stream, string ch, int arg)
        {
            return stream.ParseInfixExpression();
        }

        public static object ReadLambdaCharacterHandler(LispReader stream, char ch)
        {
            return stream.ParseLambdaCharacter();
        }

        public static object ReadLineCommentHandler(LispReader stream, char ch)
        {
            // ;
            stream.ReadLine();
            return VOID.Value;
        }

        public static object ReadLineCommentHandler2(LispReader stream, string ch, int arg)
        {
            // #!
            stream.ReadLine();
            return VOID.Value;
        }

        public static object ReadListHandler(LispReader stream, char ch)
        {
            return stream.ReadDelimitedList(")");
        }

        public static object ReadMinusExprHandler(LispReader stream, string ch, int arg)
        {
            object test = stream.Read();
            if (stream.Scanning)
            {
                //scanning
                return stream.Read();
            }
            bool haveFeatures = EvalFeatureExpr(test);
            if (haveFeatures)
            {
                stream.ReadSuppressed();
                return VOID.Value;
            }
            else
            {
                return stream.Read();
            }
        }

        public static object ReadNumberHandler(LispReader stream, string ch, int arg)
        {
            var token = stream.ReadToken();

            switch (ch)
            {
                case "r":
                {
                    return Number.ParseNumberBase(token, arg);
                }
                case "o":
                {
                    return Number.ParseNumberBase(token, 8);
                }
                case "b":
                {
                    return Number.ParseNumberBase(token, 2);
                }
                case "x":
                {
                    return Number.ParseNumberBase(token, 16);
                }
                default:
                {
                    // not reached
                    return null;
                }
            }
        }

        public static object ReadPlusExprHandler(LispReader stream, string ch, int arg)
        {
            object test = stream.Read();
            if (stream.Scanning)
            {
                //scanning
                return stream.Read();
            }
            bool haveFeatures = EvalFeatureExpr(test);
            if (!haveFeatures)
            {
                stream.ReadSuppressed();
                return VOID.Value;
            }
            else
            {
                return stream.Read();
            }
        }

        public enum BranchMode
        {
            False = 0,
            True = 1,
            Eval = 2
        }

        public static object ReadIfExprHandler(LispReader stream, string ch, int arg)
        {
            if (stream.Scanning)
            {
                //scanning
                return stream.Read();
            }

            Vector branch = null;
            var term = ReadBranch(stream, true, BranchMode.Eval, ref branch);
            while (term == Symbols.HashElif)
            {
                var haveBranch = branch != null;
                term = ReadBranch(stream, true, haveBranch ? BranchMode.False : BranchMode.Eval, ref branch);
            }
            if (term == Symbols.HashElse)
            {
                var haveBranch = branch != null;
                term = ReadBranch(stream, false, haveBranch ? BranchMode.False : BranchMode.True, ref branch);
            }
            if (term != Symbols.HashEndif)
            {
                throw stream.MakeScannerException("EOF: Missing #endif");
            }

            stream.InsertForms(branch);
            return VOID.Value;
            // Returns the first element of the branch eventually
        }

        public static Symbol ReadBranch(LispReader stream, bool hasTest, BranchMode mode, ref Vector branch)
        {
            var haveFeatures = false;

            if (hasTest)
            {
                var test = stream.Read();
                switch (mode)
                {
                    case BranchMode.False:
                    {
                        haveFeatures = false;
                        break;
                    }
                    case BranchMode.True:
                    {
                        haveFeatures = true;
                        break;
                    }
                    case BranchMode.Eval:
                    default:
                    {
                        haveFeatures = EvalFeatureExpr(test);
                        break;
                    }
                }
            }
            else
            {
                haveFeatures = mode == BranchMode.True;
            }

            if (haveFeatures)
            {
                branch = new Vector();
            }

            while (true)
            {
                var obj = haveFeatures ? stream.Read() : stream.ReadSuppressed();

                if (obj == Symbols.HashElif || obj == Symbols.HashElse || obj == Symbols.HashEndif)
                {
                    return (Symbol)obj;
                }
                else if (haveFeatures)
                {
                    branch.Add(obj);
                }
            }

        }

        public static object ReadElifExprHandler(LispReader stream, string ch, int arg)
        {
            return Symbols.HashElif;
        }

        public static object ReadElseExprHandler(LispReader stream, string ch, int arg)
        {
            return Symbols.HashElse;
        }

        public static object ReadEndifExprHandler(LispReader stream, string ch, int arg)
        {
            return Symbols.HashEndif;
        }

        public static object ReadPrototypeHandler(LispReader stream, char ch)
        {
            var list = stream.ReadDelimitedList("}");
            if (stream.Scanning)
            {
                //scanning
                return list;
            }
            var obj = new Prototype(Runtime.AsArray(list));
            return obj;
        }

        public static object ReadQuasiQuoteHandler(LispReader stream, char ch)
        {
            var exp1 = stream.Read();
            var exp2 = QuasiQuoteExpandRest(exp1);
            return exp2;
        }

        public static object ReadQuoteHandler(LispReader stream, char ch)
        {
            return MakeList(Symbols.Quote, stream.Read());
        }

        public static object ReadRegexHandler(LispReader stream, string ch, int arg)
        {
            var rx = stream.ParseRegexString(ch[0]);
            return rx;
        }

        public static object ReadShortLambdaExpressionHandler(LispReader stream, string ch, int arg)
        {
            return stream.ParseShortLambdaExpression(")");
        }

        public static object ReadSpecialStringHandler(LispReader stream, string ch, int arg)
        {
            var str = stream.ParseSpecialString();
            return str;
        }

        public static object ReadStringHandler(LispReader stream, char ch)
        {
            // C# string "...."
            return ParseInterpolateString(stream.ParseString());
        }

        public static object ReadStringHandler2(LispReader stream, string ch, int arg)
        {
            // C# string @"..."
            return ParseInterpolateString(stream.ParseMultiLineString());
        }

        public static object ReadUninternedSymbolHandler(LispReader stream, string ch, int arg)
        {
            throw stream.MakeScannerException("Uninterned symbols are not supported.");
        }

        public static object ReadVectorHandler(LispReader stream, char ch)
        {
            var list = stream.ReadDelimitedList("]");
            var obj = new Vector();
            obj.AddRange(list);
            return obj;
        }

        public static object ReadVectorHandler2(LispReader stream, string ch, int arg)
        {
            if (stream.ReadChar() != '(')
            {
                throw stream.MakeScannerException("Invalid #() expression");
            }
            var list = stream.ReadDelimitedList(")");
            var obj = new Vector();
            obj.AddRange(list);
            if (arg == -1)
            {
                // default no action
            }
            else if (arg < obj.Count)
            {
                throw new LispException("Vector {0} contains more than {1} items", ToPrintString(obj), arg);
            }
            else if (arg > obj.Count)
            {
                var filler = obj.Count == 0 ? (object)null : obj[obj.Count - 1];
                while (obj.Count < arg)
                {
                    obj.Add(filler);
                }
            }
            return obj;
        }

        public static object ReadStructHandler(LispReader stream, string ch, int arg)
        {
            if (stream.ReadChar() != '(')
            {
                throw stream.MakeScannerException("Invalid #s() expression");
            }
            var list = stream.ReadDelimitedList(")");
            var obj = new Prototype(AsArray(list));
            return obj;
        }

        public class PrettyPrinting
        {
            public static object ReadBlockCommentHandler(LispReader stream, string ch, int arg)
            {
                // Nested comments are allowed.
                var s = stream.ReadBlockComment("#|", "|#");
                return MakeList(Symbols.PrettyReader, "block-comment", s);
            }

            public static object ReadLineCommentHandler(LispReader stream, char ch)
            {
                // ;
                var s = stream.ReadLine();
                return MakeList(Symbols.PrettyReader, "line-comment", ";" + s);
            }

            public static object ReadLineCommentHandler2(LispReader stream, string ch, int arg)
            {
                // #!
                var s = stream.ReadLine();
                return MakeList(Symbols.PrettyReader, "line-comment", "#!" + s);
            }

            public static object ReadNumberHandler(LispReader stream, string ch, int arg)
            {
                var token = stream.ReadToken();
                return MakeList(Symbols.PrettyReader, "literally", "#" + ch + token);
            }

            public static object ReadPlusMinusExprHandler(LispReader stream, string ch, int arg)
            {
                object test = stream.Read();
                object expr = stream.Read();
                return MakeList(Symbols.PrettyReader, "#" + ch, test, expr);
            }

            public static object ReadSpecialStringHandler(LispReader stream, string ch, int arg)
            {
                return MakeList(Symbols.PrettyReader, "string", stream.ExtractSpecialStringForm());
            }

            public static object ReadStringHandler(LispReader stream, char ch)
            {
                // C# string "...."
                return MakeList(Symbols.PrettyReader, "string", stream.ExtractStringForm());
            }

            public static object ReadStringHandler2(LispReader stream, string ch, int arg)
            {
                // C# string @"..."
                return MakeList(Symbols.PrettyReader, "string", stream.ExtractMultiLineStringForm());
            }

            public static object ReadShortLambdaExpressionHandler(LispReader stream, string ch, int arg)
            {
                return stream.ReadDelimitedList(")");
            }

            public static object ReadStructHandler(LispReader stream, string ch, int arg)
            {
                // #s(...)
                if (stream.ReadChar() != '(')
                {
                    throw stream.MakeScannerException("Invalid #s expression");
                }
                return MakeList(Symbols.PrettyReader, "struct", stream.ReadDelimitedList(")"));
            }
        }
    }
}
