;;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.


;;;;
;;;; clr.k
;;;;

(in-package "lisp")

;;;
;;; pseudo classes
;;;

(import "Kiezel.Atom")
(import "Kiezel.List")
(import "Kiezel.Number")
(import "Kiezel.Sequence")
(import "Kiezel.Integer")
(import "Kiezel.Keyword")
(import "Kiezel.Rational")

;;;
;;; real classes
;;;

(import "Kiezel.Symbol")
(import "Kiezel.ThreadContext")
(import "Kiezel.Cons")
(import "Kiezel.Vector")
(import "Kiezel.Prototype")
(import "Kiezel.AssertFailedException")
(import "Kiezel.LispException")

(import "Numerics.BigRational")

(import "System.Array")
(import "System.Boolean")
(import "System.Byte")
(import "System.Char")
(import "System.Console")
(import "System.Convert")
(import "System.DateTime")
(import "System.Decimal")
(import "System.Double")
(import "System.Environment")
(import "System.Globalization.CultureInfo")
(import "System.IO.Directory")
(import "System.IO.File")
(import "Kiezel.FileExtensions" :extends-package-name "file")
(import "System.IO.Path")
(import "Kiezel.PathExtensions" :extends-package-name "path")
(import "System.IO.StreamReader")
(import "System.IO.StreamWriter")
(import "System.IO.StringReader")
(import "System.IO.StringWriter")
(import "System.Int32")
(import "System.Int32" :package-name "int")
(import "System.Int64")
;;;(import "System.Math")
(import "System.Numerics.BigInteger")
(import "System.Numerics.Complex")
(import "System.Object")
(import "System.String")
(import "Kiezel.StringExtensions" :extends-package-name "string")
(import "System.Text.RegularExpressions.Regex")
(import "System.Text.RegularExpressions.RegexOptions")
(import "System.StringSplitOptions")
(import "System.Text.Encoding")
(import "System.TimeSpan")

;;;
;;; Interfaces
;;;

(import "System.Collections.IEnumerable")
(import "System.Collections.IEnumerator")
