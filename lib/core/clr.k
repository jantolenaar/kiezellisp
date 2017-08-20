;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.


;;;;
;;;; clr.k
;;;;

(in-package "lisp")

;;;
;;; pseudo classes
;;;

(import "Kiezel.Atom")
(import "Kiezel.Integer")
(import "Kiezel.KeywordClass" )
(import "Kiezel.List")
(import "Kiezel.Number")
(import "Kiezel.Rational")
(import "Kiezel.Sequence")

;;;
;;; real classes
;;;

(import "Numerics.BigRational")

(import "System.Array")
(import "System.Boolean")
(import "System.Byte")
(import "System.Char")
(import "System.Convert")
(import "System.DateTime")
(import "System.Decimal")
(import "System.Diagnostics.Process")
(import "System.Double")
(import "System.Environment")
(import "System.Globalization.CultureInfo")
(import "System.IO.Directory")
(import "System.IO.File")
(import "System.IO.Path")
(import "System.IO.FileInfo")
(import "System.IO.StreamReader")
(import "System.IO.StreamWriter")
(import "System.IO.StringReader")
(import "System.IO.StringWriter")
(import "System.IO.SearchOption")
(import "System.Int32" :package-name "int")
(import "System.Int32")
(import "System.Int64")
(import "System.Numerics.BigInteger")
(import "System.Numerics.Complex")
(import "System.Object")
(import "System.String")
(import "System.StringSplitOptions")
(import "System.Text.Encoding")
(import "System.Text.RegularExpressions.Regex")
(import "System.Text.RegularExpressions.RegexOptions")
(import "System.TimeSpan")
(import "System.Console")
(import "System.ConsoleColor")
(import "System.ConsoleKey")

(import "Kiezel.AssertFailedException")
(import "Kiezel.Cons")
(import "Kiezel.FileExtensions" :extends-package-name "file")
(import "Kiezel.LispException")
(import "Kiezel.PathExtensions" :extends-package-name "path")
(import "Kiezel.Prototype")
(import "Kiezel.StringExtensions" :extends-package-name "string")
(import "Kiezel.Symbol")
(import "Kiezel.ThreadContext")
(import "Kiezel.Vector")

;;;
;;; Interfaces
;;;

(import "System.Collections.IEnumerable")
(import "System.Collections.IEnumerator")

(import "System.Runtime.CompilerServices.IRuntimeVariables")
