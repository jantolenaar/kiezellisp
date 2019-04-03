;;;; Copyright (C) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; make-html
;;;;

;;;;
;;;; Rewrites .mdk and .md files to html.
;;;;
;;;; The .mdk input files contain directives:
;;;; 	.topic .index .syntax .terms .examples .line
;;;; The topic and index directives are used to build the index file.
;;;; The syntax, terms and examples directives are converted to Kiezellisp code
;;;; which is executed to produce markdown text.
;;;;
;;;; Uses linux `par` utility to reformat paragraphs.

(require "common-mark")
(use-package-alias "common-mark" "md")

(import "System.Security.Cryptography.MD5")

(require "xml")
(require "html")
(use-package "html-elements")

(def home shell:$home)
(def projects "${home}/projects")

(def mathjax-url-2 "https://cdnjs.cloudflare.com/ajax/libs/mathjax/2.7.5/latest.js??config=TeX-AMS-MML_HTMLorMML")
(def mathjax-url-3 "/mathjax/MathJax.js?config=TeX-AMS-MML_HTMLorMML")
(def mathjax-config @"MathJax.Hub.Config({tex2jax: {inlineMath: [['$','$'], ['\\(','\\)']]}});")

; needed by examples
(import "System.Collections.IList")
(import "System.IO.Path")
(import "System.IO.File")

(defun time-stamp (now)
    (let span (.subtract now (date-time:new 2000 1 1)))
    (as-long (.total-seconds span)))

(def $input-dir null)
(def $output-dir null)
(def $master-index null)
;(def $style-sheet "styles.css?v=<%=(time-stamp)%>")
(def $style-sheet "styles.css")
(def $style-sheet-version null)
(def $site-name null)

(defun make-kiezellisp-html()
    (let $site-name "jan&nbsp;tolenaar&nbsp;/&nbsp;<span style='color:yellow'>kiezel</span>lisp")
    (let $input-dir "${projects}/kiezellisp/public/src/doc")
    (let $output-dir "${home}/www/local/jantolenaar/kiezellisp")
    ;; Mdk files containing an index directive are added to the 
    ;; master index of the public api. If $master-index is null no 
    ;; master index is created.
    (let $master-index (vector))
    ;; this code is duplicated in make-html
    ;; styles.css must be present in $input-dir
    (let info (file-info:new (path:combine $input-dir $style-sheet)))
    (let $style-sheet-version (string (time-stamp (.last-write-time info))))
    (rewrite-md-change-log)
    (make-html))

(defun make-html ()
    ;; used when wrapping api links and pretty printing examples
    ;; width of <pre> in html
    (let $right 66)
    ;; styles.css must be present in $input-dir
    (let info (file-info:new (path:combine $input-dir $style-sheet)))
    (let $style-sheet-version (string (time-stamp (.last-write-time info))))
    (rewrite-files)
    (if (vector? $master-index)
        (write-master-index)))

(defun copy-change-log ()
    (file:copy "${projects}/kiezellisp/public/change-log.md"
               "${$input-dir}/change-log.mdk"
               true))

(defun equal-files? (path1 path2)
    (let info1 (file-info:new path1))
    (let info2 (file-info:new path2))
    (and (file:exists path1)
         (file:exists path2)
         (= (.length info1) (.length info2))
         (= (.last-write-time info1) (.last-write-time info2))))

(defun unchanged-file? (new-content old-path)
    (when (file:exists old-path)
        (let old-html-text (file:read-all-text old-path))
        (string:equals old-html-text new-content)))

(defun get-files ()
    (let prefix-length (inc (.length $input-dir)))
    (letfun get-base-name (path)
        (path:get-unix-name (.substring path prefix-length)))
    (map get-base-name (directory:get-files $input-dir "*" search-option:ALL-DIRECTORIES)))

(defun rewrite-files ()
    (each rewrite-file (get-files)))

(defun rewrite-file (name)
    ;(say name)
    (cond
        ((.ends-with name ".mdk")
            (rewrite-mdk-file name))
        ((.ends-with name ".md")
            (rewrite-md-file name))
        (true (copy-non-mdk-file name))))

(defun copy-non-mdk-file (name)
    (let input-file (path:combine $input-dir name))
    (let output-file (path:combine $output-dir name))
    (directory:create-directory (path:get-directory-name output-file))
    (when (not (equal-files? input-file output-file))
        (say output-file)
        (file:copy input-file output-file true)))

(defun rewrite-mdk-file (name)
    (let item (get-rewrite-data name))
    (if (and 
            (.anchor item)
            (vector? $master-index))
        (.add $master-index item))
    (let md-text (.data item))
    (let anchor (.anchor item))
    (let html-text (render-html name (md:convert-to-html md-text) :anchor anchor))
    (save-html-file name html-text))

(defun rewrite-md-file (name)
    (let input-file (path:combine $input-dir name))
    (let md-text (file:read-all-text input-file))
    (let html-text (render-html name (md:convert-to-html md-text)))
    (save-html-file name html-text))

(defun rewrite-md-change-log ()
    (let input-file (path:combine $input-dir "../../change-log.md"))
    (let md-text (file:read-all-text input-file))
    (let html-text (render-html "change-log" (md:convert-to-html md-text)))
    (save-html-file "change-log" html-text))

(defun render-header (name &key etag math)
    (let depth (count #(= %1 #\/) name))
    (let root (.repeat "../" depth))
    (let head
        (<head>
            (<meta> :name "copyright"
                    :content "Jan Tolenaar (c) 2009-2019")
            (<meta> :http-equiv "Content-Type"
                    :content "text/html; charset=utf-8")
            (<meta> :http-equiv "CACHE-CONTROL"
                    :content "NO-CACHE")
            (<title> name)
            (<link> :rel "StyleSheet"
                    :type "text/css"
                    :href (string root $style-sheet "?v=" $style-sheet-version))
            (if math
                (<script> :type "text/x-mathjax-config" mathjax-config))
            (if math
                (<script> :type "text/javascript" :src mathjax-url-2))
            ))
    (when etag
        (xml:add head :etag etag))
    head)

(defun render-body (name content &key anchor wide)
    (let depth (count #(= %1 #\/) name))
    (let root (.repeat "../" depth))
    (let content-id (if wide "wide-content" "content"))
    (<body>        
        (<a> :class "banner" :href "${root}index.html" $site-name)
        "&nbsp;"
        "&nbsp;"
        "&nbsp;"
        "&nbsp;"
        "&nbsp;"
        "&nbsp;"
        (if (and anchor (vector? $master-index))
            (<a> :class "banner" :href "${root}help/index.html" "index of everything"))
        (<div> :id content-id content)))

(defun make-md5sum (content)
    (using (md5 (md5:create))
        (let hash (.compute-hash md5 (.get-bytes (encoding:utf8) content)))
        (with-output-to-string ($stdout)
            (foreach (byte hash)
                (print (.to-string byte "x2"))
        ))))

(defun get-etag (content)
    (make-md5sum (string content "-" $style-sheet-version)))

(defun get-math-flag (content)
    (and false (string? content)
         (or (/= (.index-of content "$") -1)
             (/= (.index-of content @"\(") -1))))

(defun render-html(name body &key anchor wide)
    (let etag (get-etag body))
    (let math (get-math-flag body))
    (html:to-string
        (<html>
            :lang "en"
            (render-header name :etag etag
                                :math math)
            (render-body name body :anchor anchor :wide wide))))

(defun save-html-file (name html-text)
    (let path (path:combine $output-dir name))
    (let output-file (path:change-extension path ".html"))
    (let folder (path:get-directory-name output-file))
    (directory:create-directory folder)
    (when (not (unchanged-file? html-text output-file))
        (say output-file) 
        (file:write-all-text output-file html-text)))

(defun write-master-index ()
    (let lisp-package (find-package "lisp"))
    (letfun is-not-alnum (item)
        (let letter (elt (symbol-name (.anchor item)) 0))
        (not (char:is-letter-or-digit letter)))
    (letfun is-lisp-alnum (item)
        (let letter (elt (symbol-name (.anchor item)) 0))
        (let package (symbol-package (.anchor item)))
        (and (char:is-letter-or-digit letter)
        	 (= package lisp-package)))
    (letfun is-other (item)
        (not (or (is-not-alnum item)
                 (is-lisp-alnum item))))

    (let not-alnum-items (sort-by .anchor (filter is-not-alnum $master-index)))
    (let lisp-alnum-items (sort-by .anchor (filter is-lisp-alnum $master-index)))
    (let other-items (sort-by .anchor (filter is-other $master-index)))
    (let set1 (make-links (append not-alnum-items other-items)
                            #(symbol-package (.anchor %1))))
    (let set2 (make-links lisp-alnum-items
                            #(elt (symbol-name (.anchor %1)) 0)))

    (let md-text (with-output-to-string ($stdout)
    	(print-line "<br/><div class=indexofeverything>")
        (print-line set1 "<br/><br/>" set2)
    	(print-line "</div>")))

    (let name "help/index.html")
    (let html-text (render-html name md-text :anchor "index" :wide true))
    (save-html-file name html-text))

(defun* make-links
    ((items partitioner)
        (let partitions (partition-by partitioner items))
        (string:join "<br/>\n<br/>\n" (map make-links partitions)))
    ((items)
        (letfun make-link (item)
            (let anchor (.anchor item))
            (let file1 (path:change-extension (.file item) ".html"))
            (let file2 (.substring file1 (length "help/")))
            ;;; code is not safe for all anchors
            (string "<a href='" file2 "'>" anchor "</a>"))
        (string:join "<br/>\n" (map make-link items))))

(defun get-rewrite-data (name)
    (let input-file (path:combine $input-dir name))
    (let lines (file:read-all-lines input-file))
    (var anchor null)
    (var index null)
    (var usage null)
    (var result
        (loop
            (initially
                (var terminator-line null))
            (for line :in lines)
            (cond
                ((string? terminator-line)
                    (cond
                        ((= (.trim line) "")
                            (collecting terminator-line)
                            ;(collecting "")
                            (setf terminator-line null))
                        (true
                            (collecting line))))
                ((.starts-with line ".index")
                    (let text (.trim (.substring line (.length ".index"))))
                    (setf anchor (make-symbol text))
                    (setf index text)
                    (collecting "## Name")
                    ;;;; surround with <p></p> to prevent mark down interpretation of some special characters
                    (collecting "<p>${(.html-encode text)}</p>"))
                ((.starts-with line ".usage")
                    (let text (.trim (.substring line (.length ".usage"))))
                    (setf usage text)
                    (collecting "\n## Usage")
                    (collecting text))
                ((.starts-with line ".syntax*")
                    (collecting "<% embedded-syntax")
                    (setf terminator-line "%>"))
                ((.starts-with line ".syntax")
                    (collecting "\n## Syntax")
                    (collecting "<% embedded-syntax")
                    (setf terminator-line "%>"))
                ((.starts-with line ".api")
                    (collecting "<% embedded-api-links")
                    (setf terminator-line "%>"))
                ((.starts-with line ".examples")
                    (collecting "<! embedded-examples #q qwerty")
                    (setf terminator-line "qwerty !>"))
                ((.starts-with line ".code")
                    (collecting "<! embedded-code #q qwerty")
                    (setf terminator-line "qwerty !>"))
                ((.starts-with line ".see-also")
                    (collecting "\n## See also"))
                ((.starts-with line ".description")
                    (collecting "\n## Description\n")
                    (setf terminator-line null))
               (true
                    (collecting line)))
            (finally
                (when (string? terminator-line)
                    (collecting terminator-line)))))
    (let data (evaluate result))
    (new :anchor anchor :file name :data data))

(defun evaluate (lines)
	(chain (~)
	    (string:join "\n" lines)
	    (interpolate-string ~)
	    (remove-duplicate-blank-lines ~)))

(defun remove-duplicate-blank-lines (text)
    (let lines (map .trim-end (.split text "\n")))
    (let lines2 (loop
        (for p on-list lines by cdr)
        (when (or (/= (first p) "")
                  (/= (second p) ""))
            (collecting (first p)))))
    (string:join "\n" lines2))


;;;;
;;;; embedded code
;;;;

(defmacro embedded-api-links (&rest args)
    `(do-api-links ',args))

(defun do-api-links (args)
	(chain (~)
		(filter boolean args)
		(sort ~)
		(map make-api-link ~)
		(string:join " " ~)
		(reformat-line ~ 100)
		(print-line ~)))

(defun transform-topic-to-path-name (topic)
    (chain (~)
        (.long-name topic)
        (.replace ~ "%" "(perc)")
        (.replace ~ "$" "(dollar)")
        (.replace ~ "?" "(qm)")
        (.replace ~ "." "(dot)")
        (.replace ~ ">=" "(ge)")
        (.replace ~ "<=" "(le)")
        (.replace ~ ">" "(gt)")
        (.replace ~ "<" "(lt)")
        (.replace ~ "/" "(slash)")
        (.replace ~ "*" "(star)")
        (.replace ~ ":" "/")
        (.replace ~ "=" "(eq)")
        (.replace ~ "+" "(plus)")))

(defun make-api-link (name)
    (let ref (string "../" (transform-topic-to-path-name name) ".html"))
    (string "[" name "](" ref ")"))

(defmacro embedded-syntax (&rest args)
    `(do-prototypes ',args))

(defmacro embedded-code (code-text)
    `(do-code ,code-text))

(defmacro embedded-examples (example-text)
    `(do-examples ,example-text))

(defun do-prototypes (prototypes)
    (each do-prototype prototypes))

(defun do-prototype (prototype)
    (print-line "<code>")
    (cond
        ((list? prototype)
            (cond
                ((and (= (first prototype) 'setf)
                      (list? (second prototype)))
                    (print "(setf ")
                    (do-prototype-list (second prototype))
                    (do-prototype-arg (third prototype))
                    (print ")"))
                (true
                    (do-prototype-list prototype))))
        (true
            (do-variable prototype)))
     (print-line "</code><br/>"))

(defun do-prototype-list (seq)
    (print "(")
    (print (bold (first seq)))
    (foreach (arg (rest seq))
        (do-prototype-arg arg))
    (print ")"))

(defun do-prototype-arg (arg)
    (cond
        ((cons? arg)
            (print " (")
            (print (italic (first arg)))
            (each (lambda (x) (print " ") (write x :escape true)) (rest arg))
            (print ")"))
        ((and (symbol? arg)
              (= (elt (symbol-name arg) 0) #\&))
            (print " " (small arg)))
        ((keyword? arg)
            (print " " (bold (symbol-name arg))))
        (true
            (print " " (italic arg)))))

(defun do-variable (arg)
    (print (bold arg)))

(defun bold (&rest args)
    (string "__" (string:join " " args) "__"))

(defun italic (&rest args)
    (string "_" (string:join " " args) "_"))

(defun small (&rest args)
    (string:join " " args))

(defun do-indent (text)
    (print-line "~~~")
    (if (.ends-with text "\n")
        (print text)
        (print-line text))
    (print-line "~~~"))

(defun do-examples (example-text)
    (let examples (split-examples example-text))
    (let content (print-examples-to-string examples true))
    (do-indent content))

(defun open-parens? (line)
    (> (count #(= %1 #\left-par) line) (count #(= %1 #\right-par) line)))

(defun split-examples (example-text)
    (loop
        (initially
            (let lines (.split (.trim-end example-text) #\newline))
            (var buffer null))
        (for line in lines)
        (let line2 (.trim-end line))
        (let override? (.ends-with line2 "\\"))
        (let line3 (.trim-end line2 "\\"))
        (setf buffer (string buffer (if buffer "\n") line3))
        (when (not (or override?
                       (open-parens? buffer)))
            (collecting buffer)
            (setf buffer null))))

(defun do-code (example-text)
    (do-indent example-text))

(defun include-code-file (path)
    (let content (file:read-all-text path))
    (do-indent content))

(defun print-examples-to-string (examples eval?)
    (var $package $package)
    (let env (make-extended-environment))
    (var dbglevel 0)
    (var catch-exceptions false)
    (gensym-reset)
    (with-output-to-string ($stdout)
        (loop
            (for example :in examples)
            (when (= example ":throws")
                (setf catch-exceptions true)
                (continue))
            (let prompt (make-prompt dbglevel))
            (let indented-example (indent-example prompt example))
            (print-line indented-example)
            (continue-if (not eval?))
            (let example-code (delay (read-from-string example)))
            (case example
                (":a"
                    (assert (< 0 dbglevel))
                    (decf dbglevel))
                (":t"
                    (assert (< 0 dbglevel))
                    (setf dbglevel 0))
                (":d"
                    (describe it))
                (true
                    (let result 
                            (do
                                (let $enable-warnings false)
                                (let $stderr $stdout)
                                (let $stdlog $stdout)
                                (if catch-exceptions
                                      (try-and-catch
	                                      (let code (visit-example (force example-code)))
                                          (force (eval code env)))
                                      (do
                                          (let code (visit-example (force example-code)))
	                                      (force (eval code env))))))
                    (cond
                        ((missing-value? result)
                            #;(print nothing))
                        ((exception? result)
                            (print (.message result))
                            (incf dbglevel))
                        (true
                            (setf it result)
                            (print "it: ")
                            (pprint result :left 4)))))
            (setf catch-exceptions false)
            (print-line))))

(defun visit-example (code)
	(if (and (cons? code)
			 (index-of (first code) '(let var)))
		(cons 'def (rest code))
		code))

(defun make-prompt (dbglevel)
    (with-output-to-string ($stdout)
        (print (.name $package))
        (if (> dbglevel 0)
            (print " " dbglevel))
        (print " > ")))

(defun indent-example (prompt example)
    (let len (length prompt))
    (let prefix (.repeat " " len))
    (let str1 (.indent example prefix))
    (let str2 (.substring str1 len))
    (string prompt str2))

(defun reformat-line (text width)
    (shell:exec text "par" "-w${width}"))

