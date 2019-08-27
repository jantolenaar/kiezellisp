;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; package.k
;;;;

(in-package "lisp")

(defmacro defpackage (name &rest options)
    `(defpackage-imp ,name ',options))

(defun get-option (keyword options)
    (map rest (filter #(= (car %1) keyword) options)))

(defun defpackage-imp (name options)
    (let $package (make-package name))
    (each #(set-doc $package (apply string %1))
          (get-option :documentation options))
    (use-package "lisp")
    (each use-package (flatten (get-option :use options)))
    (each import-symbol (flatten (get-option :import options)))
    (each shadow-symbol (flatten (get-option :shadow options)))
    (each export-symbol (flatten (get-option :export options)))
    $package)

(defun export-symbols (&rest names)
    (each export-symbol names))

(defun import-symbols (&rest names)
    (each import-symbol names))

(defun shadow-symbols (&rest names)
    (each shadow-symbol names))

(export-symbols 
    "defpackage"
    "export-symbols"
    "import-symbols"
    "shadow-symbols")

 
