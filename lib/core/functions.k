;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; functions.k
;;;;

(defpackage "lisp"
    (:export
        "constantly"
        "filterv"
        "keep-indexed"
        "partially-apply"
        "map-indexed"
        "mapv"
        "mapcat"
    ))

(in-package "lisp")

;;;
;;; Misc functions
;;;

(defun constantly (expr)
    (lambda (&rest args)
        (declare (ignore args))
        expr))

;;;
;;; Functional functions
;;;

(defun partially-apply (func &rest head-args)
    "Creates a new function by partially applying arguments to the given function."
    (lambda (&rest tail-args)
        (apply func (append head-args tail-args))))

;;;
;;; Sequence functions
;;;

(defun map-indexed (func &rest seqs)
    (apply map func (range) seqs)) 

(defun mapv (func &rest seqs)
    (as-vector (apply map func seqs))) 

(defun mapcat (func &rest seqs)
    (apply concat (apply map func seqs)))

(defun filterv (predicate seq &rest args)
    (as-vector (apply filter predicate seq args)))


