
;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

(defpackage "lisp"
    (:export 
        "documentation"
        "help"
        "set-online-help"
    ))

(in-package "lisp")

(def $website "http://www.kiezellisp.nl")

#+windows
(def $browser null)

#+unix
(def $browser "firefox")

(defun set-online-help (browser url)
    (setf $browser browser)
    (setf $website url))


(defun help (&optional topic)
    (if topic
          (print-online-help topic)
        (print-console-help))
    (void))

(defun print-online-help (topic)
    (assert (symbol? topic))
    (let $package (find-package "lisp"))
    (let url (path:combine $website (string "api.html?#" topic)))
    (help-run-browser url)
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

(defun help-run-browser (url)
    (let proc (process:new))
    (let info (.start-info proc))
    (cond 
        ($browser
            (setf (.file-name info) $browser)
            (setf (.redirect-standard-output info) true)
            (setf (.redirect-standard-error info) true)
            (setf (.redirect-standard-input info) true)
            (setf (.use-shell-execute info) false)
            (setf (.arguments info) (enclose-in-quotes url)))
        (true
            (setf (.file-name info) url)
            (setf (.redirect-standard-output info) false)
            (setf (.redirect-standard-error info) false)
            (setf (.redirect-standard-input info) false)
            (setf (.arguments info) "")
            (setf (.use-shell-execute info) true)))
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

    help                    Shows this help text.
    help 'car               Shows online help for the symbol `car`.
    help 'help              Shows online help for the symbol `help`.
    help 'repl<TAB>         Shows online help for `repl` after TAB completion.
    help 'kiezel<TAB>       Shows online help for `kiezellisp` after TAB completion.
    documentation           Shows this help text.
    documentation 'car      Shows summary description for the symbol `car`.
""")

(make-symbol "repl:commands")
(make-symbol "keyboard-shortcuts")
(make-symbol "kiezellisp:command-line-options")


