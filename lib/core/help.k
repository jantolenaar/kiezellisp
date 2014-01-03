
;;;; Copyright (C) 2012-2014 Jan Tolenaar. See the file LICENSE for details.

(defpackage "lisp"
    (:export 
        "help"
        "import-documentation"
        "export-documentation"))

(in-package "lisp")

(defun help (topic)
"""
Prints information attached to the global symbol `topic`.
"""    
    (try
        (var $package (get-package "example"))
        (if topic
            (do
                (var descr (get-description topic))
                (var syntax-list (.function-syntax descr))
                ;; make clusters of text and examples so that examples can be eval-ed in a DO block.
                (var doc-list (partition-by string? (.documentation descr)))
                (var type (.usage descr))
                (print-line (.to-upper (string type)))
                (print-line)
                (when (find type '(variable readonly-variable constant special-variable special-readonly-variable special-constant))
                    (print-line "    " (.name descr))
                    (print-line))
                (when syntax-list
                    (each show-syntax syntax-list)
                    (print-line))
                (when doc-list
                    ;;(print-line "DESCRIPTION")
                    ;;(print-line)
                    (each show-doc doc-list))
                )
            (do
                (show-console-help)))))

(setq $help-hook help)

(defun show-syntax (syn)
    (print "    ")
    (write-line syn :escape false))

(defun show-doc (doc)
    (if (string? (first doc))
        (show-strings doc)
        (show-examples doc)))

(defun show-strings (items)
    (each show-string items))

(defun show-string (item)
    ; rewrite markdown html links []() and [][]
    (var item1 item)
    (var item2 (.regex-replace item1 #"\[(.*?)\]\(.*?\)" @"`\1`"))
    (var item3 (.regex-replace item2 #"\[(.*?)\]\[.*?\]" @"`\1`"))
    (loop 
        (for part :in (.split item3 "`"))
        (for standout :initially false :then (not standout))
        (write part :escape false 
                    :color (if standout $standout-color)
                    :background-color (if standout $standout-background-color)))
    (print-line)
    (print-line))

(defun make-prompt (prompt-nr dbglevel)
    (with-output-to-string ($stdout)
        (print (.name $package) " " prompt-nr)
        (if (> dbglevel 0)
            (print " " dbglevel))
        (print " > ")))

(defun show-examples (examples &key add-terminating-prompt)
    (var content (print-examples-to-string examples add-terminating-prompt))
    (print-line (.indent content "    ")))

(defun print-examples-to-string (examples add-terminating-prompt)
    (var $package $package)
    (var env (make-environment))
    (var prompt-nr 1)
    (var dbglevel 0)
    (with-output-to-string ($stdout)
        (loop
            (for example in examples)
            (var prompt (make-prompt prompt-nr dbglevel))
            (print prompt)
            (var example-code null)
            (cond 
                ((and (cons? example)
                      (= (string (first example)) "EXAMPLE"))
                    (var code (apply string (rest example)))
                    (var $print-escape false)
                    (pprint code :left (length prompt))
                    (setf example-code (read-from-string code)))
                (true
                    (pprint example :left (length prompt))
                    (setf example-code example)))
            (print-line)
            (case example
                (:a
                    (assert (< 0 dbglevel))
                    (decf dbglevel))
                (:t
                    (assert (< 0 dbglevel))
                    (setf dbglevel 0))
                (:d 
                    (describe it))
                (true
                    (incf prompt-nr)
                    (var result (do
                                    (var $enable-warnings false)
                                    (var $stdlog $stdout)
                                    (try-and-catch (force (eval example-code env)))))
                    (cond
                        ((exception? result)
                            (print-line (.message (.inner-exception result)))
                            (incf dbglevel))
                        (true
                            (setf it result)
                            (print "it: ")
                            (pprint result :left 4))))
                )
            (print-line))
        (if add-terminating-prompt
            (print (make-prompt prompt-nr dbglevel)))))

(defun import-documentation (topics)
    (each import-symbol-documentation topics))

(defun import-symbol-documentation (topic)
    (var name (first topic))
    (var syntax-list (vector))
    (var doc-list null)
    (loop 
        (for parts :on-list (rest topic))
        (var part (first parts))
        (cond
            ((string? part)
                (setf doc-list parts)
                (break))
            (true 
                (.add syntax-list (cons name part)))))
    (setf (symbol-function-syntax name) syntax-list)
    (setf (symbol-documentation name) doc-list))

(def exportable-usage-list '(
    special-variable
    special-readonly-variable
    special-constant
    function
    special-form
    macro
    method
    generic-function
    builtin-function
    builtin-constructor
    ))

(def exportable-symbol-list '(
    recur
    ))

(defun exportable (descr)
    (or
        (find (.usage descr) exportable-usage-list)
        (find (.symbol descr) exportable-symbol-list)))

(defun export-documentation (file-name &rest package-names)
    ;(var $print-short-symbol-names true)
    (var data (with-output-to-string ($stdout)
        (print-line)
        (print-line "(in-package \"lisp\")")
        (print-line)
        (print-line "(import-documentation '(")
        (print-line)
        (map list-exported-symbols package-names)
        (flatten ~)
        (sort ~)
        (map get-description ~)
        (filter exportable ~)
        (each export-symbol-documentation ~)
        (print-line "))")
        (print-line)))
    (file.write-all-text file-name data))

(defun readable-list (x)
    (if x
        (write-to-string x :escape true)
        "()"))

(defun export-symbol-documentation (description)
    ;(var name (.name description))
    ;(var type (.usage description))
    (var sym (.symbol description))
    (var doc (or (.documentation description)
                 (list "\nTO DO\n")))
    (var syntax (.function-syntax description))
    (var leading (string "(" sym " "))
    (print leading)
    (when syntax
        (print (readable-list (rest (car syntax))))
        (foreach (item (cdr syntax))
            (print-line)
            (write "" :escape false :width (.length leading))
            (print (readable-list (rest item)))))
    (print-line)   
    (foreach (item doc)
        (if (string? item)
            (do
                (var str (.trim item))
                (print-line "\"\"\"")
                (print-line str)
                (print-line "\"\"\""))
            (do
                (write-line item :pretty true :escape true))))
    (print-line ")")
    (print-line)
    (print-line #\semicolon "------------------------------------------------------------------------------" )
    (print-line))

(defun show-console-help ()
    (print
"""
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
? [symbol]              Show general help or specific syntax and documentation.
:help [symbol]          Show general help or specific syntax and documentation.
:globals [pattern]      Show global variables that contain the pattern.
:history                Show current history.
:init                   Restart the interpreter.
:Init                   Restart the interpreter (verbose).
:time expr              Show elapsed time (compilation+execution).
:quit                   Quit program.

Debugger top-level commands:

:abort                  Abort to previous level.
:backtrace              Show evaluation stack.
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
Ctrl+Enter              Make a complete function call and execute.
Ctrl+LeftArrow          Move to left word.
Ctrl+RightArrow         Move to right word.
Ctrl+BackSpace          Delete left word.
Ctrl+Delete             Delete right word.
Ctrl+Shift+C            Copy the pretty printed value of <it> to the clipboard.
UpArrow                 Move up in history.
DownArrow               Move down in history.
"""))

