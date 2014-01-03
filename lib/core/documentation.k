
(in-package "lisp")

(import-documentation '(

($debug-mode 
"""
Returns true if Kiezellisp is running in debug mode. See command line options.
"""
)

;------------------------------------------------------------------------------

($enable-warnings 
"""
Enables output by the function `print-warning`.
"""
)

;------------------------------------------------------------------------------

($exception 
"""
Stores the latest exception encountered by the REPL.
"""
)

;------------------------------------------------------------------------------

($features 
"""
A list of keywords showing some properties of Kiezellisp running on this computer.
"""
$features
)

;------------------------------------------------------------------------------

($help-hook 
"""
A function taking a symbol as a parameter. See the file `help.k`.
"""
)

;------------------------------------------------------------------------------

($interactive-mode 
"""
Returns true if Kiezellisp is running with a REPL.
"""
)

;------------------------------------------------------------------------------

($pprint-left-margin
"""
The column number of the current left margin used by `pprint`.
"""
)

;------------------------------------------------------------------------------

($load-path 
"""
Returns a list of folder names. See `set-load-path`.
"""
$load-path
)

;------------------------------------------------------------------------------

($load-print 
"""
When true, the functions `load` and `require` print the value of every top-level
expression.
"""
$load-print
)

;------------------------------------------------------------------------------

($load-verbose 
"""
When true, the functions `load` and `require` print some data to the console.
By default enabled in the interactive REPL.
"""
$load-verbose
)

;------------------------------------------------------------------------------

($modules 
"""
Returns a list of the files that been loaded by `require`.
"""
$modules
)

;------------------------------------------------------------------------------

($package 
"""
Returns the current package. See also `in-package`.
"""
$package
)

;------------------------------------------------------------------------------

($pprint-hook 
"""
A function. Some REPL output functions use this hook to create prettier output.
See also the file `pprint.k` and `system.k`.
"""
$pprint-hook
)

;------------------------------------------------------------------------------

($print-background-color 
"""
The default value of the parameter `background-color` of the function `write`. Initially `null`.
"""
$print-background-color
(do (var $print-background-color :red) (print-line 13))
)

;------------------------------------------------------------------------------

($print-base 
"""
The default value of the parameter `base` of the function `write`. Initially `10`.
"""
$print-base
(do (var $print-base 7) (print-line 13))
)

;------------------------------------------------------------------------------

($print-color 
"""
The default value of the parameter `color` of the function `write`. Initially `null`.
"""
$print-color
(do (var $print-color :red) (print-line 13))
)

;------------------------------------------------------------------------------

($print-compact 
"""
Used by `pprint`.
"""
(EXAMPLE "(write '(.to-upper \"abc\"))")
(EXAMPLE "(do (var $print-compact true) (write '(.to-upper \"abc\")))")
)

;------------------------------------------------------------------------------

($print-escape 
"""
The default value of the parameter `escape` of the function `write`. When true, 
the function `write` prints strings and characters as code instead of data.
"""
$print-escape
)

;------------------------------------------------------------------------------

($print-force 
"""
The default value of the parameter `force` of the function `write`.
The REPL sets this to false. By default: false.
"""
)

;------------------------------------------------------------------------------

($print-max-elements
"""
The function `write` truncates output of a list, vector or array to this number
of elements. When negative, all elements are printed.
"""
$print-max-elements
(series 20)
)

;------------------------------------------------------------------------------

($print-short-symbol-names 
"""
When true, symbol names are never printed with their package names.
"""
$print-short-symbol-names
)

;------------------------------------------------------------------------------

($read-eval 
"""
When false the lisp reader does not support `#.` syntax. When `null`, the lisp
reader allows `#.` syntax only if it is `load`ing or `requiring` a file.
"""
)

;------------------------------------------------------------------------------

($read-suppress 
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

($repl-listener-port 
"""
The TCP port number used by the REPL listener. Defaults to 8080.
"""
$repl-listener-port
)

;------------------------------------------------------------------------------

($pprint-right-margin
"""
The column number of the current right margin used by `pprint`.
"""
)

;------------------------------------------------------------------------------

($quick-import
"""
When true, the function `import` does not create class member symbols until
they are referenced. Has an effect on tab completion. By default: true.
"""
)

;------------------------------------------------------------------------------

($command-line-arguments 
"""
A list of strings. The command line arguments to Kiezellisp that follow
the option `--`.
"""
)

;------------------------------------------------------------------------------

($script-directory 
"""
Returns the directory name of the file that is being `loaded` or `required`.
Initially the startup directory.
"""
$script-directory
)

;------------------------------------------------------------------------------

($script-name 
"""
Returns the file name of the file that is being `loaded` or `required`.
Initially `null`.
"""
$script-name
)

;------------------------------------------------------------------------------

($standout-background-color 
"""
The background color of warning, error and help messages.
"""
$standout-background-color
)

;------------------------------------------------------------------------------

($standout-color 
"""
The foreground color of warning, error and help messages.
"""
$standout-color
)

;------------------------------------------------------------------------------

($stderr 
"""
Error output stream. Default is `true` which means to use whatever `$stdout` uses.
"""
)

;------------------------------------------------------------------------------

($stdin 
"""
A `LispReader` stream created by `make-reader`. Not a `TextReader` stream! 
The functions `read` and `read-all` read lisp expressions.
"""
)

;------------------------------------------------------------------------------

($stdlog 
"""
Log output stream. Default is `true` which means to use whatever `$stdout` uses.
"""
)

;------------------------------------------------------------------------------

($stdout 
"""
Standard output stream, i.e. an instance `TextWriter` or a special value: default 
is `true` which is console output. When `null`, output is discarded. When a string, 
a write will go to the file named by the string with the current date appended. 
After the write the file will be closed.
"""
)

;------------------------------------------------------------------------------

($strict 
"""
When true, the compiler emits warnings about undefined variables.
Whether true or false, an undefined variable may result in a runtime error.
The function `strict` sets the variable `$strict` to true for the currently 
`loading` or `requiring` file.
"""
$strict
)

;------------------------------------------------------------------------------

($tracing 
"""
Enables/disables tracing by the `trace` macro.
"""
$tracing
)

;------------------------------------------------------------------------------

(% (number1 number2)
"""
Returns the remainder of the division of two numbers.
"""
(% 7 3)
(% -7 3)
(% 7 -3)
(% -7 -3)
)

;------------------------------------------------------------------------------

(%and (a1 a2)
"""
Equivalent of the special form `and`.
"""
)

;------------------------------------------------------------------------------

(%attr (target attr)
"""
Equivalent of the special form `attr`.
"""
)

;------------------------------------------------------------------------------

(%elt (target &rest indexes)
"""
Equivalent of the special form `elt`.
"""
)

;------------------------------------------------------------------------------

(%or (a1 a2)
"""
Equivalent of the special form `or`.
"""
)

;------------------------------------------------------------------------------

(%set-attr (target attr value)
"""
Equivalent of the special form `set-attr`.
"""
)

;------------------------------------------------------------------------------

(%set-elt (target &rest indexes-and-value)
"""
Equivalent of the special form `set-elt`.
"""
)

;------------------------------------------------------------------------------

(* (&rest numbers)
"""
Returns the product of the numbers.
"""
(* 3 4 5)
)

;------------------------------------------------------------------------------

(+ (&rest numbers)
"""
Returns the sum of the numbers.
"""
(+ 3 4 5)
)

;------------------------------------------------------------------------------

(- (&rest numbers)
"""
Subtracts subsequent numbers from the first number. If one
number is given, negates the number.
"""
(- 7 3)
(- 7)
)

;------------------------------------------------------------------------------

(. (member)
"""
Returns a function to access a member of an arbitrary object,
e.g. `(. 'to-upper)`. This is usually written as `.to-upper` and 
expanded by the lisp reader to the former expression.
"""
(EXAMPLE "((. 'to-upper) \"hello\")")
(.to-upper "hello")
)

;------------------------------------------------------------------------------

(/ (&rest args)
"""
Divides the first number by the other numbers. If only one number
is given, returns its reciprocal.
"""
(/ 16 10)
(/ 8)
(/ 8 5.0)
)

;------------------------------------------------------------------------------

(/= (&rest args)
"""
Returns true if all arguments are not `equal` to each other.
"""
)

;------------------------------------------------------------------------------

(< (&rest args)
"""
Returns true if `args` is in strictly monotonic increasing order. See `compare`.
"""
)

;------------------------------------------------------------------------------

(<= (&rest args)
"""
Returns true if `args` is in monotonic increasing order. See `compare`.
"""
)

;------------------------------------------------------------------------------

(= (&rest args)
"""
Returns true if all arguments are `equal` to each other.
"""
)

;------------------------------------------------------------------------------

(> (&rest args)
"""
Returns true if `args` is in strictly monotonic decreasing order. See `compare`.
"""
)

;------------------------------------------------------------------------------

(>= (&rest args)
"""
Returns true if `args` is in monotonic increasing order. See `compare`.
"""
)

;------------------------------------------------------------------------------

(adjoin (item seq)
"""
Return `seq` if `seq` contains `item`, otherwise returns a new lazy sequence 
with `item` as the first item.
"""
(adjoin 2 (list 1 2 3))
(adjoin 2 (list 1 3))
(adjoin 2 (vector 1 3))
)

;------------------------------------------------------------------------------

(and (expr*)
"""
Returns the value of the first argument that evaluates as false. Otherwise 
returns the value of the last argument.
"""
(var n 12)
(and (number? n) (< n 13))
)

;------------------------------------------------------------------------------

(any? (predicate seq)
"""
Returns true if some element of `seq` satisfies the function `predicate`.
"""
(any? odd? '(2 3 4))
)

;------------------------------------------------------------------------------

(append (&rest seqs)
"""
Concatenates sequences into a new lazy sequence.
"""
(append '(1 2 3) '(4 5))
)

;------------------------------------------------------------------------------

(apply (func args)
       (func)
"""
The first form calls `func` with an argument list created by the function `list*`.
The second form returns a function with one argument of type sequence that applies the 
argument to `func`.
"""
(apply + 1 2 '(3 4))
(apply + '(1 2 3 4))
(var f (apply +))
(f '(1 2 3 4))
)

;------------------------------------------------------------------------------

(array (&rest items)
"""
Creates a fixed length array of type `object[]` with the given contents.
"""
(array 1 2 3)
:d
)

;------------------------------------------------------------------------------

(array* (&rest items)
"""
Creates a fixed length array of type `object[]` with the given contents. The 
last item must be a sequence. Works like `list*`.
"""
(array* 1 2 '(3 4))
:d
)

;------------------------------------------------------------------------------

(as-array (seq type)
          (seq)
"""
Converts a sequence to an array.
"""
(as-array '(1 2 3))
(as-array '(1 2 3) 'int)
)

;------------------------------------------------------------------------------

(as-big-integer (a)
"""
Converts a number to BigInteger.
"""
)

;------------------------------------------------------------------------------

(as-big-rational (a)
"""
Converts a number to BigRational.
"""
)

;------------------------------------------------------------------------------

(as-complex (a)
"""
Converts a number to Complex.
"""
)

;------------------------------------------------------------------------------

(as-decimal (a)
"""
Converts a number to Decimal.
"""
)

;------------------------------------------------------------------------------

(as-double (a)
"""
Converts a number to Double.
"""
)

;------------------------------------------------------------------------------

(as-int (a)
"""
Converts a number to Int32.
"""
)

;------------------------------------------------------------------------------

(as-int32 (a)
"""
Converts a number to Int32.
"""
)

;------------------------------------------------------------------------------

(as-int64 (a)
"""
Converts a number to Int64.
"""
)

;------------------------------------------------------------------------------

(as-lazy-list (seq)
"""
Converts a sequence to a lazy list.
"""
(var z (as-lazy-list (series 5)))
(first z)
(rest z)
z
(last z)
z
)

;------------------------------------------------------------------------------

(as-list (seq)
"""
Converts a sequence to a list.
"""
(var z (as-list (series 5)))
(first z)
(rest z)
)

;------------------------------------------------------------------------------

(as-long (a)
"""
Converts a number to Int64.
"""
)

;------------------------------------------------------------------------------

(as-prototype (obj)
"""
Adds the contents of a hashtable, the values of static properties of a type
or the instance properties of an object to a prototype hashtable.
"""
(as-prototype date-time.T)
:d
(as-prototype (date-time.now))
:d
)

;------------------------------------------------------------------------------

(as-vector (seq)
"""
Converts a sequence to a vector.
"""
(as-vector (series 5))
(as-vector "abc")
)

;------------------------------------------------------------------------------

(assert (expr)
"""
Throws an AssertFailedException if `expr` evaluates to false. Generates no code 
when `$debug-mode` is false.
"""
(assert (= 1 2))
)

;------------------------------------------------------------------------------

(assert-throws-exception (expr)
"""
Throws an AssertFailedException if `expr` does not throw an exception. Generates no code 
when `$debug-mode` is false.
"""
(assert-throws-exception (= 1 2))
)

;------------------------------------------------------------------------------

(assert-throws-no-exception (expr &key msg)
"""
Throws an AssertFailedException if `expr` throws an exception. Generates no code 
when `$debug-mode` is false.
"""
(assert-throws-no-exception (= xxxxxxxx 1))
)

;------------------------------------------------------------------------------

(atom? (expr)
"""
Returns true if `expr` is `null`, a value type, a number, a string or a symbol.
"""
(atom? 1)
(atom? null)
(atom? '(a))
)

;------------------------------------------------------------------------------

(attr (obj attr-name)
"""
Returns the value of attribute or property `attr-name` of `obj`. The compiler 
generates efficient code if `attr-name` is a compile-time constant, otherwise 
the compiler uses a call to the function `%attr`.
"""
(attr "hello" :length)
(var s "length")
(attr "hello" s)
)

;------------------------------------------------------------------------------

(average (seq &key key)
"""
Computes the numeric average of a sequence of numbers.
"""
(average '(1 2 3 4))
(average '("aap" "noot" "mies") :key .length)
)

;------------------------------------------------------------------------------

(bit-and (&rest numbers)
"""
Returns the bitwise and of the given integer numbers.
"""
(bit-and 12 34)
)

;------------------------------------------------------------------------------

(bit-not (number)
"""
Returns the bitwise not of the given integer number.
"""
(bit-not -2)
)

;------------------------------------------------------------------------------

(bit-or (&rest numbers)
"""
Returns the bitwise or of the given integer numbers.
"""
(bit-or 1 2)
)

;------------------------------------------------------------------------------

(bit-shift-left (number1 number2)
"""
Returns the bitwise left algebraic shift of `number1` by `number2` bits.
"""
(bit-shift-left 3 1)
)

;------------------------------------------------------------------------------

(bit-shift-right (number1 number2)
"""
Returns the bitwise right algebraic shift of `number1` by `number2` bits.
"""
(bit-shift-right 3 1)
(bit-shift-right -1 1)
(bit-shift-right -3 1)
)

;------------------------------------------------------------------------------

(bit-xor (&rest numbers)
"""
Returns the bitwise exclusive or of the given integer numbers.
"""
(bit-xor 5 6)
)

;------------------------------------------------------------------------------

(boolean (a)
"""
Returns `true` if `a` is true, otherwise `false`.
"""
(boolean 0)
(boolean 1)
(boolean null)
(boolean "")
(boolean "aaaa")
(boolean (vector))
(boolean (vector 1 2 3))
)

;------------------------------------------------------------------------------

(break (&optional val)
"""
Breaks out of the current `loop`, `while` or `foreach` loop returning the value 
of `val`.
"""
(loop (break))
(loop (break 123))
)

;------------------------------------------------------------------------------

(break-if (test &optional val)
"""
If `test` is true, breaks out of the current `loop`, `while` or `foreach` loop 
returning the value of `val`.
"""
)

;------------------------------------------------------------------------------

(break-on-ctrl-d ()
"""
Place a call to this function in a loop if you want the ability to stop it
by entering CTRL-D.
"""
)

;------------------------------------------------------------------------------

(caar (list)
"""
Equivalent to `(car (car list))`.
"""
(caar '((1 2) 3 4))
)

;------------------------------------------------------------------------------

(cadr (list)
"""
Equivalent to `(car (cdr list))`.
"""
(cadr '((1 2) 3 4))
)

;------------------------------------------------------------------------------

(call-next-method (&rest args)
"""
Calls the next, i.e. less specialized method of the currently executing generic
function. Returns `null` if there is no next method.
"""
)

;------------------------------------------------------------------------------

(car (list)
"""
Returns the first element of a list.
"""
(car '((1 2) 3 4))
)

;------------------------------------------------------------------------------

(case (key-form clause*)
"""
    clause := '(' test-form form* ')'
    test-form := form | '(' form* ')'

Each `test-form` is a list that starts with an unevaluated non-list item or a list of 
unevaluated items. A `clause` matches the `key-form` if the `key-form` is `equal`
or `=` to one of the items. The value of the `clause` is the value of the implicit
`do` block consisting of the `rest` of the `clauses`. The non-list items `true`
and `otherwise` indicate that the `clause` matches anything.
"""
(var s 'aap)
(case s
    ((not mies) (print-line 1) false)
    (aap (print-line 2) true)
    (otherwise (print-line 3) false))
)

;------------------------------------------------------------------------------

(case-match (key-form clause*)
"""
    clause := '(' test-form form* ')'
    test-form := true | otherwise | pattern

Patterns are described in [pattern-matching](pattern-matching.html).
"""
)

;------------------------------------------------------------------------------

(cdar (list)
"""
Equivalent to `(cdr (car list))`.
"""
(cdar '((1 2) 3 4))
)

;------------------------------------------------------------------------------

(cddr (list)
"""
Equivalent to `(cdr (cdr list))`.
"""
(cddr '((1 2) 3 4))
)

;------------------------------------------------------------------------------

(cdr (list)
"""
Returns the list without the first element.
"""
(cdr '((1 2) 3 4))
)

;------------------------------------------------------------------------------

(char? (expr)
"""
Returns true if `expr` is a character.
"""
(char? #\a)
)

;------------------------------------------------------------------------------

(collect (&rest items)
"""
When used inside a `loop`, `while` or `foreach`, adds each item of `items` to the 
list value returned by the loop.
"""
(loop (for i in (series 1 5)) (collect i (* i i)))
)

;------------------------------------------------------------------------------

(collect-each (items)
"""
When used inside a `loop`, `while` or `foreach`, adds each item of `items` to the 
list value returned by the loop.
"""
(loop (for i in (series 1 5)) (collect-each (series 1 i)))
)

;------------------------------------------------------------------------------

(compare (a1 a2)
"""
Compares two numbers, characters, strings, symbols, dates and timespans.
"""
(compare 3 5)
(compare 3 null)
(compare "een" "twee")
(compare #\a "a")
(compare (date-time.today) (.add-years (date-time.today) 1))
)

;------------------------------------------------------------------------------

(compare-no-case (a1 a2)
"""
Like `compare`, but compares strings and characters case-insensitively.
"""
(compare "aap" "Aap")
(compare-no-case "aap" "Aap")
)

;------------------------------------------------------------------------------

(complex (r i)
"""
Returns a complex number with real part `r` and imaginary part `i`.
"""
(var a (complex 1 1))
(* a a)
)

;------------------------------------------------------------------------------

(complex-from-polar-coordinates (m ph)
"""
Returns a complex number with magnitude `m` and phase `ph`.
"""
(var a (complex-from-polar-coordinates 1 (/ math.PI 4)))
(* a a)
)

;------------------------------------------------------------------------------

(complex? (expr)
"""
Returns true if `expr` has type `System.Numerics.Complex`.
"""
(complex? #c(1 1))
(complex? #c(0 0))
)

;------------------------------------------------------------------------------

(compose (func1 &rest funcs)
"""
Creates a new function that applies the given functions from the right to the left.
"""
(var f (compose dec .length))
(f "hello")
)

;------------------------------------------------------------------------------

(cond (clause*)
"""
    clause := '( test-expr form* ')'
"""
(var a 137)
(cond 
    ((< a 100) (print-line 'smaller-than-100))
    ((< a 200) (print-line 'smaller-than-200))
    (true (print-line 'large)))
)

;------------------------------------------------------------------------------

(cons (item list)
      (item delayed-expression)
"""
Returns a new list with specified first and rest elements.
"""
(defun fibo (a b) (cons a (delay (fibo b (+ a b)))))
(take 15 (fibo 1 1))
)

;------------------------------------------------------------------------------

(cons? (expr)
"""
Returns true if `expr` is a non-empty list.
"""
(cons? 1)
(cons? null)
(cons? '(1 1))
)

;------------------------------------------------------------------------------

(continue ()
"""
Starts the next iteration of the current `loop`, `while` or `foreach` loop.
"""
)

;------------------------------------------------------------------------------

(continue-if (test)
"""
Starts the next iteration of the current `loop`, `while` or `foreach` loop if 
`test` condition is true.
"""
(loop
    (for i in (series 5))
    (print-line i)
    (continue-if (odd? i))
    (print-line i))
(macroexpand-1 '(continue-if (odd? i)))
)

;------------------------------------------------------------------------------

(copy-seq (seq)
"""
Returns a copy of `seq`.
"""
)

;------------------------------------------------------------------------------

(count (item seq &key (test =) (key identity))
"""
Counts the number of elements in `seq` that match `item`.
"""
(count 4 '(1 2 3 4 5))
(count 4 '("aap" "noot" "mies") :key .length)
)

;------------------------------------------------------------------------------

(count-if (predicate seq &key (key identity))
"""
Counts the number of elements in `seq` that match the `predicate` function.
"""
(count-if odd? '(1 2 3 4 5))
(count-if odd? '("aap" "noot" "mies") :key .length)
)

;------------------------------------------------------------------------------

(create-array (type size)
"""
Creates an array.
"""
(var a (create-array 'int64 4))
:d
)

;------------------------------------------------------------------------------

(csv.read-string-to-grid (str)
                         (str options)
"""
TO DO
"""
)

;------------------------------------------------------------------------------

(csv.write-grid-to-string (lines options)
"""
TO DO
"""
)

;------------------------------------------------------------------------------

(csv.write-value-to-string (value culture)
"""
TO DO
"""
)

;------------------------------------------------------------------------------

(date? (expr)
"""
Returns true if `expr` is a DateTime.
"""
(date? (date-time.now))
)

;------------------------------------------------------------------------------

(dec (number)
"""
Decrements `number` by `1`.
"""
(dec 123)
(dec int32.MIN-VALUE)
)

;------------------------------------------------------------------------------

(decf (place)
"""
Decrements the variable or any `setf`-able expression by `1`.
"""
(var a 3)
(decf a)
a
(def b '(1 1 3))
(decf (second b))
b
)

;------------------------------------------------------------------------------

(def (name value \[doc-string\])
"""
Defines a global variable `name` in the current package with initial value `value`.
"""
(def a 123)
)

;------------------------------------------------------------------------------

(deftype (name type)
"""
Gives the name `name` to the prototype or System.Type `type`. The symbol `name`
may now be used by the function `type?` and by the special form `defmethod`.
See also: `set-find-type`.
"""
(deftype a (new :name "mies"))
)

;------------------------------------------------------------------------------

(defconstant (name value \[doc-string\])
"""
Defines a global constant `name` in the current package with initial value `value`.
"""
(defconstant a 123)
)

;------------------------------------------------------------------------------

(defonce (name value &optional doc-string)
"""
Defines the global variable `name` in the current package with value `value` if 
currently undefined. Does nothing if the variable already exists.
"""
)

;------------------------------------------------------------------------------

(defined? (name)
"""
Returns true if `name` is a defined global variable.
"""
(defined? 'blabla)
(defined? '$package)
)

;------------------------------------------------------------------------------

(defmacro (name (arg*) \[doc-string\] form*)
"""
Defines a macro.

See also: [Macros](macros.html)
"""
)

;------------------------------------------------------------------------------

(defmethod (name (arg*) form*)
"""
Defines a method for the multi-method `name`.

See also: [Methods](methods.html)
"""
)

;------------------------------------------------------------------------------

(defmulti (name (arg*) \[doc-string\])
"""
Defines a multi-method (same as generic function in CommonLisp).

See also: [Methods](methods.html)
"""
)

;------------------------------------------------------------------------------

(defpackage (name &rest options)
"""
Defines a package. See also the file `package.k`.

See also: [Packages](packages.html)
"""
)

;------------------------------------------------------------------------------

(defsetf (name func)
"""
Defines `func` as a setter for an expression of the form: `(setf (name ...) value)`.
`func` takes parameters corresponding to `...` and `value`. 
See also the file `setf.k`.
"""
)

;------------------------------------------------------------------------------

(defun (name (arg*) \[doc-string\] form*)
"""
Defines a function.

See also: [Functions](functions.html)
"""
)

;------------------------------------------------------------------------------

(delay (&body forms)
"""
Creates a delayed expression which is not evaluated until the function `force` is called.
Unfortunately, the example processor always calls `force`.
"""
(var z (delay (+ 1 2)))
)

;------------------------------------------------------------------------------

(delete-package (name)
"""
Deletes the package with the given `name`.
"""
)

;------------------------------------------------------------------------------

(describe (obj &optional show-non-public-members)
"""
Shows some information about `obj`. Called by the REPL command `:d`.
"""
(describe "abc")
(describe "abc" true)
)

;------------------------------------------------------------------------------

(distinct (seq &key test key)
"""
Returns a lazy sequence of distinct elements in `seq`.
"""
(distinct '(1 2 3 4 5) :key odd?)
)

;------------------------------------------------------------------------------

(divrem (number)
        (numerator denominator)
"""
Returns a list with the quotient and remainder of the division.
"""
(divrem 6)
(divrem 12 7)
(divrem 0.2)
)

;------------------------------------------------------------------------------

(do (form*)
"""
Evaluates the forms and returns the value of the last form. The body 
of a function is an implicit `do` block. Macros such as `when` and `loop` 
generate `do` blocks.
"""
"""
The `var` special form declares a lexical or dynamic variable.
"""
(do (var x 3) (var y 2) (+ x y))
"""
Use `do` to perform method chaining. The compiler creates the variable `~` 
automatically.
"""
(do "een twee   drie vier vijf zes zeven" (.split ~) (map .length ~) (sum ~))
(sum (map .length (.split "een twee   drie vier vijf zes zeven")))
"""
Every expression in a `do` block get its own numbered `~` variable. `~` refers
to the previous expression. `~2` refers to the second expression, etc.
"""
(do 'a 'b (list ~1 ~2))
)

;------------------------------------------------------------------------------

(each (action seq)
"""
Applies the function `action` to each element of `seq`.
"""
(each print-line "abc")
)

;------------------------------------------------------------------------------

(elt (obj &rest indices)
"""
`Elt` can be used on every object with an indexer method.
"""
(var a '(a b c d e))
(elt a 1)
(elt a 2)
)

;------------------------------------------------------------------------------

(empty? (seq)
"""
Returns true if `seq` is `null` or an empty collection.
"""
(empty? null)
(empty? '(a b c))
(empty? "")
)

;------------------------------------------------------------------------------

(enumerable? (expr)
"""
Returns true if `expr` is IEnumerable.
"""
)

;------------------------------------------------------------------------------

(eq (a b)
"""
Returns true if `a` and `b` are the same object (`object.ReferenceEquals`).
"""
(eq 'foo 'foo)
(eq "foo" "foo")
(eq 1 1)
)

;------------------------------------------------------------------------------

(eql (a b)
"""
Returns true if `a` and `b` are equal objects (`object.Equals`).
"""
(eql 'foo 'foo)
(eql "foo" "foo")
(eql 1 1)
(eql 1 1.0)
)

;------------------------------------------------------------------------------

(equal (a b)
"""
Same as `=`.
Returns true if `a` and `b` are `eql` or equal after numeric type conversion.
"""
(= 1 1.0)
(= 1 #c(1 0))
)

;------------------------------------------------------------------------------

(equal-ci (a b)
"""
Like `equal` but compares strings and characters case-insensitively.
"""
(equal "aap" "Aap")
(equal-ci "aap" "Aap")
)

;------------------------------------------------------------------------------

(eval (expr env)
      (expr)
"""
Evaluates a lisp expression.
"""
(var n 3)
(eval '(+ 3 4))
(eval '(+ 3 n))
)

;------------------------------------------------------------------------------

(even? (number)
"""
Returns true if `number` is even.
"""
(even? 222)
)

;------------------------------------------------------------------------------

(every? (predicate seq)
"""
Returns true if every element in `seq` satisfies the function `predicate`.
"""
(every? even? '(1 2 3 4))
)

;------------------------------------------------------------------------------

(except (seq1 seq2 &key (test =) (key identity))
"""
Returns all elements from `seq1` that are not also in `seq2`.
"""
(except '(1 2 3 4) '(3))
)

;------------------------------------------------------------------------------

(exception? (expr)
"""
Returns true if `expr` has type Exception.
"""
)

;------------------------------------------------------------------------------

(exit ()
      (code)
"""
Terminates the program with exit-code `0` or `code`.
"""
)

;------------------------------------------------------------------------------

(export-documentation (file-name &rest package-names)
"""
Exports the documentation currently attached to the global symbols of the named
packages to a text file.
"""
(export-documentation "d:/temp/csv-doc.k" "csv")
)

;------------------------------------------------------------------------------

(export-symbol (name)
"""
Exports the symbol with the given `name` from the current package. This function
does not create symbols, it merely marks the `name` as being exported. Called
by the macro `defpackage`.
"""
(export-symbol "export-symbol")
)

;------------------------------------------------------------------------------

(fifth (seq)
"""
Returns the fifth element in a `seq`; or `null` if there is no fifth element.
"""
(fifth '(1 2 3 4 5 6))
)

;------------------------------------------------------------------------------

(file.read-all-text (path)
                    (path encoding)
"""
As the original, but (on Windows) with CRLF to LF conversion.
"""
)

;------------------------------------------------------------------------------

(file.write-all-text (path contents)
                     (path contents encoding)
"""
As the original, but (on Windows) with LF to CRLF conversion.
"""
)

;------------------------------------------------------------------------------

(filter (predicate seq &key (key identity))
"""
Returns a lazy list containing the elements of `seq` that satisfy the function
`predicate`.
"""
(filter odd? (series 1 10))
(filter odd? (series 1 10) :key inc)
)

;------------------------------------------------------------------------------

(find (item seq &key (test =) (key identity) (default null))
"""
Returns the first element in `seq` matching the conditions.
"""
(find 4 '(1 2 3 4 5))
(find 99 '(1 2 3 4 5) :default -1)
(find 4 '("aap" "noot" "mies") :key .length)
(find 3 '("aap" "noot" "mies") :key .length :test <)
(find 3 '("aap" "noot" "mies") :key .length :test <=)
)

;------------------------------------------------------------------------------

(find-type (name)
"""
Returns the prototype or System.Type associated with the symbol `name`.
"""
(find-type 'string)
)

;------------------------------------------------------------------------------

(find-if (predicate seq &key (key identity))
"""
Returns the first element in `seq` matching the conditions.
"""
(find-if even? '(1 2 3 4 5))
(find-if even? '("aap" "noot" "mies") :key .length)
)

;------------------------------------------------------------------------------

(find-in-property-list (item seq &key test key default)
"""
Retrieves the value of a property `item` on a property list `seq`. Otherwise like `find`.
"""
(find-in-property-list 'noot '(aap 1 noot 2 mies 3))
(find-in-property-list 'teun (vector 'aap 1 'noot 2 'mies 3))
(find-in-property-list 'teun (vector 'aap 1 'noot 2 'mies 3) :default -1)
)

;------------------------------------------------------------------------------

(find-package (name)
"""
Returns the package corresponding to `name`, or `null` if not found.
"""
(find-package "bla-bla")
(find-package :lisp)
)

;------------------------------------------------------------------------------

(find-source-file (name)
"""
Returns the full path name corresponding to the string `name`. Searches in the
current directory and in the directories of the load path (see also `$load-path`,
`set-load-path`). If `name` has an extension, then `name` is used as given. 
If `name` has no extension, then the following set of filenames is used:

    NAME.k
    NAME.kiezel
    NAME/NAME.k
    NAME/NAME.kiezel
    NAME/main.k
    NAME/main.kiezel
"""
(find-source-file "system")
(find-source-file :ftp)
$load-path
)

;------------------------------------------------------------------------------

(find-subsequence-position (subseq seq &key (test =) (key identity))
"""
Returns the first occurrence of `subseq` within `seq`.
"""
(find-subsequence-position "abc" "pqrabcxyz")
)

;------------------------------------------------------------------------------

(first (seq)
"""
Returns the first element in a `seq`; or `null` if there is no first element.
"""
(first '(1 2 3 4 5 6))
)

;------------------------------------------------------------------------------

(flatten (seq &optional (depth int32.MAX-VALUE))
"""
Returns a lazy sequence with subsequences replaced by their elements up to a
certain depth.
"""
(flatten '(a (b c (d e) f)))
(flatten '(a (b c (d e) f)) 1)
)

;------------------------------------------------------------------------------

(force (expr)
"""
Evaluates a lazy list or delayed expression.
"""
)

;------------------------------------------------------------------------------

(force-append (&rest seqs)
"""
Like `append`, but also calls `force` on the result.
"""
)

;------------------------------------------------------------------------------

(forced? (expr)
"""
Returns false if `expr` is a `delay`-ed expression or a list with a unevaluated `cdr`.
"""
)

;------------------------------------------------------------------------------

(foreach ((sym seq) &body forms)
"""
Executes `forms` repeatedly. Uses `loop`.
"""
(foreach (x (series 1 5)) (collect (+ x x)))
(macroexpand-1 '(foreach (x y) a b c))
(macroexpand '(foreach (x y) a b c))
)

;------------------------------------------------------------------------------

(fourth (seq)
"""
Returns the fourth element in a `seq`; or `null` if there is no fourth element.
"""
(fourth '(1 2 3 4 5 6))
)

;------------------------------------------------------------------------------

(funcall (func &rest args)
"""
Returns the value of calling `func` with arguments `args`. If `func` is a symbol,
the global value of the symbol is used as the function.
"""
(+ 3 4)
(funcall + 3 4)
(funcall '+ 3 4)
)

;------------------------------------------------------------------------------

(function? (expr)
"""
Returns true if `expr` is a function, i.e. a suitable argument to `apply` and
`funcall`.
"""
(function? .length)
(function? +)
(EXAMPLE "(function? (λx (inc x)))")
)

;------------------------------------------------------------------------------

(generator (&body forms)
"""
Returns an `IEnumerable` that returns a value for each `yield` statement in `forms`.
The `yield` statements may be issued by functions called by `forms`. Generators are 
implemented as native threads.
"""
(generator (yield 1) (yield 2) (yield 3))
(as-vector it)
(macroexpand '(generator (yield 1) (yield 2) (yield 3)))
)

;------------------------------------------------------------------------------

(gentemp (prefix)
        ()
"""
Returns a new symbol in the current package. For use by macro writers. Works as 
in Commonlisp. 
The function `gensym` does not exist in Kiezellisp because uninterned
symbols are not debug-friendly.
"""
(gentemp "xyz")
(gentemp)
)

;------------------------------------------------------------------------------

(get-type (name)
"""
Like `find-type`. Throws exception if not found.
"""
)

;------------------------------------------------------------------------------

(get-description (obj)
                 (obj show-non-public)
"""
Returns a prototype object describing the given `object`. Used by the function
`describe`.
"""
(get-description 'when)
)

;------------------------------------------------------------------------------

(get-designated-string (target)
"""
Returns the string representation of a keyword name or a symbol name or string.
"""
(get-designated-string 'aap)
(get-designated-string :aap)
(get-designated-string "aap")
(get-designated-string 123)
)

;------------------------------------------------------------------------------

(get-diagnostics (exception)
"""
Returns a string describing `exception` in the current execution environment. Used by
non-interactive Kiezellisp programs.
"""
)

;------------------------------------------------------------------------------

(get-global-symbols ()
"""
Returns a list with all globally defined symbols.
"""
)

;------------------------------------------------------------------------------

(get-package (name)
"""
Returns the package corresponding to `name`. Throws an exception if not found.
"""
(get-package "bla-bla")
(get-package :lisp)
)

;------------------------------------------------------------------------------

(get-version ()
"""
Returns Kiezellisps version string.
"""
(get-version)
)

;------------------------------------------------------------------------------

(go (tag-symbol)
"""
Jumps around in a `tagbody`. Internal use.
"""
)

;------------------------------------------------------------------------------

(group-by (key seq)
"""
Collects the elements of `seq` that have the same key into subsequences, as
determined by the function `key`. Keys should be numbers, strings or symbols.
"""
(group-by length '("aap" "noot" "mies" "blablabla"))
)

;------------------------------------------------------------------------------

(help (topic)
"""
Prints help on a `topic`. Same the REPL command `?`. Works best if `topic` is a symbol.
"""
(help 'car)
)

;------------------------------------------------------------------------------

(hidden-var (sym \[value\])
"""
Same as `var`, but the variable will not be visible in the REPL debugger. Internal use.
The command line option `--nodebug` changes most `var`s to `hidden-var`s.
"""
)

;------------------------------------------------------------------------------

(identity (a)
"""
Returns its single argument or a list of its multiple arguments.
"""
(identity 1)
(identity)
(identity 1 2 3)
)

;------------------------------------------------------------------------------

(if (test then-form \[else-form\])
"""
If `test` is true, returns the result of `then-form`, else returns the result of 
`else-form` or `null`. Truth is defined by the function `boolean`.
"""
(if (zero? 0) 1 2)
(if (zero? 1) 1 2)
(if (zero? 1) 1)
)

;------------------------------------------------------------------------------

(if-let ((sym test) then-form \[else-form\])
"""
Assigns the result of `test` to the local variable `sym` before doing the *if*
thing. `then-form` and `else-form` may refer to `sym`.
"""
(if-let (x (filter odd? '(1 2 3 4))) (print-line x))
)

;------------------------------------------------------------------------------

(if-match (pattern expr then-form \[else-form\])
"""
If `expr` matches `pattern`, returns  the result of `then-form`, else returns 
the result of `else-form` or `null`.
See also [pattern-matching](pattern-matching.html).
"""
)

;------------------------------------------------------------------------------

(import (type-name &key package-name extends-package-name type-parameters)
"""
If `extends-package-name` is given, adds extension methods from the CLR type 
`type-name` to the existing import package `extends-package-name`. Otherwise, 
imports the public fields, properties, events, methods and constructors of the 
CLR type `type-name` into the package `package-name`, which defaults to the 
rightmost name part of `type-name`. See also: `$quick-import`.
"""
(import "System.String")
(import "Kiezel.StringExtensions" :extends-package-name "string")
string.compare
string.T
string.capitalize
(import "System.Net.WebRequestMethods+Ftp" :package-name :ftp-methods)
(import "System.Collections.Generic.List" :type-parameters '(int))
)

;------------------------------------------------------------------------------

(import-documentation (items)
"""
See file `documentation.k`.
"""
)

;------------------------------------------------------------------------------

(import-symbol (name)
"""
Adds the symbol referenced by the string `name` to the current package. 
`name` must specify a package name and a symbol name,
e.g. `(import-symbol "csv.read-grid-from-string")`
"""
)

;------------------------------------------------------------------------------

(in-package (name)
"""
Sets the current package. The special variable `$package` holds the value of the
current package.
"""
(in-package "user")
(in-package :example)
)

;------------------------------------------------------------------------------

(inc (number)
"""
Returns `number` incremented by 1.
"""
(inc 4)
)

;------------------------------------------------------------------------------

(incf (place)
"""
Increments the value of the `setf`able `place`.
"""
(var x 3)
(incf x)
x
(var a '(1 2 3))
(incf (second a))
a
)

;------------------------------------------------------------------------------

(integer? (expr)
"""
Returns true if `expr` is a integer number, i.e. an Int32, an Int64 or a
BigInteger.
"""
(integer? 12)
(integer? (/ 12 2))
)

;------------------------------------------------------------------------------

(interleave (&rest seqs)
"""
Returns a new list, taking the first items of each `seqs`, then the second, and so on.
Stops when the shortest sequence is exhausted.
"""
(interleave '(1 2 3) '(4 5 6 7))
(interleave '(1 2 3) (repeat '-))
)

;------------------------------------------------------------------------------

(interpolate-string (str)
"""
Evaluates lisp code embedded in `str` and returns a new string. String literals 
in source code are automatically interpolated. And that is why this example is so 
difficult to type in.
"""
(string (string (string (string "aap``(+ "))) "3 4)``noot")
(interpolate-string it)
)

;------------------------------------------------------------------------------

(intersect (seq1 seq2 &key (test =) (key identity))
"""
Returns the intersection of two sequences.
"""
(intersect '(1 2 3 4) '(2 4 6))
)

;------------------------------------------------------------------------------

(keyword? (expr)
"""
Returns true if `expr` is a keyword.
"""
(keyword? 123)
(keyword? 'aap)
(keyword? :aap)
)

;------------------------------------------------------------------------------

(lambda ((args*) forms*)
"""
See also: [Functions](functions.html).
"""
)

;------------------------------------------------------------------------------

(lambda? (expr)
"""
Returns true if `expr` is a lambda.
"""
(defun f (x) (not x))
(lambda? f)
(lambda? 'f)
(EXAMPLE "(lambda? (λx (not x)))")
)

;------------------------------------------------------------------------------

(last (seq)
      (count seq)
"""
Returns the last 1 or `count` cons cells of a list.
"""
(last '(1 2 3))
(last null)
(last 2 '(1 2 3))
(last 5 '(1 2 3))
)

;------------------------------------------------------------------------------

(length (seq)
"""
Returns the length of a sequence or string.
"""
(length "abc")
(length (range 10))
)

;------------------------------------------------------------------------------

(list (&rest items)
"""
Returns a list containing the given items.
"""
(list 1 2 3)
(list 'a 'b 'c)
'(a b c)
)

;------------------------------------------------------------------------------

(list* (&rest items)
"""
Returns a list containing the given items. The last item is a list that becomes 
the tail of the new list.
"""
(list* 1 2 '(3 4))
(list 1 2 '(3 4))
)

;------------------------------------------------------------------------------

(list-all-classes ()
"""
Returns a list with the names of all classes.
"""
(list-all-classes)
)

;------------------------------------------------------------------------------

(list-all-packages ()
"""
Returns a list with the names of all packages.
"""
(list-all-packages)
)

;------------------------------------------------------------------------------

(list-exported-symbols (package-name)
"""
Returns a list with the symbols exported by package `package-name`.
"""
(list-exported-symbols "csv")
(list-exported-symbols :int)
)

;------------------------------------------------------------------------------

(list? (expr)
"""
Returns true if `expr` is a list or `null`.
"""
(list? '(1 2 3))
(list? (range 10))
(list? null)
)

;------------------------------------------------------------------------------

(literal? (expr)
"""
Returns true if `expr` is `null`, a value type, a number or a string.
"""
(literal? 123)
(literal? "abc")
(literal? 'aap)
)

;------------------------------------------------------------------------------

(load (file &key (verbose $load-verbose) (print $load-print))
"""
Loads, parses and evaluates the contents of file `file`. If `verbose` is true,
the name and true location of `file` are printed to standard log. If `print`
is true, the result of every top-level statement is printed to standard log.
See also: `find-source-file`, `require`, `run`.
"""
(load "ftp" :print true)
(load :ftp)
)

;------------------------------------------------------------------------------

(loop (&body forms)
"""
Creates a loop. See also the file `loop.k` and [Loops](loops.html).
"""
)

;------------------------------------------------------------------------------

(macro? (expr)
"""
Returns true if `expr` is a macro.
"""
(macro? when)
(macro? if)
)

;------------------------------------------------------------------------------

(macroexpand (expr)
"""
Returns the macro expansion of `expr` by repeatedly calling `macroexpand-1`.
"""
(macroexpand '(setf (car x) 123))
(macroexpand '(while a b))
)

;------------------------------------------------------------------------------

(macroexpand-1 (expr)
"""
Returns the macro expansion of `expr`.
"""
(macroexpand-1 '(setf (car x) 123))
(macroexpand-1 '(while a b))
)

;------------------------------------------------------------------------------

(make-environment ()
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(make-package (package-name)
"""
Returns an existing package or creates a new one. Called by the macro `defpackage`.
"""
(make-package "csv")
)

;------------------------------------------------------------------------------

(make-reader (text)
"""
Creates a reader stream for the string `text`. The value of `$stdin` is a reader 
stream. See also `read` and `read-all`.
"""
)

;------------------------------------------------------------------------------

(make-symbol (key package-name)
             (key package)
             (key)
"""
Returns an existing symbol or creates a new one. For use by macros.
"""
(var a 'test1)
(var b (make-symbol (string "test" 1) $package))
(eq a b)
)

;------------------------------------------------------------------------------

(map (func seq)
"""
Returns a lazy sequence where each element of `seq` is transformed by the
function `func`.
"""
(map length (vector "aap" "noot" "mies"))
(EXAMPLE #q{(map (λx (odd? (length x))) (vector "aap" "noot" "mies"))})
)

;------------------------------------------------------------------------------

(mapcat (func seq)
"""
Returns a lazy sequence where each element of `seq` is transformed by the
function `func` and all `func` outputs are concatenated.
"""
(mapcat series (series 5))
)

;------------------------------------------------------------------------------

(math.conjugate (a)
"""
Returns the conjugate of a number.
"""
(math.conjugate #c(3 4))
)

;------------------------------------------------------------------------------

(max (seq &key (key identity))
"""
Returns the largest number in a sequence.
"""
(max '(1 2 4 1 3))
(max (vector "aap" "noot" "mies") :key length)
)

;------------------------------------------------------------------------------

(merge (seq1 seq2 &key (test <) (key identity))
"""
Merges two ordered sequences into one. Used by the function `sort`.
"""
(merge '(1 3 5) '(2 2 4 6))
)

;------------------------------------------------------------------------------

(merge-with-outer-do (form*)
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(min (seq &key key)
"""
Returns the smallest number in a sequence.
"""
(min '(1 2 4 1 3))
(min (vector "aap" "noot" "mies") :key length)
)

;------------------------------------------------------------------------------

(mismatch (seq1 seq2 &key (test =) (key identity))
"""
Returns the position of the first mismatch of two sequences. Returns `null` if
the sequences match.
"""
(mismatch '(1 2 3 4) '(1 2 3 4))
(mismatch '(1 2 3 4) '(1 2 x 4))
)

;------------------------------------------------------------------------------

(multi-method? (expr)
"""
Returns true if `expr` is a multi-method.
"""
(defmulti f (a b))
(multi-method? f)
)

;------------------------------------------------------------------------------

(natural-compare (x y &key ignore-white-space compact-whitespace punctuation-is-whitespace culture)
"""
Compares the strings representations of objects. Defaults are: false, true, true,
InvariantCulture.
"""
(compare "abc11" "abc2")
(natural-compare "abc11" "abc2")
)

;------------------------------------------------------------------------------

(natural-less (x y)
"""
Compares the string representations of objects.
"""
(natural-less "abc11" "abc2")
)

;------------------------------------------------------------------------------

(new (&rest args)
     (parent &rest args)
"""
Equivalent to `prototype.new`.
Creates a new prototype object. A single argument can be a parent prototype. Two 
consecutive arguments can be a name/value pair used to initialize
the new object. Names can be given as strings, symbols and keywords.
"""
(def thing (new :color "black" :model "0"))
(new thing :model "12")
)

;------------------------------------------------------------------------------

(not (expr)
"""
Returns the logical complement of `expr`. See also `boolean`.
"""
(not null)
(not 1)
)

;------------------------------------------------------------------------------

(not-any? (predicate seq)
"""
Returns true if no elements of `seq` satisfy the function `predicate`.
"""
(not-any? odd? '(1 2 3 4))
(not-any? odd? '(2 4))
)

;------------------------------------------------------------------------------

(not-every? (predicate seq)
"""
Returns true if at least one element of `seq` fails the function `predicate`.
"""
(not-every? odd? '(1 2 3 4))
(not-every? odd? '(2 4))
(not-every? odd? '(1 3))
)

;------------------------------------------------------------------------------

(nth (pos seq)
"""
Returns the element at position `pos` in `seq`, or `null` if there is no 
element at that position.
"""
(nth 0 '(a b c))
(nth 2 '(a b c))
(nth 9 '(a b c))
(nth -1 '(a b c))
)

;------------------------------------------------------------------------------

(null? (expr)
"""
Returns true if `expr` equals `null`.
"""
(null? null)
(null? '(3 4))
)

;------------------------------------------------------------------------------

(number? (expr)
"""
Returns true if `expr` is a number.
"""
(number? 123)
(number? #c(1 2))
(number? "123")
)

;------------------------------------------------------------------------------

(odd? (number)
"""
Returns true if `number` is odd. `number` must be an integer.
"""
(odd? 3)
(odd? 4)
(odd? 3.4)
)

;------------------------------------------------------------------------------

(or (expr*)
"""
Returns the value of the first argument that evaluates as true. Otherwise 
returns the value of the last argument.
"""
(or "" "default")
(or "abc" "default")
)

;------------------------------------------------------------------------------

(parallel-each (action seq)
"""
Applies the function `action` to each element of `seq`. Uses the Task Parallel 
Library.
"""
(parallel-each print (series 15))
)

;------------------------------------------------------------------------------

(parallel-foreach ((sym seq) &body forms)
"""
Applies the `forms` to each element of `seq` in parallel. Uses the Task Parallel 
Library.
"""
(parallel-foreach (x (series 15)) (print x))
)

;------------------------------------------------------------------------------

(parallel-list (&rest forms)
"""
Returns a list like the function `list`. Uses the Task Parallel Library. The example
is not very instructive.
"""
(parallel-list 1 2 3 4)
)

;------------------------------------------------------------------------------

(parallel-map (func seq)
"""
Returns a lazy sequence where each element of `seq` is transformed by the
function `func`. Uses the Task Parallel Library.
"""
(parallel-map length (vector "aap" "noot" "mies"))
(EXAMPLE "(parallel-map (λx (odd? (length x))) (vector \"aap\" \"noot\" \"mies\"))")
)

;------------------------------------------------------------------------------

(partially-apply (func &rest head-args)
"""
Creates a new function by partially applying arguments to the given function.
"""
(def f (partially-apply string "```"))
(map f '(aap noot mies))
)

;------------------------------------------------------------------------------

(partition (size step padding seq)
           (size step seq)
           (size seq)
"""
Divides `seq` into subsequences of length `size`. Default <step> equals `size`.
In order to get the right `size`, the last subsequence may borrow elements
from the sequence `padding`.
"""
(partition 3 (series 10))
(partition 3 2 (series 10))
(partition 3 4 (series 10))
(partition 3 3 '(a b) (series 10))
(partition 3 3 '(a) (series 10))
)

;------------------------------------------------------------------------------

(partition-all (size step seq)
               (size seq)
"""
Divides `seq` into subsequences of length `size`. Default <step> equals `size`.
The last subsequence may contain less elements.
"""
(partition-all 3 (series 10))
(partition-all 3 2 (series 10))
(partition-all 3 4 (series 10))
)

;------------------------------------------------------------------------------

(partition-by (key seq)
"""
Returns a list of subsequences of `seq`. A new subsequence is started every time
the function `key` has a different value for two adjacent elements of `seq`.
"""
(partition-by odd? '(1 2 3 3 4 2 5 6 6))
)

;------------------------------------------------------------------------------

(path.combine (path1 path2)
              (path1 path2 path3)
              (path1 path2 path3 path4)
              (&rest paths)
"""
Like the original but with forward slashes.
"""
)

;------------------------------------------------------------------------------

(path.get-directory-name (path)
"""
Like the original but with forward slashes.
"""
)

;------------------------------------------------------------------------------

(path.get-unix-name (name)
"""
Converts backward slashes to forward slashes.
"""
)

;------------------------------------------------------------------------------

(path.get-windows-name (name)
"""
Converts forward slashes to backward slashes.
"""
)

;------------------------------------------------------------------------------

(pop (place)
"""
Returns the `first` element of the list `place` and updates the `setf`able 
`place` to the `rest` of the list.
"""
(var a '(1 2 3))
(pop a)
a
)

;------------------------------------------------------------------------------

(position (item seq &key (test =) (key identity))
"""
Returns the position of `item` in `seq`.
"""
(position 3 '(1 2 3 4))
(position 3 '(1 2 4))
(position 3 '(1 2 3 4) :test <)
(position 4 (vector "aap" "noot" "mies") :key length)
)

;------------------------------------------------------------------------------

(position-if (predicate seq &key (key identity))
"""
Returns the position of the first item in `seq` that satisfies the function 
`predicate`.
"""
(position-if odd? '(2 3 4))
(position-if odd? '(2 4))
(position-if odd? (vector "aap" "noot" "mies") :key length)
)

;------------------------------------------------------------------------------

(math.pow (number1 number2)
"""
Raises `number1` to the power of `number2`.
"""
(math.pow 2 3)
(math.pow -1 1)
(math.pow -1 (/ 1 2))
(* it it)
)

;------------------------------------------------------------------------------

(pprint (expr &key (left $left) (right $right))
"""
Called by `write` when its argument `pretty` is true. See also `$pprint-hook`.
This function is defined in the file pprint.k.
"""
)

;------------------------------------------------------------------------------

(print (&rest items)
"""
Writes each item unescaped to standard output.
"""
(print "Hello" ", " "World")
(write "Hello" :escape false)
(write "Hello" :escape true)
)

;------------------------------------------------------------------------------

(print-line (&rest items)
"""
Writes each item unescaped to standard output followed by a newline.
"""
(print-line "Hello" ", " "World")
(write-line "Hello" :escape false)
(write-line "Hello" :escape true)
)

;------------------------------------------------------------------------------

(print-warning (&rest args)
"""
Prints a warning message to `$stdlog`. Warning messages are prefixed by `;; warning: `.
"""
)

;------------------------------------------------------------------------------

(prototype.get-parents (object &key (inherited false))
"""
Returns the list of parent objects of the prototype object `object`.
"""
(var a (new :color "black" :model "0"))
(var b (new a :model "12"))
(.get-parents b)
)

;------------------------------------------------------------------------------

(prototype.get-superclasses (obj)
"""
Returns a list of the superclasses of the prototype object `obj`.
"""
(deftype a1 (new :city "leiden"))
(deftype a2 (new :country "netherlands"))
(def b (new '(a1 a2) :name "mies"))
b
(.get-superclasses b)
)

;------------------------------------------------------------------------------

(prototype.get-value (object name)
"""
Returns the value of property `name` of prototype object `object`.
"""
(var z (new "name" "Piet"))
(prototype.get-value z "name")
(prototype.get-value z :name)
(.get-value z :name)
(.name z)
(elt z "name")
(attr z "name")
)

;------------------------------------------------------------------------------

(prototype.has-property (object ident &key (inherited true))
"""

"""
(var z (new "name" "Piet"))
(.has-property z :name)
(.has-property z :address)
)

;------------------------------------------------------------------------------

(prototype.keys (object)
"""
Returns a list of strings.
"""
(var z (new "name" "Piet" :city "Amsterdam"))
(.keys z)
)

;------------------------------------------------------------------------------

(prototype.new (&rest args)
"""
Equivalent to `new`.
Creates a new prototype object. A single argument can be a parent prototype. Two 
consecutive arguments can be a name/value pair used to initialize
the new object. Names can be given as strings, symbols and keywords.
"""
(var a (new :color "black" :model "0"))
(new a :model "12")
)

;------------------------------------------------------------------------------

(prototype.set-parents (object parents)
"""
Sets the list of the direct parents of `object` to `parents`.
"""
)

;------------------------------------------------------------------------------

(prototype.set-value (object name value)
"""
Sets the value of property `name` of prototype object `object` to `value`.
"""
(var z (new "name" "Piet"))
(prototype.set-value z :name "Jan")
z
(setf (.name z) "Mies")
z
)

;------------------------------------------------------------------------------

(prototype? (expr)
"""
Returns true if `expr` is a prototype object.
"""
(prototype? (new :color "black"))
)

;------------------------------------------------------------------------------

(push (item place)
"""
Prepends `item` to the list `place` and updates the `setf`able `place` to 
the new list.
"""
(var a '(1 2 3))
(push 4 a)
)

;------------------------------------------------------------------------------

(pushnew (item place)
"""
Prepends `item` to the list `place` if `item` is not in in the list and updates 
the `setf`able `place` to the new list.
"""
(var a '(1 2 3))
(pushnew 4 a)
(pushnew 3 a)
)

;------------------------------------------------------------------------------

(quote (expr)
"""
Returns `expr` as literal data instead of evaluation `expr` as code. Usually 
written as `'expr` instead of `(quote expr)`.
"""
(EXAMPLE "(quote a)") 
'a
(EXAMPLE "(quote (+ 3 4))")
'(+ 3 4)
)


;------------------------------------------------------------------------------

(r/filter (predicate seq)
"""
Returns a `reducible` where each element of `seq` is filtered by the
function `predicate`. 
"""    
(reduce + (r/filter even? (r/map length (vector "aap" "noot" "mies"))))
)

;------------------------------------------------------------------------------

(r/map (func seq)
"""
Returns a `reducible` where each element of `seq` is transformed by the
function `func`. 
"""
(reduce + (r/map length (vector "aap" "noot" "mies")))
)

;------------------------------------------------------------------------------

(r/mapcat (func seq)
"""
Returns a `reducible` where each element of `seq` is transformed by the
function `func` and all `func` outputs are concatenated.
"""
(reduce + (r/mapcat series (series 5)))
)

;------------------------------------------------------------------------------

(r/take (count seq)
"""
Returns the first `count` items in `seq`.
"""
(reduce + (r/take 3 (series 10)))
)

;------------------------------------------------------------------------------

(r/take-while (predicate seq)
"""
Returns items from `seq` while they satisfy the function `predicate`.
"""
(reduce + (r/take-while (λx (< x 3)) (series 10)))
)

;------------------------------------------------------------------------------

(range (start end step)
       (start end)
       (end)
       ()
"""
Returns an enumerator of integer numbers. By default, a range starts at 0 and
excludes its upper bound.
"""
(range 3 10 2)
(range 3 10)
(range 10)
)

;------------------------------------------------------------------------------

(rational? (expr)
"""
Returns true if `expr` is a rational number.
"""
(rational? (/ 1 2))
(rational? 1)
(rational? (math.pow 2 0.5))
)

;------------------------------------------------------------------------------

(read (&key (stream $stdin) eof-value)
"""
Reads an expression from a stream created by `make-reader`.
"""
)

;------------------------------------------------------------------------------

(read-all (&key (stream $stdin))
"""
Reads a list of expressions from a stream created by `make-reader`.
"""
)

;------------------------------------------------------------------------------

(read-all-from-string (text)
"""
Reads a list of expressions from the string `text`.
"""
)

;------------------------------------------------------------------------------

(read-from-string (text &key eof-value)
"""
Reads an expression from the string `text`.
"""
(read-from-string "(+ 3 4)" :eof-value -1)
(read-from-string "" :eof-value -1)
)

;------------------------------------------------------------------------------

(read-string-from-console ()
"""
Returns a string read from the console. The terminating CR, LF or CRLF is stripped from
the string.
"""
)

;------------------------------------------------------------------------------

(recur 
"""
A lexical read-only variable bound to the lambda that contains it.
"""
)

;------------------------------------------------------------------------------

(reduce (adder seq &key initial-value (key identity))
"""
Returns the accumulated result of repeatly calling the function `adder` 
on each element of `seq`. `adder` is a function with two arguments. 
The first argument is the result so far, the second argument is the current 
element of `seq`.
"""
(reduce + (range 1 101))
)

;------------------------------------------------------------------------------

(reference (assembly-name)
"""
Loads an assembly.
"""
(reference "ICSharpCode.SharpZipLib")
)

;------------------------------------------------------------------------------

(repeat (count value)
        (value)
"""
Returns a lazy sequence that yields `value`. The second form yields `value` an 
infinite number of times.
"""
(repeat 10 'a)
)

;------------------------------------------------------------------------------

(require (file &key (verbose $load-verbose) (print $load-print))
"""
Does nothing if `file` is already on the list `$modules`. Otherwise adds `file`
to `$modules` and calls the function `load`.
"""
(require :ftp :print true)
)

;------------------------------------------------------------------------------

(rest (list)
"""
Returns the remainder of `list` without the `first` element.
"""
(rest '(a b c))
)

;------------------------------------------------------------------------------

(resume (ctx)
"""
Called by the thread that created the generator thread `ctx`. Returns the value 
the was `yield`ed by the generator thread.
"""
)

;------------------------------------------------------------------------------

(return (&optional value)
"""
Returns `value` or `null` from a function.
"""
)

;------------------------------------------------------------------------------

(return-if (test &optional value)
"""
If `test` evaluates as true, returns `value` or `null` from a function, otherwise
continues execution of the function.
"""
)

;------------------------------------------------------------------------------

(return-or-throw (try-and-catch-result)
"""
Depending on the run-time type of the value of `try-and-catch-result`, returns the
value or throws the exception.
"""
)

;------------------------------------------------------------------------------

(reverse (seq)
"""
Returns a lazy sequence with the elements of `seq` in reverse order.
"""
(reverse (series 1 10))
)

;------------------------------------------------------------------------------

(run (file)
"""
Loads `file` and (if it exists) executes the function `user.main`.
"""
)

;------------------------------------------------------------------------------

(second (seq)
"""
Returns the second element of `seq`, or `null`.
"""
(second '(a))
(second '(a b))
)

;------------------------------------------------------------------------------

(sequence? (expr)
"""
Returns true if `expr` is a sequence, i.e. `null` or IEnumerable.
"""
(sequence? null)
(sequence? "abc")
(sequence? '(a b c))
)

;------------------------------------------------------------------------------

(series (start end step)
        (start end)
        (end)
"""
Returns an enumerator of integer numbers. By default, a series starts at 1 and
includes its upper bound.
"""
(series 3 10 2)
(series 3 10)
(series 10)
)

;------------------------------------------------------------------------------

(set (sym value)
"""
Assigns `value` to the global or dynamic variable named by the value of `sym`. 
Does not work for lexical variables.
"""
(setq x 'a137)
(set x 1)
(def a137 2)
(set x 1)
a137
)

;------------------------------------------------------------------------------

(set-attr (object property value)
"""
Assigns `value` to `property` of `object`.
"""
(var obj (new))
(set-attr obj 'name "mies")
(attr obj 'name)
(.name obj)
(macroexpand '(setf (.name obj) "mies"))
)

;------------------------------------------------------------------------------

(set-car (list item)
"""
Assigns `item` to the first item of `list`.
"""
(var z '(111 2 3))
(set-car z 111)
z
(macroexpand '(setf (car z) 111))
)

;------------------------------------------------------------------------------

(set-cdr (list item)
"""
Assigns the list `item` to the rest or cdr of `list`.
"""
(var z '(1 111))
(set-cdr z '(111))
z
(macroexpand '(setf (cdr z) '(111)))
)

;------------------------------------------------------------------------------

(set-console-key-binding (key modifiers handler)
"""
Binds a key/modifier combination to a handler.
See also: `system.k`
"""
)

;------------------------------------------------------------------------------

(set-elt (obj "index..." value)
"""
Assigns `value` to the place `index...` of `obj`. A multi-dimensional array uses
more than one index.
"""
(var obj (new))
(set-elt obj "name" "mies")
(elt obj "name")
(attr obj 'name)
(macroexpand '(setf (elt obj "name") "mies"))
)

;------------------------------------------------------------------------------

(set-fifth (seq item)
"""
Sets the fifth element of `seq` to `item.
"""
)

;------------------------------------------------------------------------------

(set-find-type (name type)
"""
Gives the name `name` to the prototype or System.Type `type`. The symbol `name`
may now be used by the function `type?` and by the special form `defmethod`.
See also: `deftype`.
"""
(set-find-type 'a (new :name "mies"))
)

;------------------------------------------------------------------------------

(set-first (seq item)
"""
Sets the first element of `seq` to `item.
"""
(var seq '(a 2 3 4))
(set-first seq 'a)
(macroexpand '(setf (first seq) 'a))
)

;------------------------------------------------------------------------------

(set-fourth (seq item)
"""
Sets the fourth element of `seq` to `item.
"""
)

;------------------------------------------------------------------------------

(set-load-path (&rest folders)
"""
Sets the variable `$load-path` to `folders`. 
See also: `kiezellisp-init.k`.
"""
)

;------------------------------------------------------------------------------

(use-package-alias (package-name nick-name)
"""
Gives the package with the long offical name `package-name` a short `nick-name`
but only within the current package.
"""
(use-package-alias "lisp" "l")
)

;------------------------------------------------------------------------------

(set-second (seq item)
"""
Sets the second element of `seq` to `item.
"""
)

;------------------------------------------------------------------------------

(set-symbol-documentation (sym value)
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(set-symbol-function-syntax (sym value)
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(set-symbol-value (sym value)
"""
Assigns `value` to the global variable `sym`.
"""
(def z 0)
(set-symbol-value 'z 1)
(setq z 2)
(macroexpand '(setf (symbol-value 'z) 1))
)

;------------------------------------------------------------------------------

(set-third (seq item)
"""
Sets the third element of `seq` to `item.
"""
)

;------------------------------------------------------------------------------

(setf (place value)
"""
Assigns `value` to `place`. See the file core/setf.
"""
(var a '(1 two 3))
(setf (second a) 'two)
a
(macroexpand '(setf (second a) 'two))
)

;------------------------------------------------------------------------------

(setq (sym value)
      (sym-list seq)
"""
Sets the value of the lexical, dynamic or global variables.
"""
(var x 1)
(setq x 3)
(var y)
(setq (x y) '(1 2))
x
y
)

;------------------------------------------------------------------------------

(shadow-symbol (name)
"""
Interns the string `name` in the current package to avoid it being inherited
from a package used by the current package, usually to avoid redefining the
value of the symbol in the used package.
See also: shell.k, [use-package](#use-package).
"""
)

;------------------------------------------------------------------------------

(shuffle (seq)
"""
Returns a random reordering of `seq`.
"""
(shuffle (range 10))
)

;------------------------------------------------------------------------------

(skip (count seq)
"""
Skips `count` elements of `seq` and returns the remainder, which may be empty.
"""
(skip 3 '(a b c d e f))
(skip 33 '(a b c d e f))
)

;------------------------------------------------------------------------------

(skip-while (predicate seq)
"""
Skips elements of `seq` while they satisfy the `predicate`.
"""
(skip-while odd? '(1 1 2 3 4 5))
)

;------------------------------------------------------------------------------

(sleep (millisec)
"""
Lets the current thread sleep for `millisec` milliseconds. -1 means forever.
"""
)

;------------------------------------------------------------------------------

(sort (seq &key (test <) (key identity))
"""
Sorts `seq`. The function `test` returns true when the first argument is less
than the second argument. Do not use `<=`.
"""
(sort "hello, world")
(sort '("hello" "goodbye" "doei" "farewell") :test < :key length)
)

;------------------------------------------------------------------------------

(special-form? (expr)
"""
Returns true if `expr` is a special form.
"""
(special-form? if)
(special-form? when)
)

;------------------------------------------------------------------------------

(special-symbol? (expr)
"""
Returns true if `expr` is a symbol whose name starts with a $ character.
"""
(special-symbol? 'stdout)
(special-symbol? '$stdout)
)

;------------------------------------------------------------------------------

(split-at (count seq)
"""
Equivalent to `(list (take count seq) (skip count seq))`.
"""
(split-at 4 (series 10))
)

;------------------------------------------------------------------------------

(split-with (predicate seq)
"""
Equivalent to `(list (take-while predicate seq) (skip-while predicate seq))`.
"""
(split-with even? '(2 2 2 1 3 3 3))
)

;------------------------------------------------------------------------------

(strict ()
"""
Sets the dynamic variable `$strict` to `true`. 
"""
)

;------------------------------------------------------------------------------

(string (&rest values)
"""
Returns the concatenation of the pure string representations of each `value`.
"""
(string "aap" '(1 2 3) 'noot)
)

;------------------------------------------------------------------------------

(string.capitalize (str)
"""
Capitalizes each word of a string.
"""
(.capitalize "aap noot mies")
)

;------------------------------------------------------------------------------

(string.convert-to-external-line-endings (str)
"""
On Windows, changes LF to CRLF.
"""
)

;------------------------------------------------------------------------------

(string.convert-to-internal-line-endings (str)
"""
On Windows, changes CRLF to LF.
"""
)

;------------------------------------------------------------------------------

(string.html-decode (str)
"""
Same as HttpUtility.HtmlDecode
"""
)

;------------------------------------------------------------------------------

(string.html-encode (str)
"""
Same as HttpUtility.HtmlEncode
"""
)

;------------------------------------------------------------------------------

(string.indent (text prefix)
"""
Adds a prefix string to each line in `text`.
"""
(print-line (.indent "aaaa\nbbbb" "---"))
)

;------------------------------------------------------------------------------

(string.indent-with-line-numbers (text line-number width separator)
"""
Adds a line number and prefix string to each line in `text`.
"""
(print-line (.indent-with-line-numbers "aaaa\nbbbb" 12 4 ": "))
)

;------------------------------------------------------------------------------

(string.join (separator values)
             (separator value start-index count)
             (separator seq)
"""
The third form is a Kiezellisp extension.
"""
(.join ", " '(aap noot mies))
)

;------------------------------------------------------------------------------

(string.json-decode (str)
"""
Decodes a JSON string.
"""
(string.json-encode (new :name "mies"))
(.json-decode it)
)

;------------------------------------------------------------------------------

(string.json-encode (value)
"""
Encodes a value as a JSON string.
"""
(print-line (string.json-encode (new :name "mies")))
)

;------------------------------------------------------------------------------

(string.latex-encode (str)
"""
Escapes characters that have a meaning in latex source.
"""
)

;------------------------------------------------------------------------------

(string.left (str count)
"""
Returns the leftmost `count` characters of `str`.
"""
(.left "aap" 2)
(.left "aap" 22)
)

;------------------------------------------------------------------------------

(string.lisp-name (name)
"""
Converts between naming conventions.
"""
(.lisp-name "EenTweeDrie")
)

;------------------------------------------------------------------------------

(string.lisp-to-camel-case-name (name)
"""
Converts between naming conventions.
"""
(.lisp-to-camel-case-name "een-twee-drie")
)

;------------------------------------------------------------------------------

(string.lisp-to-pascal-case-name (name)
"""
Converts between naming conventions.
"""
(.lisp-to-pascal-case-name "een-twee-drie")
)

;------------------------------------------------------------------------------

(string.next (str)
"""
Returns the string that succeeds `str`.
"""
(.next "abc")
(.next "abz")
)

;------------------------------------------------------------------------------

(string.pad-left (str width)
                 (str width padding-char)
                 (str width padding-str)
"""
The third form is a Kiezellisp extension.
"""
)

;------------------------------------------------------------------------------

(string.pad-right (str width)
                 (str width padding-char)
                 (str width padding-str)
"""
The third form is a Kiezellisp extension.
"""
)

;------------------------------------------------------------------------------

(string.parse-date (str &key format culture)
"""
See also: [try-parse-date](#string.try-parse-date). 
"""
)

;------------------------------------------------------------------------------

(string.parse-number (str &key base culture)
"""
See also: [try-parse-number](#string.try-parse-number). 
"""
)

;------------------------------------------------------------------------------

(string.prev (str)
"""
Returns the string that preceeds `str`.
"""
(.prev "abc")
(.prev "ab0")
)

;------------------------------------------------------------------------------

(string.regex-encode (str)
"""
Same as Regex.Escape.
"""
)

;------------------------------------------------------------------------------

(string.regex-match (str pattern)
"""
Compares `str` to the string or Regex `pattern`. Returns null if no match.
Returns a list of matches otherwise.
"""
(.regex-match "hello, world" "(.)rl")
)

;------------------------------------------------------------------------------

(string.regex-replace (str pattern transform-func)
                      (str pattern replacement-str)
"""
Compares `str` to the string or Regex `pattern`. The number of arguments of 
`transform-func` must be equal to the number of groups in the pattern. 
`replacement-str` may refer to groups as \1, \2, etc.
"""
)

;------------------------------------------------------------------------------

(string.regex-split (str pattern)
"""

"""
)

;------------------------------------------------------------------------------

(string.repeat (str count)
"""
Returns a string consisting of `count` copies of `str`.
"""
(.repeat "abc" 3)
)

;------------------------------------------------------------------------------

(string.right (s count)
"""
Returns the rightmost `count` characters of `str`.
"""
(.right "aap" 2)
(.right "aap" 22)
)

;------------------------------------------------------------------------------

(string.split (str)
              (str count)
              (str separators)
              (str separators count)
"""
These forms are Kiezellisp extension methods. The first and second forms split on
white space and always remove empty substrings. In the third and fourth forms
`separators` is a string or another sequence (IEnumerable other than string).
In case of a string, the function splits on the individual characters of the
string.
"""
(.split "aap noot mies" 2)
:d
(.split "hello, world" "lw")
:d
)

;------------------------------------------------------------------------------

(string.trim (str)
             (str &rest trim-chars)
             (str trim-chars-str)
"""
The third form is a Kiezellisp extension. 
"""
(.trim "efefabcdefefefef" "fe")
)

;------------------------------------------------------------------------------

(string.trim-end (str)
                 (str &rest trim-chars)
                 (str trim-chars-str)
"""
The third form is a Kiezellisp extension. 
"""
(.trim-end "abcdefefefef" "fe")
)

;------------------------------------------------------------------------------

(string.trim-start (str)
                   (str &rest trim-chars)
                   (str trim-chars-str)
"""
The third form is a Kiezellisp extension. 
"""
(.trim-start "abcdefefefef" "fe")
)


;------------------------------------------------------------------------------

(string.try-parse-date (str &key format culture)
"""
Converts a string to a DateTime.
"""
(.try-parse-date "20130104" :format "yyyyMMdd")
(.try-parse-date "20139904" :format "yyyyMMdd")
)

;------------------------------------------------------------------------------

(string.try-parse-number (str &key base culture)
"""
Converts a string to a number.
"""
(.try-parse-number "123.4" )
(.try-parse-number "123a" )
(.try-parse-number "123a" :base 16 )
)

;------------------------------------------------------------------------------

(string.url-decode (str)
"""
Same as HttpUtility.UrlDecode
"""
)

;------------------------------------------------------------------------------

(string.url-encode (str)
"""
Same as HttpUtility.UrlEncode
"""
)

;------------------------------------------------------------------------------

(string? (value)
"""
Returns true if `value` is a string.
"""
(string? "aap")
(string? 123)
)

;------------------------------------------------------------------------------

(structurally-equal (obj1 obj2)
"""
Returns true if `obj1` and `obj2` are `equal` objects or sequences containing
the same `structurally_equal` objects.
"""
(structurally-equal 1 1)
(structurally-equal (series 5) (vector 1 2 3 4 5))
)

;------------------------------------------------------------------------------

(subseq (seq start &key end count default)
"""
Returns a subsequence of `seq` starting at position `start` and continuing
to position `end` or until `count` elements have been yielded. If the sequence
is too short to yield `count` arguments and if `default` is supplied, the 
subsequence is padded by `default` elements.
"""
(subseq (series 5) 2)
(subseq (series 5) 2 :count 7 :default 9999)
)

;------------------------------------------------------------------------------

(sum (seq &key (key identity))
"""
Returns the sum of elements in `seq`.
"""
(sum '(1 2 3 4))
(sum '("aap" "noot" "mies") :key length)
)

;------------------------------------------------------------------------------

(symbol-documentation (sym)
"""

"""
)

;------------------------------------------------------------------------------

(symbol-function-syntax (sym)
"""

"""
)

;------------------------------------------------------------------------------

(symbol-name (sym)
"""
Returns the string name of the symbol `target`.
"""
(symbol-name 'csv.read-string-to-grid)
)

;------------------------------------------------------------------------------

(symbol-package (sym)
"""
Returns the package owning the symbol `sym`.
"""
(symbol-package 'csv.read-string-to-grid)
)

;------------------------------------------------------------------------------

(symbol-value (sym)
"""
Returns the value of the global variable `sym`.
"""
(var a 'csv.read-string-to-grid)
(symbol-value a)
csv.read-string-to-grid
)

;------------------------------------------------------------------------------

(symbol? (expr)
"""
Returns true if `expr` is a symbol.
"""
(symbol? 'loop)
)

;------------------------------------------------------------------------------

(system.call-next-method (current-method args)
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(system.create-delayed-expression (func)
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(system.create-generator (code &rest kwargs)
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(system.create-tailcall (proc &rest args)
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(system.create-task (code)
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(system.dispose (resource)
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(system.enable-benchmark (flag)
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(system.get-current-thread ()
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(system.get-safe-enumerator (list)
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(system.optimizer (expr)
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(tagbody 
"""
Internal use.
"""
)

;------------------------------------------------------------------------------

(tailcall (func &rest args)
"""
Compiles a tail call.
"""
)

;------------------------------------------------------------------------------

(take (count seq)
"""
Returns the first `count` items in `seq`.
"""
(take 3 (series 10))
)

;------------------------------------------------------------------------------

(take-nth (step seq)
"""
Returns every `step` item from `seq`.
"""
(take-nth 1 (series 10))
(take-nth 2 (series 10))
)

;------------------------------------------------------------------------------

(take-while (predicate seq)
"""
Returns items from `seq` while they satisfy the function `predicate`.
"""
(take-while (λx (< x 3)) (series 10))
)

;------------------------------------------------------------------------------

(task (&body forms)
"""
Creates a task that runs `forms`. The return value of `task` is a ThreadContext
object that exposes the properties `Result` and `IsCompleted`. `Result` blocks
until IsCompleted is true.
"""
(var a (task (* 1 2 3 4 5)))
(.result a)
(.is-completed a)
)

;------------------------------------------------------------------------------

(third (seq)
"""
Returns the third item in `seq`.
"""
(third '(1 2 3 4))
(third '(1 2))
)

;------------------------------------------------------------------------------

(throw (exception)
"""
Throws an exception.
"""
(throw (lisp-exception.new "Bad things happened."))
)

;------------------------------------------------------------------------------

(throw-error (&rest args)
"""
Throws an exception of type LispException. The arguments are combined as if by 
the function `string`.
"""
(var n 2)
(throw-error "There are " n " things wrong.")
)

;------------------------------------------------------------------------------

(trace (expr)
"""
Conditionally writes the source code of `expr` and its value to `$stdlog`, depending 
on the value of `$tracing`. `expr` must not have side effects. Trace code is compiled
away when running in production mode.
"""
(do (var $tracing true) (trace (+ 1 2)))
)

;------------------------------------------------------------------------------

(try (&body forms)
"""
Executes `forms`. Returns `null` if an exception is thrown.
"""
(try (+ 3 hello))
)

;------------------------------------------------------------------------------

(try-and-catch (&body forms)
"""
Executes `forms`. Returns an exception if an exception is thrown.
"""
(try-and-catch (+ 3 hello))
)

;------------------------------------------------------------------------------

(try-finally (form cleanup-form)
"""
Returns `form`. `cleanup-form` is executed when `form` returns normally and
when `form` throws an exception.
"""
(try-finally (+ 3 hello) (print-line 111))
)

;------------------------------------------------------------------------------

(type? (target type)
"""
Returns true if `target` has type `type`.
"""
(type? "abc" 'string)
)

;------------------------------------------------------------------------------

(typecase (keyform normal-clause* \[otherwise-clause\])
"""
    normal-clause := '(' type form* ')'
    otherwise-clause := '(' {true|otherwise} form* ')'
    type := symbol corresponding to a (imported) class
"""
(var s 'aap)
(typecase s
    (integer (print-line 1))
    (symbol (print-line 2))
    (otherwise (print-line 3)))
)

;------------------------------------------------------------------------------

(typeof (expr)
"""
Returns the runtime type of `expr`.
"""
(typeof 123)
(typeof "hello")
)

;------------------------------------------------------------------------------

(union (seq1 seq2 &key (test =) (key identity))
"""
Returns the union of `seq1` and `seq2`.
"""
(union '(1 2 3) '(3 4 5))
)

;------------------------------------------------------------------------------

(unuse-package (name)
"""
Removes package `name` from the use list of the current package.
"""
)

;------------------------------------------------------------------------------

(unzip (seq)
"""
Returns a list of the elements at even positions and at odd positions of `seq`.
"""
(unzip (series 10))
(unzip (range 10))
)

;------------------------------------------------------------------------------

(use-package (name)
"""
Add package `name` from the use list of the current package.
"""
)

;------------------------------------------------------------------------------

(using ((sym resource) &body forms)
"""
Executes `forms` as a `do` block and guarantees to call `IDispose.dispose`
on the variable `sym` to release the `resource`.
"""
)

;------------------------------------------------------------------------------

(var (sym \[value\])
     (sym-list \[seq\])
"""
The first form declares a lexical variable `sym`, initialized to `value` or `null`. 
The second form declares a list of variables that are initialized from the 
sequence `seq`.
This form must be used in an implicit or explicit `do` block.
"""
(do (var x 111) (+ x x))
(do (var (a b c) (series 2)) (list a b c))
)

;------------------------------------------------------------------------------

(vector (&rest items)
"""
Returns a vector (ArrayList) containing `items`.
"""
(vector 1 2 3 4)
:d
)

;------------------------------------------------------------------------------

(vector* (&rest items)
"""
Returns a vector (ArrayList) containing `items`. The last item must be a
sequence.
"""
(vector* 1 2 3 '(4 5 6))
)

;------------------------------------------------------------------------------

(vector? (expr)
"""
Returns true if `expr` is a vector.
"""
)

;------------------------------------------------------------------------------

(when (test &body forms)
"""
If `test` is true, then evaluates `forms` as a `do`-block; else `null`.
"""
(when (odd? 123) (print-line "odd") 456)
)

;------------------------------------------------------------------------------

(when-let ((sym test) &body forms)
"""
If `test` is true, binds the test value to the variable `sym`,
then evaluates `forms` as a `do`-block; else `null`.
"""
(when-let (x true) (print-line "yes") x)
)

;------------------------------------------------------------------------------

(when-match (pattern expr &body forms)
"""
If `expr` matches `pattern`, returns the result of `forms`.
See also [pattern-matching](pattern-matching.html).
"""
)

;------------------------------------------------------------------------------

(while (test &body forms)
"""
Specialized form of `loop`.
"""
(macroexpand-1 '(while test a b c))
)

;------------------------------------------------------------------------------

(with-input-from-string ((sym text) &body forms)
"""
Binds `sym` to a lisp reader on the string `text` and executes `forms` as a 
`do` block.
"""
(with-input-from-string ($stdin "aap noot mies") (read) (read))
)

;------------------------------------------------------------------------------

(with-output-to-string ((sym) &body forms)
"""
Binds `sym` to a string writer and executes `forms` as a `do` block.
Returns the contents of the string writer.
"""
(with-output-to-string ($stdout) (print "hello"))
)

;------------------------------------------------------------------------------

(write (item &key stream escape width padding pretty left right force base color background-color)
"""
Writes `item` to the output stream `stream`. 

    stream := null | true | false | string | TextWriter (default: $stdout)
    escape := boolean
    width := integer
    padding := value-to-be-converted-to-a-string (default: #\space)
    pretty := boolean
    left := integer
    right := integer
    force := boolean
    base := integer (default: 10)
    color := color-designator
    background-color := color-designator

If `stream` is null or false the output is discarded.
If `stream` is true the output goes to `Console.Out`.
If `stream` is a string, the output goes to the file `stream`-yyyy-MM-dd.log. This
is thread-safe.
Otherwise `stream` must be a `TextWriter`.

If `escape` is false the output is text-like otherwise the output is code-like.

`base` is the number base for integer numbers.

`width` is the exact number of characters to be printed. If `width` is to small,
the output is truncated. If `width` is to big, numbers are padded on the left 
and strings are padded on the right.

`color` and `background-color` work only for console output. See also the
ConsoleColor enumeration.

`left` and `right` are column offsets. Used `pretty` is true. See also `pprint.k`.

"""
)

;------------------------------------------------------------------------------

(write-line (item &key stream escape width padding pretty left right force base color background-color)
"""
Same as `write` but also writes a line feed.
"""
(write-line 123 :width 8 :padding 0)
(write-line 123 :base 7 :escape false)
(write-line 123 :base 7 :escape true)
(write-line "123" :escape false)
(write-line "123" :escape true)
)

;------------------------------------------------------------------------------

(write-to-string (item &key escape width padding pretty left right force base)
"""
Returns a string written to a `StringWriter` stream by the function `write`.
"""
(write-to-string 123 :width 6 :padding "0" :base 9)
)

;------------------------------------------------------------------------------

(xor (&rest values)
"""
Returns the logical XOR of the given `values`.
"""
(xor 1)
(xor 1 2)
(xor 1 2 3)
)

;------------------------------------------------------------------------------

(yield (item)
"""
See also: [generator](#generator).
"""
)

;------------------------------------------------------------------------------

(zero? (number)
"""
Returns true if the number `number` equals zero. Fails if `number` is not a number.
"""
(zero? 0)
(zero? 1)
(zero? "aaa")
)

;------------------------------------------------------------------------------

(zip (&rest seqs)
"""
Returns a lazy sequence consisting a vector with all first elements, a vector with
all second elements, and so on.
"""
(zip '(1 2 3) '(4 5 6 7))
(zip '(1 2 3) (repeat '-))
)

;------------------------------------------------------------------------------

(λ 
"""
See also: [Functions](functions.html).
"""
)

;------------------------------------------------------------------------------

))

