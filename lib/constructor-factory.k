;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

(defpackage :constructor-factory
    (:export
        "make-constructors"
        "make-constructor"))

(in-package :constructor-factory)

(defun make-constructors (tags)
    (each make-constructor tags))

(defun make-constructor (arg)
    (let package-name (if (prototype? arg) (.name arg) arg))
    (let alias (if (prototype? arg) (.alias arg) arg))
    (let new-method (make-symbol package-name ":new"))
    (let macro-name (.to-upper (string alias)))
    (let macro-sym (make-symbol macro-name))
    (eval `(defmacro ,macro-sym (&rest args)
            (make-obj ',new-method args)))
    (export-symbol macro-name))

(defun make-obj (type args)
    (let obj (gentemp "obj"))
    (with-multiple-let (ctor-args props) (split-with (complement keyword?) args)
	    `(do
	        (let ,obj (,type ,@ctor-args))
	        ,@(init-obj-properties obj props)
	        ,obj)))

(defun init-obj-properties (obj args)
    (loop
        (for head :on-list args :by cddr)
        (let name (first head))
        (let value (second head))
        (collecting (init-obj-property obj name value))))

#if gtk

(def verbs (as-list (map symbol-name '(
    add append pack-start pack-end
    ))))

#elif winforms

(def verbs (as-list (map symbol-name '(
    add 
    ))))

#endif

(defun init-obj-property (obj kwname value)
    (let name (symbol-name kwname))
    (let found (index-of name verbs))
    (lazy match (.regex-match name @"^(.*)-add$"))
    (cond
        (found
            `((. ,name) ,obj ,value))
        (match
            `(.add ((. ,(second match)) ,obj) ,value))
        (true
            `(set-attr ,obj ,name ,value))))


