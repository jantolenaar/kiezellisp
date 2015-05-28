
;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

(defpackage "lisp"
    (:export 
        "documentation"
        "help"
        "set-online-help-url"
    ))

(in-package "lisp")

(def website "http://www.kiezellisp.nl")

(defun set-online-help-url (str)
    (setf website str))

(defun help (&optional topic)
    (if topic
          (print-online-help topic)
        (print-console-help))
    (void))

(defun print-online-help (topic)
    (assert (symbol? topic))
    (let $package (find-package "lisp"))
    (let url (path:combine website (string "api.html?#" topic)))
    (help-run-windows-command url)
    (void))

(defun documentation (&optional topic)
    (if topic
          (print-topic-documentation topic)
        (print-console-help))
    (void))

(def usage-list-for-variables '(
    variable 
    readonly-variable 
    constant 
    special-variable 
    special-readonly-variable 
    special-constant))

(defun print-topic-documentation (topic)
    (let descr (get-description topic))
    (let compiler-documentation (.compiler-documentation descr))
    (let compiler-usage (.compiler-usage descr))
    (let compiler-syntax-list (.compiler-syntax descr))
    (when compiler-usage
        (print-line "COMPILER-USAGE")
        (print-line "    " compiler-usage))
    (when compiler-syntax-list
        (print-line "COMPILER-SYNTAX")
        (each print-syntax compiler-syntax-list))
    (when compiler-documentation
        (print-line "COMPILER-DESCRIPTION")
        (print-documentation-item compiler-documentation))
    (let documentation (.documentation descr))
    (let usage (.usage descr))
    (let syntax-list (.function-syntax descr))
    (when usage
        (print-line "USAGE")
        (print-line "    " usage))
    (when (find usage usage-list-for-variables)
        (print-line "SYNTAX")
        (print-line "    " (.name descr)))
    (when syntax-list
        (print-line "SYNTAX")
        (each print-syntax syntax-list))
    (when documentation
        (print-line "DESCRIPTION")
        (print-documentation-item documentation))
    (print-line))

(defun help-run-windows-command (cmd &rest arguments)
    (let proc (process:new))
    (let info (.start-info proc))
    (setf (.file-name info) cmd)
    (setf (.redirect-standard-output info) false)
    (setf (.redirect-standard-error info) false)
    (setf (.redirect-standard-input info) false)
    (setf (.use-shell-execute info) true)
    (setf (.arguments info) (make-safe-argument-string arguments))
    (.start proc)
    (void))

(defun enclose-in-quotes (arg)
    (cond
        ((null? arg)
            "")
        ((find #\double-quote arg)
            (string "\"" arg "\""))
        ((find #\space arg)
            (string "\"" arg "\""))
        (true
            arg)))

(defun make-safe-argument-string (args)
    (string:join " " (map enclose-in-quotes args)))

(defun print-syntax (syn)
    (print "    ")
    (write-line syn :escape false))

(defun print-documentation-item (item)
    ; rewrite markdown html links []() and [][]
    (let item1 (.trim item "\r\n"))
    (let item2 (.regex-replace item1 #/\[(.*?)\]\(.*?\)" @"`\1`/))
    (let item3 (.regex-replace item2 #/\[(.*?)\]\[.*?\]" @"`\1`/))
    (write-line item3 :escape false))


(defun print-console-help ()
    (print console-help))

(def console-help        
"""
Help examples:

    help               Shows this help text.
    help 'car          Shows online help for the symbol `car`.
    documentation      Shows this help text.
    documentation 'car Shows summary description for the symbol `car`.

Command line options:

-c --command <exprs>    Evaluates <exprs> as a DO block and exits.
-i --init <exprs>       Evaluates <exprs> as a DO block then REPL.
-d --debug              Slower code and better debugging.
-n --nodebug            Faster code and worse debugging.
-o --optimize           Optimize expressions with constants.

REPL input:

Lines starting with a colon are top-level commands,
which may be abbreviated. Other lines are lisp expressions.

General top-level commands:

:clear                  Clear screen and history.
:describe [expr]        Describe expression or last result.
:force [expr]           Force lazy expression or last result.
:globals [pattern]      Show global variables that contain the pattern.
:history                Show current history.
:reset                  Restart the interpreter.
:Reset                  Restart the interpreter (verbose).
:time expr              Show elapsed time (compilation+execution).
:quit                   Quit program.

Debugger top-level commands:

:abort                  Abort to previous level or abort after a breakpoint.
:backtrace              Show evaluation stack.
:continue               Continue the program after a breakpoint.
:exception              Print latest exception without DLR stuff.
:Exception              Print latest exception.
:variables [n]          Print lexical variables at depth n of the evaluation stack.
:$variables [n]         Print dynamic variables at depth n of the evaluation stack.
:top                    Abort to top level.

Non-trivial Editor Keys:

Enter                   Execute input when it is a complete lisp expression, otherwise 
                        continue editing.
Tab                     Autocomplete symbol.
Escape                  Cancels input or tab completion.
Ctrl+Enter              Execute without adding smart parentheses.
Ctrl+LeftArrow          Move to left word.
Ctrl+RightArrow         Move to right word.
Ctrl+BackSpace          Delete left word.
Ctrl+Delete             Delete right word.
Ctrl+Shift+C            Copy the pretty printed value of <it> to the clipboard.
UpArrow                 Move up in history.
DownArrow               Move down in history.
""")

