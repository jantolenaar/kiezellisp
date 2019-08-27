;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; asciidoc.k
;;;;

(require "shell")

(defpackage "asciidoc"
    (:export "convert-to-html"))

(in-package "asciidoc")

(defun convert-to-html (str &key print-output)
    (let html (shell:exec-with-io-redirect str "asciidoc" "-o" (or print-output "-") "-s" "-"))
    (if print-output
        (print html)
        html))

         

