# Change Log

## 2015/06/11

*   Moved backquote/quasiquote expansion from compiler back to reader.
*   Added `$repl-force-it` variable.

## 2015/05/20

*   Removed `--listener` command line option.
*   Added `start-listener` function.
*   Undid removal of REPL smart parentheses feature.
*   Modified Ctrl+Enter to suppress REPL smart parentheses feature.
*   Removed function `unuse-package`.

## 2015/05/17

*   Fixed implicit conversion bug.
*   Modified `describe` for compiler macro feature.

## 2015/05/16

*   Added `define-compiler-macro`.
*   Removed REPL smart parentheses feature.

## 2015/05/01

*   Added warning when shadowing symbol from inherited package.
*   Changed application of `UnwindException`.

## 2015/04/27

*   Fixed bug in `merging-do` compilation.
*   Separated namespaces for specialforms/macros and functions/variables.

## 2015/04/26

*   Changed implementation of documentation functions.

## 2015/04/17

*   Changed `#"..."` regex literal to `#/.../`.
*   Added `defclass` and `defstruct` macros.
*   Added `#s(...)` reader macro.
*   Added `#v(...)` reader macro.
*   Fixed (rare) bug in `loop` macro expansion.
*   Added `$print-vector-with-brackets`.
*   Added `$print-prototype-with-braces`.

## 2015/03/16

*   Added `assoc` and `assoc-if` functions.
*   Added `[STAThread]` attributes that went missing.

## 2015/03/15

*   Changed the name of `kiezellisp-init` to be a constant.

## 2015/03/12

*   Simplified `EmbeddedMode` class.
*   Added `math::random` function.

## 2015/03/06

*   Added `EmbeddedMode` feature.

## 2015/02/21

*   Added `lambda*` and `defun*` defining multi-arity functions.
*   Fixed `recur` bug.

## 2015/01/08

*   Better handling of numeric conversions.
*   Fixed extension handling in `reference` function.
*   Fixed bug in `shell.k`.
*   Added `~/` expansion in strings.
*   Added `${...}` expansion in strings.

## 2014/12/28

*   Changed all files to LF instead of CRLF line endings.
*   Fixed warnings detected by mono compiler.
*   Added `$home` and other variables to `shell.k`.
*   Added function `shell:expand-path-name` to expand path names 
    starting with a tilde.

## 2014/12/22

*   Renamed `:windows-nt` feature to `:windows`.
*   Renamed `:windows-mode` feature to `:graphical-mode`.
*   Fixed bug in `shell::enclose-in-quotes`.

## 2014/12/19

*   Fixed bug using PATH environment variable on unix.
*   Changed location of DLR dlls to be in the src tree.

## 2014/12/07

*   Added `IApply` interface to `Symbol`.
*   Pretty print extensions for Kiezellisp IDE (in progress).
*   Moved backquote/quasiquote expansion from reader to compiler.
*   Fixed bug in function call to `>` operator with three or more arguments.

## 2014/11/21

*   Renamed `skip` to `drop`.
*   Changed `map` to support multiple sequences.
*   Added `remove`, `map-indexed`, `keep`, `keep-indexed`.
*   Added `reductions`, `cycle`, `repeatedly`, `iterate`.
*   Refactored method selection code.
*   Changed `multiple-var` etc from special form to macro.
*   Added `complement`.
*   Fixed bug in code of array/indexer assignment.

## 2014/09/07

*   Added `multiple-var`.
*   Added `multiple-let`.
*   Added `multiple-setf`. 

## 2014/08/18

*   Fixed bug: `any` sequence function.
*   Refactored `loop` macro.
*   Refactored pattern matching functions.
*   Added `breakpoint` function.
*   Fixed bug: changed reference from `make-reader` to `lisp-reader:new`.
*   Added `copy-tree`.
*   Renamed `make-environment` to `make-extended-environment`.
*   Added `&environment` argument modifier.
*   Added `environment` parameter to `macroexpand` and `macroexpand-1`.
*   Added `code-walk` and `code-walk-list`.
*   Added `find-name-in-environment`.
*   Added `make-environment`.
*   Added `letfun`.
*   Replaced keyword labels by the `label` special form.
*   Fixed bug in `equal` function.


## 2014/06/16

*   Added `var` and `let` declarations to file scope.
*   REPL expression does not need parentheses if first term is a function or
    a special form.
*   Added `block`, `return-from` for named blocks.
*   Added `$package-name-prefix` special variable.

## 2014/05/29

*   Merged `tagbody` into `do` to improve loop performance. `do` forms that 
    are keywords are interpreted as labels.
*   Removed `tagbody`.
*   Renamed `go` to `goto`.
    

## 2014/05/24

*   Fixed bug in handling of function call with too few arguments.
*   Changed `#(...)` lambda expression to be compatible with clojure's.
*   Added sequence functions `repeatedly`, `iterate` and `cycle`.

## 2014/05/12

*   Added `ecase`, `etypecase` and `ecase-match` macros.
*   Added `loop` clauses like `(for i :from a :to b)`.
*   Added `series-enumerator`.

## 2014/05/08

*   Added `(lazy sym expr))`.
*   Added `(future sym expr))`.
*   Fixed bug: infinite loop compiling call to multi-method with `null` eql 
    specifier.

## 2014/04/15

*   Removed `$strict` and `strict`.

## 2014/04/08

*   REPL expression does not need parentheses if first term is a function.
*   Renamed `$quick-import` to `$lazy-import`.
*   Added accessor chaining feature: `(.name.to-upper obj)`
*   Symbols generated by `gentemp` are now placed in the `temp` package.
*   Deimplemented uninterned symbols.
*   Fixed bug in code generated for lambda derived delegate with value type 
    parameters.
*   Rewrote parser/scanner to use readtables.
*   Removed function `make-reader` in favor of `lisp-reader:new`.

## 2014/03/03

*   BREAKING CHANGE: changed `try` to `ignore-errors`; introduced `try` with
    `catch` and `finally` clauses.
    
## 2014/02/22

*   BREAKING CHANGE: using colon and double colon package-symbol separators 
    instead of period and exclamation mark. 

## 2014/02/02

*   Removed `$print-max-elements`.

## 2014/01/21

*   Fixed bug where `then` and `else` branch of `if` yielded different types.
*   Fixed bug where a prototype getter function was called on the wrong object.
*   Added `&whole` parameter modifier.
*   Added `--listener` command line option.

## 2014/01/20

*   Fixed numeric arguments conversion bugs.

## 2014/01/03

*   Added DLR restrictions for calls to multi-methods.
*   Fixed bug in `reduce`.
*   Added `mapcat`.
*   Added `conjoin`.
*   Added `r/map`, `r/mapcat`, `r/filter`, `r/take`, `r/take-while`.
*   Added `$print-max-elements`.
*   Changed Ctrl+ENTER action in the REPL.

## 2013/11/29

*   Renamed `set-package-alias` to `use-package-alias`.
*   Restricted use of parameter modifiers such as `&key` to just one in
    order to optimize calls to lisp functions.
*   Removed the `&whole` modifier.
*   Extended `<=` and other relational parameters to have two or more arguments.

## 2013/11/14

*   Replaced `#{...}` string interpolation markers with `double-back-tick...double-back-tick`.

## 2013/11/04

*   Fixed bug in `json-decode`.

## 2013/10/21

*   Refactored `console.cs`.
*   Added `-init` command line option.
*   Added `exit` function.

## 2013/10/18

*   Added variable `$quick-import`.
*   Other optimizations.
*   Added `winforms.k`.

## 2013/10/04

*   Added `(declare (ignore var))`.
*   `DataSource` support for prototype objects.
*   Added `(as-vector seq type)`.
*   Changed `Vector` type to `List<object>`.
*   Fixed bug in code for constructing value types.
*   Removed `add-event-handler`.
*   Support for adding event handlers through `setf`.

## 2013/09/17

*   Fixed bug `loop` macro.
*   Added delta parameter to `incf` and `decf`.
*   Added some unit tests.
*   Added `as-tuple`.

## 2013/09/16

*   Fixed bugs in `split-with` and `skip-while`.
*   Fixed internal pattern matching bugs.
*   Implemented numbered `~` variables to improve debugging experience.
*   Replaced `gensym` by `gentemp` to improve debugging experience.
*   Rewrote (part of) binding of lambda arguments to parameters.
*   Added prototype and vector literals.
*   Restricted scope of `set-package-alias` to the current package.
*   Automatic conversion of enumerable arguments to enumerable<T> parameters.
*   Support for generic type by adding `type-parameters` argument to
    the function `import`.
*   Added `string.regex-match-all` and `string.wildcard-match`.
*   Added options to `natural-compare`.
*   Added `gentemp`.
*   Fixed handling of explicit enumerator interfaces.

## 2013/08/19

*   Added prototype as a function feature.
*   Changed member accessor function '.' to have string argument only.
*   Added `elt` support for lists.
*   Added function `set-read-decimal-numbers(flag)` for better interfacing
    with math libraries.
*   Fixed error in codegen of index binders.
*   Allow any key type in prototype objects.

## 2013/08/02

*   Fixed spelling of external dll names for case-sensitive OS.
*   Fixed error in usage of `as-array` function.

## 2013/07/30

*   Initial release.    
