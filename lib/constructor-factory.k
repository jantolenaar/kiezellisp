;;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

(defpackage :constructor-factory
    (:export
        "make-constructors"
        "make-constructor"))

(in-package :constructor-factory)

(defun make-constructors (tags)
    (each make-constructor tags))

(defun make-constructor (arg)
    (var name (if (prototype? arg) (.name arg) arg))
    (var alias (if (prototype? arg) (.alias arg) arg))
    (var package (get-package name))
    (var new-method (make-symbol "new" package))
    (var macro-name (.to-upper (string alias)))
    (var macro-sym (make-symbol macro-name))
    (eval `(defmacro ,macro-sym (&rest args)
            (make-obj ',new-method args)))
    (export-symbol macro-name))

(defun make-obj (type args)
    (var obj (gentemp "obj"))
    `(do
        (var ,obj (,type))
        ,@(init-obj-properties obj args)
        ,obj))

(defun init-obj-properties (obj args)
    (loop
        (for head :on-list args :by cddr)
        (var name (first head))
        (var value (second head))
        (collect (init-obj-property obj name value))))

(defun init-obj-property (obj name value)
    (var match (.regex-match (symbol-name name) @"^(.*)-add$"))
    (cond
        (match
            `(.add ((. ,(second match)) ,obj) ,value))
        (true
            `(set-attr ,obj ,name ,value))))


