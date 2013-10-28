# Change Log

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