;;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

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
    `(system.create-task (λ () ,@forms)))

(defmacro generator (&body forms)
    `(system.create-generator (λ () ,@forms)))

(defmacro parallel-foreach ((v seq) &body forms)
    `(parallel-each (λ (,v) ,@forms) ,seq))

(defmacro parallel-list (&rest forms)
    `(map .result (list ,@(map (λ (expr) `(task ,expr)) forms))))

(defmacro delay (&body forms)
    `(system.create-delayed-expression (λ () ,@forms)))
