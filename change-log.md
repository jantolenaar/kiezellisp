# Change Log

### 2019/03/20

*   Fixed version/copyright strings.

### 2019/03/16

*   Removed command line options for foreground and background color.
*   Added :ansi-terminal feature.

### 2019/03/10

*   Improved code completion.

### 2019/02/07

*   Removed multiple value features due to implementation issues.
*   Modified `find`, `math:floor` etc to return a single value again.

### 2018/12/31

*   Removed support for chained properties as in `(.name.to-upper obj)`
    due to compilation complexity.
*   Renamed `first-position` to `find`.
*   `find` returns a multiple value (item, index, flag).

### 2018/12/12

*   Added `values`, `values-list`, `multiple-value-list`, 
*   Modified `math:floor` and its look-alikes to use multiple values.
*   Removed `divrem` and `rem`.
*   Added `math:rem` and `math:mod`.


### 2018/12/1

*   Moved `kiezellisp-init.k` to application data folder.
*   Moved history to application data folder.
*   Save history immediately after entering a line.
*   Checked solution to use Microsoft Visual Studio document 
    formatting.
*   Fixed recursion problem in `ReadDelimitedList`.

### 2018/11/6

*   Added source of documentation and build scripts.
*   Restricted documentation build to html.

### 2018/08/29

*   Fixed bug in `reduce`.
*   Fixed bug in `min`, `max` , `minimize` and `maximize`.
*   Fixed bug in `loop`.

### 2018/07/31

*   Removed DLR (lightcompile, lightdynamic) from project.
*   Added lazy compilation of linq expressions in lambda definitions.

### 2018/07/30

*   Renamed `recur` to `reprog`.

### 2018/05/18

*   Removed `goto` and `label`.
*   Changed `block` to special form.
*   Added `redo` and `leave` special forms to jump in named blocks.

### 2017/12/30

*   Allow :commands and ,commands in REPL.

### 2017/12/01

*   Fixed `pop` macro.

### 2017/10/09

*   loop collecting clause now returns list instead of vector.
 
### 2017/08/31

*   Parse `a:b:c` as package name `a` and symbol name `b:c` to support
    things like `:xmlns:xlink` as a keyword in `XmlElement`.
*   Improved warning about shadowing symbols.
*   Fixed bug in generated `loop` code, present in debug version only.
*   Fixed `call-next-method` macro.
*   Prevent keyword-like variable names.
*   Added `lftp` wrapper.

### 2017/08/20

*   Fixed bug in `keep` and `keep-indexed`.

### 2017/08/20

*   Added `mono5` commandline option.

### 2017/08/04

*	Kiezellisp 4.0
*	Removed options for debug/release.
*	Renamed `$debug-mode` to `$debugging`.
*	Added `set-debug-level` function.
*   Added `decompile` function (diagnostics.k).
*   Fixed bug in compilation of `.a.b.c` and `?a.b.c` forms.
*   Removed chaining with `~` from `do` special form.
*   Added `chain` macro.
*   Added `:eval` and `:modify` debugger commands.

### 2017/07/28

*	Removed `multiple-let`, `multiple-var` and `merging-do`.
*	Added `with-multiple-let` and `with-multiple-var`.

### 2017/06/25

*	Added `fg` and `bg` commandline options.

### 2017/05/13

*	Added `#\single-quote`.

### 2017/04/17

*	Fixed conversion bug in property and indexer assigments.
*	Added `xwt.k` (open source gtk...wrapper)

### 2017/04/09

*	Removed `kiezellisp-repl` project (text windows).
*	Added `curses.k` (open source ncurses wrapper).
*	Improved code-completion in `kiezellisp-con`.

### 2017/03/23

*	Added transducers a la Clojure.
*	Added `transduce`, `dedupe`, `reduced`, `reduced?`, `unreduced`,
	`completing`, `eduction`, `sequence`, `cat`.
*	Renamed `count-if` to `count`.
*	Renamed `find-if` to `first`.
*	Renamed `position` to `index-of`.
*	Renamed `position-if` to `first-position`.
*	Removed `find-in-property-list`.
*	Renamed `collect` loop clause to `collecting`.
*	Similar change of `count`, `sum`, `minimize`, `maximize`, `multiply`.
*	Added `sort-by`, `merge-by`.
*	Renamed `min` to `minimize`.
*	Renamed `max` to `maximize`.
*	Added `min`, `max` reducer functions.

### 2016/12/09

*   Using `nameof(...)` feature.

### 2016/12/01

*   Changed TextForm construction.

### 2016/11/24

*   Fixed bug: not scrolling at end of buffer.

### 2016/11/22

*   Added CTRL+K, CTRL+U and CTRL+W readline editing key codes.

### 2016/11/20

*   Refactored everything ala StyleCop.
*   Fixed display of selection in readline editing code.

### 2016/11/17

*   Refactored readline editing code.

### 2016/11/16

*   Fixed CRLF vs LF bug on windows.

### 2016/11/15

*   Added `flatpak` feature.
*   Added `set-assembly-path` function.
*   Fixed code gen issues appearing in mono 4.6 and monodevelop 6.

### 2016/11/15

*   Reformatting by NArrange.

### 2016/11/14

*   Refactored `kiezellisp-gfx` into `kiezellisp-repl` and `kiezellisp-gui`.
*   Removed `terminal` class (`kiezellisp-repl`).
*   Changed `window` class to `text-window` (`kiezellisp-repl`).
*   Added `open-log`.
*   Added `show-log-window` and `hide-log-window` (`kiezellisp-repl`).
*   Changed `print` and `print-line`.

### 2016/10/24

*   Added intellisense tab expansion to kiezellisp-con.
*   Changed `help` from function to macro.
*   Added `man` macro.
*   Added `CTRL+C` to copy the REPL input line to the clipboard.
*   Added `CTRL+ENTER` to append a LF to the REPL input line.
*   Added `more` pager function.

### 2016/10/20

*   Added history to kiezellisp-con.
*   Fixed omission of executables in downloadable bin archive.
*   Added `interpose` sequence function.

### 2016/10/14

*   Refactored `CompilerHelpers` class.

### 2016/10/13

*   Renamed macro `tail-recursion` to special-form `prog`.
*   Made LispReader invisible: use TextReader instead of LispReader with
    Kiezellisp `read` functions.
*   Removed `open-lisp-reader`.

### 2016/10/09

*   Renamed `kiezellisp` to `kiezellisp-gfx`.
*   Added `kiezellisp-con`.
*   Added `repl` and `no-repl` commandline options.

### 2016/09/20

*   Refactored shell.k.

### 2016/08/27

*   Added `get-setf-expansion` and `define-modify-macro`.
*   Added `#<backquote>` reader macro.
*   Added `$macroexpand-hook`.

### 2016/07/23

*   Fixed bug: impossible to run with option `--release`.
*   Removed `tailcall`.
*   Added special form `recur` (tail recursion).
*   Added macro `tail-recursion`.
*   Added `&rawparams` parameter modifier.
*   Changed multi-arity functions from special form to macro.
*   Refactored `loop` macro.

### 2016/07/11

*   Refactored lisp reader.
*   Removed greek lambda.
*   Added function `undef`.
*   Added macros `psetq` and `psetf`.
*   Renamed `recur` to `self`.

### 2016/06/21

*   Added special form `define-symbol-macro`.
*   Added special form `let-symbol-macro`.
*   Added special form `let-macro`.
*   Added function `symbol-macro?`.
*   Added earmuff style `*var*` to special variables.

### 2016/06/06

*   Added accidentally omitted help files.
*   Fixed init.k script.

### 2016/05/29

*   Undid changes to CollectParameterInfo() diagnostic code.
*   Removed all occurrences of Cons.EMPTY.
*   Fixed bug in Prototype.Apply.

### 2016/05/24

*   Refactored prototype implementation.

### 2016/05/23

*   Fixed documentation errors.
*   Removed prototype getter mechanism.

### 2016/05/12

*   Fixed stack overflow bug caused by recursion in prototype getters
    (lambda-valued members).

### 2016/03/23

*   Update cursor position after entering a REPL command.

### 2016/01/28

*   Changed most files due to conversion to monodevelop syntax formatter.
*   Changed `case` and `ecase` macros to evaluate the the cases.
*   Fixed pretty printing of `case` and `cond` forms.
*   REPL smart parentheses feature for lines **not** starting with a space.
*   Redesign of REPL.
*   Redesign of `man` and `help` functions.
*   Added `terminal` package with curses like window functions.
*   Added `about` package with additional help topics.

### 2015/07/28

*   Added `#if`, `#elif`, `#else` and `#endif` conditional feature to lisp reader.
*   Added `#ignore` to lisp reader.

### 2015/07/22

*   Fixed online help on linux/windows.

### 2015/07/15

*   Fixed bug in `ImportMissingSymbol` caused by some underscored names.
*   Added unsigned integer support.

### 2015/07/13

*   Added `w` option to regular expression literal to support wildcard patterns.
*   Regular expression literal is also a function.
*   Removed double back tick string interpolation marker.
*   Added `lib/pandoc.k` and `lib\commonmark.k`.

### 2015/07/03

*   Added `lisp` package to the use-list of the `csv` package.

### 2015/06/23

*   Refactored symbol/package lookup/creation.

### 2015/06/11

*   Moved backquote/quasiquote expansion from compiler back to reader.
*   Added `$repl-force-it` variable.

### 2015/05/20

*   Removed `--listener` command line option.
*   Added `start-listener` function.
*   Undid removal of REPL smart parentheses feature.
*   Modified Ctrl+Enter to suppress REPL smart parentheses feature.
*   Removed function `unuse-package`.

### 2015/05/17

*   Fixed implicit conversion bug.
*   Modified `describe` for compiler macro feature.

### 2015/05/16

*   Added `define-compiler-macro`.
*   Removed REPL smart parentheses feature.

### 2015/05/01

*   Added warning when shadowing symbol from inherited package.
*   Changed application of `UnwindException`.

### 2015/04/27

*   Fixed bug in `merging-do` compilation.
*   Separated namespaces for specialforms/macros and functions/variables.

### 2015/04/26

*   Changed implementation of documentation functions.

### 2015/04/17

*   Changed `#"..."` regex literal to `#/.../`.
*   Added `defclass` and `defstruct` macros.
*   Added `#s(...)` reader macro.
*   Added `#v(...)` reader macro.
*   Fixed (rare) bug in `loop` macro expansion.
*   Added `$print-vector-with-brackets`.
*   Added `$print-prototype-with-braces`.

### 2015/03/16

*   Added `assoc` and `assoc-if` functions.
*   Added `[STAThread]` attributes that went missing.

### 2015/03/15

*   Changed the name of `kiezellisp-init` to be a constant.

### 2015/03/12

*   Simplified `EmbeddedMode` class.
*   Added `math::random` function.

### 2015/03/06

*   Added `EmbeddedMode` feature.

### 2015/02/21

*   Added `lambda*` and `defun*` defining multi-arity functions.
*   Fixed `recur` bug.

### 2015/01/08

*   Better handling of numeric conversions.
*   Fixed extension handling in `reference` function.
*   Fixed bug in `shell.k`.
*   Added `~/` expansion in strings.
*   Added `${...}` expansion in strings.

### 2014/12/28

*   Changed all files to LF instead of CRLF line endings.
*   Fixed warnings detected by mono compiler.
*   Added `$home` and other variables to `shell.k`.
*   Added function `shell:expand-path-name` to expand path names
    starting with a tilde.

### 2014/12/22

*   Renamed `:windows-nt` feature to `:windows`.
*   Renamed `:windows-mode` feature to `:graphical-mode`.
*   Fixed bug in `shell::enclose-in-quotes`.

### 2014/12/19

*   Fixed bug using PATH environment variable on unix.
*   Changed location of DLR dlls to be in the src tree.

### 2014/12/07

*   Added `IApply` interface to `Symbol`.
*   Pretty print extensions for Kiezellisp IDE (in progress).
*   Moved backquote/quasiquote expansion from reader to compiler.
*   Fixed bug in function call to `>` operator with three or more arguments.

### 2014/11/21

*   Renamed `skip` to `drop`.
*   Changed `map` to support multiple sequences.
*   Added `remove`, `map-indexed`, `keep`, `keep-indexed`.
*   Added `reductions`, `cycle`, `repeatedly`, `iterate`.
*   Refactored method selection code.
*   Changed `multiple-var` etc from special form to macro.
*   Added `complement`.
*   Fixed bug in code of array/indexer assignment.

### 2014/09/07

*   Added `multiple-var`.
*   Added `multiple-let`.
*   Added `multiple-setf`.

### 2014/08/18

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


### 2014/06/16

*   Added `var` and `let` declarations to file scope.
*   REPL expression does not need parentheses if first term is a function or
    a special form.
*   Added `block`, `return-from` for named blocks.
*   Added `$package-name-prefix` special variable.

### 2014/05/29

*   Merged `tagbody` into `do` to improve loop performance. `do` forms that
    are keywords are interpreted as labels.
*   Removed `tagbody`.
*   Renamed `go` to `goto`.


### 2014/05/24

*   Fixed bug in handling of function call with too few arguments.
*   Changed `#(...)` lambda expression to be compatible with clojure's.
*   Added sequence functions `repeatedly`, `iterate` and `cycle`.

### 2014/05/12

*   Added `ecase`, `etypecase` and `ecase-match` macros.
*   Added `loop` clauses like `(for i :from a :to b)`.
*   Added `series-enumerator`.

### 2014/05/08

*   Added `(lazy sym expr))`.
*   Added `(future sym expr))`.
*   Fixed bug: infinite loop compiling call to multi-method with `null` eql
    specifier.

### 2014/04/15

*   Removed `$strict` and `strict`.

### 2014/04/08

*   REPL expression does not need parentheses if first term is a function.
*   Renamed `$quick-import` to `$lazy-import`.
*   Added accessor chaining feature: `(.name.to-upper obj)`
*   Symbols generated by `gentemp` are now placed in the `temp` package.
*   Deimplemented uninterned symbols.
*   Fixed bug in code generated for lambda derived delegate with value type
    parameters.
*   Rewrote parser/scanner to use readtables.
*   Removed function `make-reader` in favor of `lisp-reader:new`.

### 2014/03/03

*   BREAKING CHANGE: changed `try` to `ignore-errors`; introduced `try` with
    `catch` and `finally` clauses.

### 2014/02/22

*   BREAKING CHANGE: using colon and double colon package-symbol separators
    instead of period and exclamation mark.

### 2014/02/02

*   Removed `$print-max-elements`.

### 2014/01/21

*   Fixed bug where `then` and `else` branch of `if` yielded different types.
*   Fixed bug where a prototype getter function was called on the wrong object.
*   Added `&whole` parameter modifier.
*   Added `--listener` command line option.

### 2014/01/20

*   Fixed numeric arguments conversion bugs.

### 2014/01/03

*   Added DLR restrictions for calls to multi-methods.
*   Fixed bug in `reduce`.
*   Added `mapcat`.
*   Added `conjoin`.
*   Added `r/map`, `r/mapcat`, `r/filter`, `r/take`, `r/take-while`.
*   Added `$print-max-elements`.
*   Changed Ctrl+ENTER action in the REPL.

### 2013/11/29

*   Renamed `set-package-alias` to `use-package-alias`.
*   Restricted use of parameter modifiers such as `&key` to just one in
    order to optimize calls to lisp functions.
*   Removed the `&whole` modifier.
*   Extended `<=` and other relational parameters to have two or more arguments.

### 2013/11/14

*   Replaced `#{...}` string interpolation markers with `double-back-tick...double-back-tick`.

### 2013/11/04

*   Fixed bug in `json-decode`.

### 2013/10/21

*   Refactored `console.cs`.
*   Added `-init` command line option.
*   Added `exit` function.

### 2013/10/18

*   Added variable `$quick-import`.
*   Other optimizations.
*   Added `winforms.k`.

### 2013/10/04

*   Added `(declare (ignore var))`.
*   `DataSource` support for prototype objects.
*   Added `(as-vector seq type)`.
*   Changed `Vector` type to `List<object>`.
*   Fixed bug in code for constructing value types.
*   Removed `add-event-handler`.
*   Support for adding event handlers through `setf`.

### 2013/09/17

*   Fixed bug `loop` macro.
*   Added delta parameter to `incf` and `decf`.
*   Added some unit tests.
*   Added `as-tuple`.

### 2013/09/16

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

### 2013/08/19

*   Added prototype as a function feature.
*   Changed member accessor function '.' to have string argument only.
*   Added `elt` support for lists.
*   Added function `set-read-decimal-numbers(flag)` for better interfacing
    with math libraries.
*   Fixed error in codegen of index binders.
*   Allow any key type in prototype objects.

### 2013/08/02

*   Fixed spelling of external dll names for case-sensitive OS.
*   Fixed error in usage of `as-array` function.

### 2013/07/30

*   Initial release.    
