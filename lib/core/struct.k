;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; struct.k
;;;;

(defpackage "lisp"
    (:export
        "defclass"
        "defstruct"))

(in-package "lisp")

(defmacro defclass (name arg1 &optional arg2)
    (let have-descr (string? arg1))
    (let descr (if have-descr arg1 ""))
    (let proto (if have-descr arg2 arg1))
    (define-class-functions name descr (eval proto)))

(defmacro defstruct (name &rest args)
    (let have-descr (string? (first args)))
    (let descr (if have-descr (first args) ""))
    (let slots (if have-descr (rest args) args))
    (let kv-list (slots-to-keyword-and-value-list slots))
    `(defclass ,name ,descr (new ,@kv-list)))

(defun slots-to-keyword-and-value-list (slots)
    (mapcat slot-to-keyword-and-value slots))

(defun slot-to-keyword-and-value (slot)
    (if (list? slot)
          (list (make-symbol ":" (symbol-name (first slot)))
              (second slot))
        (list (make-symbol ":" (symbol-name slot))
              null)))

(defun define-class-functions (name descr prototype)
    (declare (ignore descr))
    (when (not (string? descr))
        (throw-error "Expected documentation string, not: " descr))
    (let constructor-name (make-symbol "make-" name))
    (let predicate-name (make-symbol name "?"))
    (let slots (.keys prototype))
    `(do 
        (deftype ,name ,prototype)
        (defun ,predicate-name (obj) (type? obj ',name))
        (defun ,constructor-name (&rest args)
            (apply prototype:new ',name args))
        ,@(loop 
            (for slot in slots)
            (let field (make-symbol name "-" slot))
            (collect `(defun ,field (obj) ((. ,slot) obj)))
            (collect `(define-compiler-macro ,field (obj) `((. ,,slot) ,obj))))
        ',name))
