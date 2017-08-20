;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; asciidoc.k
;;;;

(require "shell")

(defpackage "asciidoc"
    (:export "convert-to-html"))

(in-package "asciidoc")

(defun convert-to-html (str &key output)
    (let flag (not output))
    (let options (list "-o" (or output "-") "-s" "-"))
    (shell:exec "asciidoc" :stdin str :return-stdout flag :args options))
        

