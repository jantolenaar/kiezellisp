#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Dynamic;
	using System.Linq.Expressions;
	using System.Reflection;

	#region Enumerations

	public enum LambdaKind
	{
		Function,
		Method,
		Macro
	}

	#endregion Enumerations

	public class ApplyWrapper : IApply, IDynamicMetaObjectProvider
	{
		#region Fields

		private IApply Proc;

		#endregion Fields

		#region Constructors

		public ApplyWrapper(IApply proc)
		{
			Proc = proc;
		}

		#endregion Constructors

		#region Methods

		object IApply.Apply(object[] args)
		{
			return Runtime.ApplyStar(Proc, args[0]);
		}

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
		{
			return new GenericApplyMetaObject<ApplyWrapper>(parameter, this);
		}

		#endregion Methods
	}

	public class ApplyWrapper2 : IApply, IDynamicMetaObjectProvider
	{
		#region Fields

		private Func<object[], object> Proc;

		#endregion Fields

		#region Constructors

		public ApplyWrapper2(Func<object[], object> proc)
		{
			Proc = proc;
		}

		#endregion Constructors

		#region Methods

		object IApply.Apply(object[] args)
		{
			return Proc(args);
		}

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
		{
			return new GenericApplyMetaObject<ApplyWrapper2>(parameter, this);
		}

		#endregion Methods
	}

	public class CompareApplyWrapper : IApply
	{
		#region Methods

		object IApply.Apply(object[] args)
		{
			return Runtime.Compare(args[0], args[1]);
		}

		#endregion Methods
	}

	public class EqualApplyWrapper : IApply
	{
		#region Methods

		object IApply.Apply(object[] args)
		{
			return Runtime.Equal(args);
		}

		#endregion Methods
	}

	public class GenericApplyMetaObject<T> : DynamicMetaObject
	{
		#region Fields

		public T lambda;

		#endregion Fields

		#region Constructors

		public GenericApplyMetaObject(Expression objParam, T lambda)
			: base(objParam, BindingRestrictions.Empty, lambda)
		{
			this.lambda = lambda;
		}

		#endregion Constructors

		#region Methods

		public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
		{
			// TODO: optimize lambda calls
			MethodInfo method = typeof(IApply).GetMethod("Apply");
			var list = new List<Expression>();
			foreach (var arg in args)
			{
				list.Add(Runtime.ConvertArgument(arg, typeof(object)));
			}
			var callArg = Expression.NewArrayInit(typeof(object), list);
			var expr = Expression.Call(Expression.Convert(this.Expression, typeof(T)), method, callArg);
			var restrictions = BindingRestrictions.GetTypeRestriction(this.Expression, typeof(T));
			return new DynamicMetaObject(Runtime.EnsureObjectResult(expr), restrictions);
		}

		#endregion Methods
	}

	public class IdentityApplyWrapper : IApply
	{
		#region Methods

		object IApply.Apply(object[] args)
		{
			return Runtime.Identity(args);
		}

		#endregion Methods
	}

	public class LambdaApplyMetaObject : DynamicMetaObject
	{
		#region Fields

		public LambdaClosure lambda;

		#endregion Fields

		#region Constructors

		public LambdaApplyMetaObject(Expression objParam, LambdaClosure lambda)
			: base(objParam, BindingRestrictions.Empty, lambda)
		{
			this.lambda = lambda;
		}

		#endregion Constructors

		#region Methods

		public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
		{
			var restrictions = BindingRestrictions.Empty;
			var callArgs = LambdaHelpers.FillDataFrame(lambda.Definition.Signature, args, ref restrictions);
			MethodInfo method = typeof(LambdaClosure).GetMethod("ApplyLambdaFast", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			var expr = Expression.Call(Expression.Convert(this.Expression, typeof(LambdaClosure)), method, callArgs);
			restrictions = BindingRestrictions.GetInstanceRestriction(this.Expression, this.Value).Merge(restrictions);
			return new DynamicMetaObject(Runtime.EnsureObjectResult(expr), restrictions);
		}

		#endregion Methods
	}

	public class LambdaClosure : IDynamicMetaObjectProvider, IApply, ISyntax
	{
		#region Fields

		public LambdaDefinition Definition;
		public Frame Frame;
		public BindingRestrictions GenericRestrictions;
		public object Owner;

		#endregion Fields

		#region Properties

		public MultiMethod Generic
		{
			get
			{
				return Owner as MultiMethod;
			}
		}

		public LambdaKind Kind
		{
			get
			{
				return Definition.Signature.Kind;
			}
		}

		#endregion Properties

		#region Methods

		public object ApplyLambdaBind(Cons lambdaList, object[] args, bool bound, object env, Cons wholeMacroForm)
		{
			var context = Runtime.CurrentThreadContext;
			var saved = context.Frame;
			context.Frame = Frame;
			object result;

			if (!bound)
			{
				args = MakeArgumentFrame(args, env, wholeMacroForm);
			}

			if (Runtime.DebugMode)
			{
				context.EvaluationStack = Runtime.MakeListStar(saved, context.SpecialStack, context.EvaluationStack);
				result = Definition.Proc(lambdaList, Owner ?? this, args);
				context.EvaluationStack = Runtime.Cddr(context.EvaluationStack);
			}
			else
			{
				result = Definition.Proc(lambdaList, Owner ?? this, args);
			}
			context.Frame = saved;

			return result;
		}

		public object ApplyLambdaFast(object[] args)
		{
			// Entrypoint used by compiler after rearranging args.
			return ApplyLambdaBind(null, args, true, null, null);
		}

		public Exception FillDataFrame(LambdaSignature signature, object[] input, object[] output, int offsetOutput, object env, Cons wholeMacroForm)
		{
			var offset = 0;
			var firstKey = -1;
			var usedKeys = 0;
			var haveAll = false;
			var firstArg = 0;

			if (signature.Kind != LambdaKind.Macro && signature.RequiredArgsCount > 0)
			{
				// This does not work for nested parameters.
				var n = signature.RequiredArgsCount;

				if (input.Length < n)
				{
					throw new LispException("Missing required parameters");
				}
				Array.Copy(input, 0, output, offsetOutput, n);
				offsetOutput += n;
				firstArg = n;
				offset = n;
			}

			for (int iArg = firstArg; !haveAll && iArg < signature.Parameters.Count; ++iArg)
			{
				var mod = (iArg < signature.RequiredArgsCount) ? null : signature.ArgModifier;
				var arg = signature.Parameters[iArg];
				object val;

				if (mod == Symbols.Params)
				{
					var buf = new object[input.Length - offset];
					Array.Copy(input, offset, buf, 0, buf.Length);
					val = buf;
					haveAll = true;
				}
				else if (mod == Symbols.Vector)
				{
					var v = new Vector(input.Length - offset);
					for (int i = offset; i < input.Length; ++i)
					{
						v.Add(input[i]);
					}
					val = v;
					haveAll = true;
				}
				else if (mod == Symbols.Rest || mod == Symbols.Body)
				{
					Cons list = null;
					for (int i = input.Length - 1; i >= offset; --i)
					{
						list = new Cons(input[i], list);
					}
					val = list;
					haveAll = true;
				}
				else if (mod == Symbols.Key)
				{
					if (firstKey == -1)
					{
						firstKey = offset;
						for (int i = firstKey; i < input.Length; i += 2)
						{
							if (!Runtime.Keywordp(input[i]) || i + 1 == input.Length)
							{
								throw new LispException("Invalid keyword/value list");
							}
						}
					}

					val = Runtime.MissingValue;

					for (int i = firstKey; i + 1 < input.Length; i += 2)
					{
						if (arg.Sym.Name == ((Symbol)input[i]).Name)
						{
							val = input[i + 1];
							++usedKeys;
							break;
						}
					}
				}
				else if (offset < input.Length)
				{
					val = input[offset];
					++offset;
				}
				else if (mod == Symbols.Optional)
				{
					val = Runtime.MissingValue;
				}
				else
				{
					throw new LispException("Missing required argument: {0}", arg.Sym);
				}

				if (val == Runtime.MissingValue)
				{
					if (arg.InitFormProc != null)
					{
						val = arg.InitFormProc();
					}
					else
					{
						val = null;
					}
				}

				if (arg.NestedParameters != null)
				{
					// required macro parameter
					var nestedInput = Runtime.AsArray((IEnumerable)val);
					FillDataFrame(arg.NestedParameters, nestedInput, output, offsetOutput, env, null);
					offsetOutput += arg.NestedParameters.Names.Count;
				}
				else
				{
					output[offsetOutput++] = val;
				}
			}

			if (signature.WholeArg != null)
			{
				Cons list = wholeMacroForm;
				if (list == null)
				{
					for (int i = input.Length - 1; i >= 0; --i)
					{
						list = new Cons(input[i], list);
					}
				}
				int j = output.Length - 1 - ((signature.EnvArg != null) ? 1 : 0);
				output[j] = list;
				haveAll = true;
			}

			if (signature.EnvArg != null)
			{
				int j = output.Length - 1;
				output[j] = env;
			}

			if (offset < input.Length && !haveAll && firstKey == -1)
			{
				throw new LispException("Too many parameters supplied");
			}

			return null;
		}

		object IApply.Apply(object[] args)
		{
			// Entrypoint when called via funcall or apply or map etc.
			if (Kind == LambdaKind.Macro)
			{
				var form = (Cons)args[0];
				var env = args[1];
				return ApplyLambdaBind(null, Runtime.AsArray(Runtime.Cdr(form)), false, env, form);
				// throw new LispException("Invalid macro call.");
			}
			else if (Definition.Signature.ArgModifier == Symbols.RawParams)
			{
				return ApplyLambdaBind(null, args, true, null, null);
			}
			else
			{
				return ApplyLambdaBind(null, args, false, null, null);
			}
		}

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
		{
			return new LambdaApplyMetaObject(parameter, this);
		}

		Cons ISyntax.GetSyntax(Symbol context)
		{
			if (Definition.Syntax != null)
			{
				return new Cons(Definition.Syntax, null);
			}
			else
			{
				return null;
			}
		}

		public object[] MakeArgumentFrame(object[] input, object env, Cons wholeMacroForm)
		{
			var sig = Definition.Signature;

			if (sig.Kind != LambdaKind.Macro && sig.RequiredArgsCount == input.Length && sig.Names.Count == input.Length && sig.WholeArg == null)
			{
				// fast track if all arguments (no nested parameters) are accounted for.
				return input;
			}

			var output = new object[sig.Names.Count + (sig.WholeArg == null ? 0 : 1) + (sig.EnvArg == null ? 0 : 1)];
			FillDataFrame(sig, input, output, 0, env, wholeMacroForm);
			return output;
		}

		public override string ToString()
		{
			var name = Definition.Name;

			if (Kind == LambdaKind.Macro)
			{
				return String.Format("Macro Name=\"{0}\"", name == null ? "" : name.Name);
			}
			else
			{
				return String.Format("Lambda Name=\"{0}\"", name == null ? "" : name.Name);
			}
		}

		#endregion Methods
	}

	public class LambdaDefinition
	{
		#region Fields

		public Symbol Name;
		public Func<Cons, object, object[], object> Proc;
		public LambdaSignature Signature;
		public Cons Source;
		public Cons Syntax;

		#endregion Fields

		#region Methods

		public static LambdaClosure MakeLambdaClosure(LambdaDefinition def)
		{
			var closure = new LambdaClosure
			{
				Definition = def,
				Frame = Runtime.CurrentThreadContext.Frame
			};

			return closure;
		}

		#endregion Methods
	}

	public class LambdaHelpers
	{
		#region Methods

		public static Expression FillDataFrame(LambdaSignature signature, DynamicMetaObject[] input, ref BindingRestrictions restrictions)
		{
			var elementType = typeof(object);
			var offset = 0;
			var output = new List<Expression>();

			if (signature.ArgModifier == Symbols.RawParams)
			{
				var tail = new List<Expression>();
				for (int i = offset; i < input.Length; ++i)
				{
					tail.Add(Expression.Convert(input[i].Expression, elementType));
				}
				var tailExpr = Expression.NewArrayInit(elementType, tail);
				return tailExpr;
			}

			for (offset = 0; offset < signature.RequiredArgsCount; ++offset)
			{
				if (offset >= input.Length)
				{
					throw new LispException("Missing required parameters");
				}
				output.Add(Expression.Convert(input[offset].Expression, elementType));
			}

			if (offset != signature.Parameters.Count)
			{
				var mod = signature.ArgModifier;

				if (mod == Symbols.Rest || mod == Symbols.Body || mod == Symbols.Params || mod == Symbols.Vector)
				{
					var tail = new List<Expression>();
					for (int i = offset; i < input.Length; ++i)
					{
						tail.Add(Expression.Convert(input[i].Expression, elementType));
					}
					var tailExpr = Expression.NewArrayInit(elementType, tail);
					if (mod == Symbols.Rest || mod == Symbols.Body)
					{
						var conversion = Expression.Call(Runtime.AsListMethod, tailExpr);
						output.Add(conversion);
					}
					else if (mod == Symbols.Params)
					{
						output.Add(tailExpr);
					}
					else if (mod == Symbols.Vector)
					{
						var conversion = Expression.Call(Runtime.AsVectorMethod, tailExpr);
						output.Add(conversion);
					}
				}
				else if (mod == Symbols.Optional)
				{
					for (int i = offset; i < input.Length && i < signature.Parameters.Count; ++i)
					{
						output.Add(Expression.Convert(input[i].Expression, elementType));
					}
					for (int i = input.Length; i < signature.Parameters.Count; ++i)
					{
						var expr = signature.Parameters[i].InitForm ?? Expression.Constant(null);
						output.Add(expr);
					}
					if (input.Length > signature.Parameters.Count)
					{
						throw new LispException("Too many arguments supplied");
					}
				}
				else if (mod == Symbols.Key)
				{
					var firstKey = offset;
					var usedKeys = 0;

					for (int i = firstKey; i < input.Length; i += 2)
					{
						if (!Runtime.Keywordp(input[i].Value) || i + 1 == input.Length)
						{
							throw new LispException("Invalid keyword/value list");
						}
						var keywordRestriction = BindingRestrictions.GetExpressionRestriction(Expression.Equal(input[i].Expression, Expression.Constant(input[i].Value)));
						restrictions = restrictions.Merge(keywordRestriction);
					}

					for (int i = offset; i < signature.Parameters.Count; ++i)
					{
						Expression val = null;

						for (int j = firstKey; j + 1 < input.Length; j += 2)
						{
							if (signature.Parameters[i].Sym.Name == ((Symbol)input[j].Value).Name)
							{
								val = input[j + 1].Expression;
								++usedKeys;
								break;
							}
						}

						if (val == null)
						{
							output.Add(signature.Parameters[i].InitForm ?? Expression.Constant(null));
						}
						else
						{
							output.Add(Expression.Convert(val, elementType));
						}
					}
				}
			}

			if (signature.WholeArg != null)
			{
				var tail = new List<Expression>();
				for (int i = 0; i < input.Length; ++i)
				{
					tail.Add(Expression.Convert(input[i].Expression, elementType));
				}
				var tailExpr = Expression.NewArrayInit(elementType, tail);
				var conversion = Expression.Call(Runtime.AsListMethod, tailExpr);
				output.Add(conversion);
			}

			return Expression.NewArrayInit(elementType, output);
		}

		public static BindingRestrictions GetGenericRestrictions(LambdaClosure method, DynamicMetaObject[] args)
		{
			var methodList = method.Generic.Lambdas;
			var restrictions = BindingRestrictions.Empty;

			//
			// Restrictions for this method
			//

			for (int i = 0; i < method.Definition.Signature.RequiredArgsCount; ++i)
			{
				var par = method.Definition.Signature.Parameters[i];
				if (par.Specializer != null)
				{
					var restr = BindingRestrictions.GetExpressionRestriction(Expression.Call(Runtime.IsInstanceOfMethod, args[i].Expression, Expression.Constant(par.Specializer)));
					restrictions = restrictions.Merge(restr);
				}
			}

			//
			// Additional NOT restrictions for lambdas that come before the method and fully subtype the method.
			//

			foreach (LambdaClosure lambda in methodList)
			{
				if (lambda == method)
				{
					break;
				}

				bool lambdaSubtypesMethod = true;

				for (int i = 0; i < method.Definition.Signature.RequiredArgsCount; ++i)
				{
					var par = method.Definition.Signature.Parameters[i];
					var par2 = lambda.Definition.Signature.Parameters[i];

					if (!Runtime.IsSubtype(par2.Specializer, par.Specializer, false))
					{
						lambdaSubtypesMethod = false;
						break;
					}
				}

				if (!lambdaSubtypesMethod)
				{
					continue;
				}

				Expression tests = null;

				for (int i = 0; i < method.Definition.Signature.RequiredArgsCount; ++i)
				{
					var par = method.Definition.Signature.Parameters[i];
					var par2 = lambda.Definition.Signature.Parameters[i];

					if (Runtime.IsSubtype(par2.Specializer, par.Specializer, true))
					{
						var test = Expression.Not(Expression.Call(Runtime.IsInstanceOfMethod, args[i].Expression,
									   Expression.Constant(par2.Specializer)));
						if (tests == null)
						{
							tests = test;
						}
						else
						{
							tests = Expression.Or(tests, test);
						}
					}
				}

				if (tests != null)
				{
					var restr = BindingRestrictions.GetExpressionRestriction(tests);
					restrictions = restrictions.Merge(restr);
				}
			}

			return restrictions;
		}

		#endregion Methods
	}

	public class StructurallyEqualApplyWrapper : IApply
	{
		#region Methods

		object IApply.Apply(object[] args)
		{
			return Runtime.StructurallyEqual(args[0], args[1]);
		}

		#endregion Methods
	}
}