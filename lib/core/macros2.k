;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.
;;;;
;;;; macros2.k
;;;;

(defpackage "lisp"
    (:export
        "psetq"
        "psetf"
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
    `(do
        ,@(map get-let-form genforms)
        ,@(map get-set-form genforms)))

