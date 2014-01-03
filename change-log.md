# Change Log

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