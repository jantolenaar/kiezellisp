
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
    `(show-man ,(enquote topic)))

(defun enquote(topic)
    (if (cons? topic)
        topic
        (list 'quote topic)))

#if kiezellisp-repl

(defun show-help (topic)
    (task
        (show-help-task topic true))
    (void))

(defun make-documentation-window ()
    (text-window:open
        :on-close text-window:send-interrupt-key
        :caption "Kiezellisp Help"
        :buffer-height 999))

(defun fill-documentation-window (win text)
    (with-window-suspended (win)
        (.resize-buffer win (.buffer-width win) 999)
        (.set-cursor-pos win 0)
        (.clear-to-bot win)
        (print-line win (transform-page-gui win text))
        (print-line win (string:repeat "-" (.buffer-width win)))
        (.resize-buffer win (.buffer-width win) (.buffer-height-used win))
        (.set-window-pos win 0 0)
        (.set-cursor-pos win 0)))

(defun show-help-task (topic)
    (let win (make-documentation-window))
    (var topics (vector))
    (var index -1)
    (letfun goto-new-topic (topic)
        (when topic
            (let text (get-documentation topic))
            (incf index)
            (.remove-range topics index (- (.count topics) index))
            (.add topics (list topic text))
            (fill-documentation-window win text)))
    (letfun command-key? (ch)
        (or (= ch #\slash)
            (= ch #\colon)))
    (letfun escape-key? (key)
        (or (= key keys:ESCAPE)
            (and (= key keys:BACK)
                 (zero? index))))
    (letfun back-key? (key)
        (or (= key keys:BACK)
            (= key keys:LEFT)))
    (letfun forward-key? (key)
        (or (= key keys:RIGHT)
            (= key keys:ENTER)))
    (letfun click-key? (key)
        (= key keys:LBUTTON))
    (goto-new-topic topic)
    (loop
        (let info (.read-key win))
        (let key (.key-data info))
        (let ch (.key-char info))
        (let clicks (.mouse-clicks info))
        (let x (.mouse-col info))
        (let y (.mouse-row info))
        (cond
            ((command-key? ch)
                (goto-new-topic (command-popup win "topic: ")))
            ((escape-key? key)
                (.close win)
                (return))
            ((forward-key? key)
                (when (< (inc index) (.count topics))
                    (incf index)
                    (fill-documentation-window win (second (elt topics index)))))
            ((back-key? key)
                (when (> index 0)
                    (decf index)
                    (fill-documentation-window win (second (elt topics index)))))
            ((and (click-key? key)
                  (> clicks 1))
                (goto-new-topic (.scrape-lisp-word-at win x y)))

                )))

(defun command-popup (win prompt)
    (let w (.buffer-width win))
    (let y (+ (.window-top win) (.window-height win) -2))
    (let saved (.copy-buffer win))
    (.set-cursor-top win y)
    (.set-cursor-left win 0)
    (.clear-to-bot win)
    (.write win (string:repeat "=" w))
    (.write win prompt)
    (let str (.read win :max-chars (- w 1 (.length prompt))
                        :code-completion true))
    (.paste win 0 0 saved)
    (when (not (null? str))
        (return (.trim str))))

#endif

#if kiezellisp-con

(defun show-help (topic)
    (show-man topic))

#endif

(defun show-man (topic)
    (let doc (get-documentation topic))
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
    (do
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
    (when (find usage usage-list-for-variables)
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

#if kiezellisp-repl

(defun transform-page-gui (win text)
    (letfun transform-line-gui (text)
        (let bold-pattern1 #/\*\*(\S*?)\*\*/)
        (let bold-pattern2 #/__(\S*?)__/)
        (let italic-pattern1 #/\*(\S*?)\*/)
        (let italic-pattern2 #/_(\S*?)_/)
        (let code-pattern #/`(\S*?)`/)
        (let b (.format-html win "b" @"\1"))
        (let i (.format-html win "i" @"\1"))
        (let ui (.format-html win "u" i))
        (do
            text
            (.regex-replace ~ bold-pattern1 b)
            (.regex-replace ~ bold-pattern2 b)
            (.regex-replace ~ italic-pattern1 i)
            (.regex-replace ~ italic-pattern2 i)
            (.regex-replace ~ code-pattern ui)
            ))
    (letfun bold-gui (text)
        (.format-html win "b" text))
    (let i (.format-html win "i" "x"))
    (let j (.index-of i "x"))
    (let i1 (.substring i 0 j))
    (let i2 (.substring i (inc j)))
    (transform-page-base text bold-gui i1 i2 transform-line-gui))

#endif

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
                    (collect (if inside-code
                                (string italic-on "<delete-lf>")
                                (string italic-off "<delete-lf>"))))
                (inside-code
                    (collect (string (margin) line)))
                ((.starts-with line "### ")
                    (let text (.trim (.substring line 3)))
                    (collect (string (margin) (bold text)))
                    (collect ""))
                ((.starts-with line "## ")
                    (let text (.to-upper (.trim (.substring line 2))))
                    (collect (string (margin 1) (bold text))))
                ((.starts-with line "# ")
                    (let text (.to-upper (.trim (.substring line 1))))
                    (collect (bold text)))
                (true
                    (collect (string (margin) (transform-line line)))))))
    (do
        (string:join "\n" result)
        (string:replace ~ "<delete-lf>\n" "")))

(defun transform-line (text)
    (let bold-pattern1 #/\*\*(\S*?)\*\*/)
    (let bold-pattern2 #/__(\S*?)__/)
    (let italic-pattern1 #/\*(\S*?)\*/)
    (let italic-pattern2 #/_(\S*?)_/)
    ;(let code-pattern #/`(\S*?)`/)
    (do
        text
        (.regex-replace ~ bold-pattern1 @"\1")
        (.regex-replace ~ bold-pattern2 @"\1")
        (.regex-replace ~ italic-pattern1 @"\1")
        (.regex-replace ~ italic-pattern2 @"\1")
        ;(.regex-replace ~ code-pattern @"\1")
        ))

(defun transform-page (text)
    (transform-page-base text identity "" "" transform-line))
