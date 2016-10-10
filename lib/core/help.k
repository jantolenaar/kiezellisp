
;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

(defpackage "lisp"
    (:export
        "help"
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

#if graphical-mode

(defun make-documentation-window (topic)
    (let $stdout (window:open :buffer-height 999
                              :tab-completion-enabled true
                              :html-enabled true))
    (print-line (get-documentation topic))
    (print-line (string:repeat "-" (terminal:width)))
    (.set-buffer-pos $stdout 0 0)
    $stdout)

(defun help (&optional (topic 'help))
    (var win (make-documentation-window topic))
    (var topics (list win))
    (var index 0)
    (letfun goto-new-topic (topic)
        (when topic
            (while (> index 0)
                (window:close (first topics))
                (pop topics)
                (decf index))
            (setf win (make-documentation-window topic))
            (push win topics)))
    (letfun command-key? (ch)
        (or (= ch #\slash)
            (= ch #\colon)))
    (letfun escape-key? (key)
        (or (= key terminal-keys:ESCAPE)
            (and (= key terminal-keys:BACK)
                 (null? (nth (inc index) topics)))))
    (letfun back-key? (key)
        (or (= key terminal-keys:BACK)
            (= key terminal-keys:LEFT)))
    (letfun forward-key? (key)
        (or (= key terminal-keys:RIGHT)
            (= key terminal-keys:ENTER)))
    (letfun click-key? (key)
        (= key terminal-keys:LBUTTON))
    (try
        (loop
            (.bring-to-top win)
            (let info (.read-key win))
            (let key (.key-data info))
            (let ch (.key-char info))
            (let clicks (.mouse-clicks info))
            (let x (.mouse-col info))
            (let y (.mouse-row info))
            ;(breakpoint)
            (cond
                ((escape-key? key)
                    (return))
                ((command-key? ch)
                    (goto-new-topic (command-popup win "topic: ")))
                ((forward-key? key)
                    (when-let (right-win (nth (dec index) topics))
                        (decf index)
                        (setf win right-win)))
                ((back-key? key)
                    (when-let (left-win (nth (inc index) topics))
                        (incf index)
                        (setf win left-win)))
                ((and (click-key? key)
                      (> clicks 1))
                    (goto-new-topic (.scrape-lisp-word-at win x y)))
                ))
        (finally
            (terminal:close-all-windows))))

(defun command-popup (win-orig prompt)
    (let w (.width win-orig))
    (let h (.height win-orig))
    (using (win (window:open :top (- h 2) :height 2))
        (.write win (string:repeat "-" w))
        ;(.draw-line $stdscr 0 0 w 0)
        (.set-cursor-pos win 0 1)
        (setf (.bold win) true)
        (.write win prompt)
        (setf (.bold win) false)
        (let str (.read win :max-chars 20))
        (return str)))

#endif

#if console-mode

(defun help (&optional (topic 'help))
    (print (get-documentation topic))
    (void))

#endif

(setf $help-hook help)

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
                  (get-runtime-documentation target)))
    (transform-page text))

(defun get-documentation-from-file (topic)
    (let name (transform-topic-to-path-name topic))
    (let files (list (string name ".txt") (string name ".md")))
    (let path (find-one-of-source-files files $help-path))
    (if path
        (file:read-all-text path)))

(defun margin (&optional (depth 2))
    (string:repeat "    " depth))

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

#if graphical-mode

(defun transform-line (text)
    (let bold-pattern1 #/\*\*(\S*?)\*\*/)
    (let bold-pattern2 #/__(\S*?)__/)
    (let italic-pattern1 #/\*(\S*?)\*/)
    (let italic-pattern2 #/_(\S*?)_/)
    (let code-pattern #/`(\S*?)`/)
    (do
        text
        (.regex-replace ~ bold-pattern1 @"<b>\1</b>")
        (.regex-replace ~ bold-pattern2 @"<b>\1</b>")
        (.regex-replace ~ italic-pattern1 @"<i>\1</i>")
        (.regex-replace ~ italic-pattern2 @"<i>\1</i>")
        (.regex-replace ~ code-pattern @"<u><i>\1</i></u>")
        ))

(defun bold (text)
    (string "<b>" text "</b>"))


(defun transform-page (text)
    (let lines (string:split text "\n"))
    (let result
        (loop
            (initially
                (var inside-code false))
            (for line :in lines)
            (cond
                ((.starts-with line "~~~")
                    (setf inside-code (not inside-code))
                    (collect (if inside-code "<italic><delete-lf>" "</italic><delete-lf>")))
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

#endif

#if console-mode

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

(defun bold (text)
    (string "" text ""))

(defun transform-page (text)
    (let lines (string:split text "\n"))
    (let result
        (loop
            (initially
                (var inside-code false))
            (for line :in lines)
            (cond
                ((.starts-with line "~~~")
                    (setf inside-code (not inside-code))
                    (collect (if inside-code "<delete-lf>" "<delete-lf>")))
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

#endif
