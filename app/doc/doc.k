;;;; Copyright (C) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; doc
;;;;

(defpackage "lisp"
    (:export "doc" "$doc-root"))

(in-package "lisp")

(require "make-html")

(def $doc-root "https://jantolenaar.github.io/kiezellisp/help/")

(defun syntax (topic)
    (chain (~)
        (get-description topic)
        (.function-syntax ~)
        (map write-to-string ~)
        (each print-line ~)))

(defun trim-decorations (text)
    (let pattern #/Syntax(.*)Description(.*)/s)
    (let matches (.regex-match text pattern))
    (let syntax (second matches))
    (let description (third matches))
    (cond 
        ((and syntax description)
            (string (.trim syntax) "\n\n" (.trim description)))
        (syntax
            (.trim syntax))
        (description
            (.trim description))
        (true
            text)))

(defun doc (topic)
    (let text
        (chain (~)
            (make-html:transform-topic-to-path-name topic)
            (string $doc-root ~ ".html")
            (list "w3m" "-dump" ~)
            (apply shell:exec-with-io-redirect null ~)
            (trim-decorations ~)))
    (if text
        (print text)
        (syntax topic)))
