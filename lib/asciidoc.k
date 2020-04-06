;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; asciidoc.k
;;;;

(require "shell")

(package "asciidoc")

(public "convert-to-html")

(defun convert-to-html (str &key print-output)
    (let html (shell:exec-with-io-redirect str "asciidoc" "-o" (or print-output "-") "-s" "-"))
    (if print-output
        (print html)
        html))

         

