;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.
;;;;
;;;; macros2.k
;;;;

(defpackage "lisp"
    (:export
        "psetq"
        "psetf"
        "lambda*"
        "defun*"
        "tail-recursion-loop"
        ))

(in-package "lisp")

;;;
;;; parallel assignment
;;;

(defmacro psetq (&rest forms)
    (parallel-assign 'setq forms))

(defmacro psetf (&rest forms)
    (parallel-assign 'setf forms))

(defun parallel-assign (setter forms)
    (letfun prepare-forms (forms)
        (do
            (partition 2 forms)
            (map #(cons (gentemp) %1) ~)))
    (letfun get-let-form (form)
        (list 'let (first form) (third form)))
    (letfun get-set-form (form)
        (list setter (second form) (first form)))
    (let genforms (prepare-forms forms))
    `(merging-do
        ,@(map get-let-form genforms)
        ,@(map get-set-form genforms)))

;;;
;;; multi-arity functions
;;;

(defun argument-case (sym cases)
    `(ecase (length ,sym)
        ,@(map #(argument-case-branch sym %1) cases)))

(defun argument-case-branch (sym branch)    
    (let names (first branch))
    (let body (rest branch))
    `(,(length names)
        ,@(map-indexed #(list 'var %2 (list 'elt sym %1)) names)
        ,@body))

(defmacro lambda* (&rest branches)
    `(lambda (&rawparams args)
        ,(argument-case 'args branches)))

(defmacro defun* (name &rest branches)
    `(defun ,name (&rawparams args)
        ,(argument-case 'args branches)))

;;;
;;; tail-recursion-loop
;;;

(defmacro tail-recursion-loop ((&rest bindings) &body forms)
    (let temp (gentemp))
    (let names (map first bindings))
    (let values (map second bindings))
    `(do
        (array ,@values)
        (label %recursion-label)
        (let ,temp ~)
        (do
            ,@(map-indexed #(list 'var %2 (list 'elt temp %1)) names)
            (do ,@forms))))

