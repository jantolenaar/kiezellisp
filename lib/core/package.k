;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; package.k
;;;;

(in-package "lisp")

(export-symbol "defpackage")

(defmacro defpackage (name &rest options)
    `(defpackage-imp ,name ',options))

(defun get-option (keyword options)
    (do
        options
        (filter (λx (= (car x) keyword)) ~)
        (map rest ~)))

(defun defpackage-imp (name options)
    (let $package (make-package name))
    (each (λx (set-doc $package (apply string x)))
          (get-option :documentation options))
    (use-package "lisp")
    (each use-package (flatten (get-option :use options)))
    (each import-symbol (flatten (get-option :import options)))
    (each shadow-symbol (flatten (get-option :shadow options)))
    (each export-symbol (flatten (get-option :export options)))
    $package)


