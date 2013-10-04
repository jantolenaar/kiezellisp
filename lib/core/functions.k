;;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; functions.k
;;;;

(defpackage "lisp"
    (:export
        "compose"
        "partially-apply"
    ))

(in-package "lisp")

(defun partially-apply (func &rest head-args)
    "Creates a new function by partially applying arguments to the given function."
    (lambda (&rest tail-args)
        (apply func (append head-args tail-args))))

(defmacro compose (func1 &rest funcs)
    "Creates a new function that applies the given functions from the right to the left."
    (if funcs
          `(lambda (&rest args) ,(compose-functions func1 funcs))
        func1))

(defun compose-functions (func1 funcs)
    (if funcs
            (list func1 (compose-functions (first funcs) (rest funcs)))
        (list 'apply func1 'args)))

