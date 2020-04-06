;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; pandoc.k
;;;;
;;;;
;;;; External projects:
;;;;
;;;;     installation of pandoc (www.pandoc.org)

(package "pandoc")

(public "convert-to-html")

(require "shell")

(defun convert-to-html (input)
    (let options (string:split "- --output=- --from=commonmark --to=html"))
    (shell::exec "pandoc" :stdin input :return-stdout true :args options))



