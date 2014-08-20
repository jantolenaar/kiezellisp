;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; threading.k
;;;;

(defpackage "lisp"
    (:export
        "delay"
        "generator"
        "parallel-foreach"
        "parallel-list"
        "task"
    ))

(in-package "lisp")

(defmacro task (&body forms)
    `(system:create-task (lambda null ,@forms)))

(defmacro generator (&body forms)
    `(system:create-generator (lambda () ,@forms)))

(defmacro parallel-foreach ((v seq) &body forms)
    `(parallel-each (lambda (,v) ,@forms) ,seq))

(defmacro parallel-list (&rest forms)
    (let decls (map (lambda (form) `(future ,(gentemp) ,form)) forms))
    (let vars (map cadr decls))
    `(do
        ,@decls
        (list ,@vars)))
        

(defmacro delay (&body forms)
    `(system:create-delayed-expression (lambda () ,@forms)))
