;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

(defpackage :constructor-factory
    (:export
        "make-constructors"
        "make-constructor"))

(in-package :constructor-factory)

(defun make-constructors (tags)
    (each make-constructor tags))

(defun make-constructor (arg)
    (let name (if (prototype? arg) (.name arg) arg))
    (let alias (if (prototype? arg) (.alias arg) arg))
    (let package (get-package name))
    (let new-method (make-symbol "new" package))
    (let macro-name (.to-upper (string alias)))
    (let macro-sym (make-symbol macro-name))
    (eval `(defmacro ,macro-sym (&rest args)
            (make-obj ',new-method args)))
    (export-symbol macro-name))

(defun make-obj (type args)
    (let obj (gentemp "obj"))
    `(do
        (let ,obj (,type))
        ,@(init-obj-properties obj args)
        ,obj))

(defun init-obj-properties (obj args)
    (loop
        (for head :on-list args :by cddr)
        (let name (first head))
        (let value (second head))
        (collect (init-obj-property obj name value))))

(defun init-obj-property (obj name value)
    (let match (.regex-match (symbol-name name) @"^(.*)-add$"))
    (cond
        (match
            `(.add ((. ,(second match)) ,obj) ,value))
        (true
            `(set-attr ,obj ,name ,value))))


