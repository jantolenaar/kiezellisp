(defun f (n) (+ n 3))
;; Define a simple and correct function.
<enter>
(f 5)
;; Call the function with the correct argument.
<enter>
(f "oops")
;; Call the function with an incorrect argument.
<enter>
;; We get an error message and enter the debugger.
:b
;; The :b command prints a stack trace of the active lisp forms.
<enter>
:E
;; The :E command prints a full .NET stack trace.
<enter>
:e
;; The :e command prints a reduced .NET stack trace.
<enter>
:v
;; The :v command prints the currently visible lexical variables.
<enter>
:a
;; The :a command aborts the most recent debugger prompt.
<enter>
;; This error was easy to spot. When you are not sure, 
;; set a breakpoint in the function source code.
(defun f (n) (breakpoint) (+ n 3))
<enter>
(f "oops")
<enter>
:v
<enter>
(setf n 5)
;; Fix the error
<enter>
:v
;; Is it fixed?
<enter>
:continue
;; Continue from the breakpoint
<enter>

