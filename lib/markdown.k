;;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; markdown.k
;;;;
;;;;
;;;; External projects:
;;;;
;;;;     git clone https://github.com/toptensoftware/markdowndeep

(defpackage "markdown"
    (:export "convert-to-html"))

(in-package "markdown")

(reference "MarkdownDeep.dll")

(import "MarkdownDeep.Markdown" :package-name "markdown-deep")

(defun convert-to-html (str)
    (var md (markdown-deep.new))
    (setf (.extra-mode md) true)
    (.transform md str))




