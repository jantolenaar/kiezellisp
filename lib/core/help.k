
;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

(defpackage "lisp"
    (:export
        "help"
        "man"
        "set-help-path"
        "$help-path"
    ))

(in-package "lisp")

(def $help-path null)

(defun set-help-path (&rest dirs)
    (let transformer (compose path:get-unix-name path:get-full-path))
    (setf $help-path (as-list (map transformer dirs)))
    (make-help-symbols "about"))

(defun make-help-symbols (target)
    (loop
        (for name in $help-path)
        (let folder (path:combine name target))
        (when (directory:exists folder)
            (let files (directory:get-files folder))
            (each #(make-symbol target
                                ":"
                                (path:get-file-name-without-extension %1))
                          files))))

(defmacro help (&optional (topic 'help))
    `(show-help ,(enquote topic)))

(defmacro man (&optional (topic 'man))
    `(show-help ,(enquote topic)))

(defun enquote(topic)
    (if (cons? topic)
        topic
        (list 'quote topic)))

(defun show-help (topic)
    (let doc (get-documentation topic))
    (print "\f")
    (more (transform-page doc))
    (void))

(setf $help-hook show-help)

(def usage-list-for-variables '(
    variable
    readonly-variable
    constant
    special-variable
    special-readonly-variable
    special-constant))

(defun transform-topic-to-path-name (topic)
    (chain (~)
        (.long-name topic)
        (.replace ~ "$" "(dollar)")
        (.replace ~ "?" "(qm)")
        (.replace ~ "." "(dot)")
        (.replace ~ ">" "(gt)")
        (.replace ~ "<" "(lt)")
        (.replace ~ "/" "(slash)")
        (.replace ~ "*" "(star)")
        (.replace ~ ":" "/")))

(defun get-documentation (topic)
    (let target (if (symbol? topic)
                      topic
                    (make-symbol topic)))
    (let text (or (get-documentation-from-file target)
                  (get-no-documentation target)))
    text)

(defun get-documentation-from-file (topic)
    (let name (transform-topic-to-path-name topic))
    (let files (list (string name ".txt") (string name ".md")))
    (let path (find-one-of-source-files files $help-path))
    (if path
        (file:read-all-text path)))

(defun margin (&optional (depth 2))
    (string:repeat "    " depth))

(defun get-no-documentation (topic)
    (with-output-to-string ($stdout)
        (print-line "# NAME")
        (print-line (margin) topic)
        (print-line)
        (print-line "# DESCRIPTION")
        (print-line (.indent "There is no information available about this topic." (margin)))
        (print-line)))

(defun get-runtime-documentation (topic)
    (with-output-to-string ($stdout)
        (print-runtime-documentation topic)))

(defun print-runtime-documentation (topic)
    (let descr (get-description topic))
    (let name (.name descr))
    (let compiler-documentation (.compiler-documentation descr))
    (let compiler-usage (.compiler-usage descr))
    (let compiler-syntax-list (.compiler-syntax descr))
    (let documentation (.documentation descr))
    (let usage (.usage descr))
    (let syntax-list (.function-syntax descr))
    (let value (.value descr))
    (let type (.type descr))
    (when name
        (print-line "# NAME")
        (print-line (margin) name)
        (print-line))
    (when compiler-usage
        (print-line "# COMPILER-USAGE")
        (print-line (margin) compiler-usage)
        (print-line))
    (when compiler-syntax-list
        (print-line "# COMPILER-SYNTAX")
        (each print-syntax compiler-syntax-list)
        (print-line))
    (when compiler-documentation
        (print-line "# COMPILER-DESCRIPTION")
        (print-line (.indent compiler-documentation (margin)))
        (print-line))
    (when usage
        (print-line "# USAGE")
        (print-line (margin) usage)
        (print-line))
    (when (index-of usage usage-list-for-variables)
        (print-line "# SYNTAX")
        (print-line (margin) (.name descr))
        (print-line))
    (when syntax-list
        (print-line "# SYNTAX")
        (each print-syntax syntax-list)
        (print-line))
    (when documentation
        (print-line "# DESCRIPTION")
        (print-line (.indent documentation (margin)))
        (print-line))
    (when value
        (print-line "# VALUE")
        (print (margin))
        (write-line value :pretty true :left (length (margin)))
        (print-line))
    (when type
        (print-line "# TYPE")
        (print-line (margin) type)
        (print-line))

    (print-line))

(defun print-syntax (syn)
    (print (margin))
    (write-line syn :escape false))

(defun transform-page-base (text bold italic-on italic-off transform-line)
    (let lines (string:split text "\n"))
    (let result
        (loop
            (initially
                (var inside-code false))
            (for line :in lines)
            (cond
                ((.starts-with line "~~~")
                    (setf inside-code (not inside-code))
                    (collecting (if inside-code
                                (string italic-on "<delete-lf>")
                                (string italic-off "<delete-lf>"))))
                (inside-code
                    (collecting (string (margin) line)))
                ((.starts-with line "### ")
                    (let text (.trim (.substring line 3)))
                    (collecting (string (margin) (bold text)))
                    (collecting ""))
                ((.starts-with line "## ")
                    (let text (.to-upper (.trim (.substring line 2))))
                    (collecting (string (margin 1) (bold text))))
                ((.starts-with line "# ")
                    (let text (.to-upper (.trim (.substring line 1))))
                    (collecting (bold text)))
                (true
                    (collecting (string (margin) (transform-line line)))))))
    (chain (~)
        (string:join "\n" result)
        (string:replace ~ "<delete-lf>\n" "")))

(defun transform-line (text)
    (let bold-pattern1 #/\*\*(\S*?)\*\*/)
    (let bold-pattern2 #/__(\S*?)__/)
    (let italic-pattern1 #/\*(\S*?)\*/)
    (let italic-pattern2 #/_(\S*?)_/)
    ;(let code-pattern #/`(\S*?)`/)
    (chain (~)
        text
        (.regex-replace ~ bold-pattern1 @"\1")
        (.regex-replace ~ bold-pattern2 @"\1")
        (.regex-replace ~ italic-pattern1 @"\1")
        (.regex-replace ~ italic-pattern2 @"\1")
        ;(.regex-replace ~ code-pattern @"\1")
        ))

(defun transform-page (text)
    (transform-page-base text identity "" "" transform-line))
