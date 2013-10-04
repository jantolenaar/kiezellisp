
(require :unit-test)

(defpackage "unit-test-kiezellisp"
    (:export
        "data-types"
        "operators"
        "predicates"
        "lists"
        "vectors"
        "sequences"
        "prototypes"
    ))

(in-package "unit-test-kiezellisp")

(defun data-types ()
    (assert
        (integer? 1)
        (not (integer? 12.3))
        (string? "aap")
        (not (string? 123))
        (list? '(1 2 3))
        (not (list? 123))
        (vector? [1 2 3])
        (not (vector? '(a b c)))
        (complex? #c(3 3))
        (not (complex? 1))
        (type? 123 'int)
        (type? 1.0 'decimal)
        (type? '(a b) 'cons)
        (type? '(a b) 'list)
        (type? null 'list)
        (type? [1 2 3] 'vector)
        (type? {:a 1} 'prototype)
        (type? 23:59 'time-span)
    ))

(defun operators ()
    (assert
        (= 3 3)
        (/= 2 3)
        (< 2 3)
        (<= 2 2)
        (> 3 2)
        (>= 2 2)
        (= (compare 1 1) 0)
        (< (compare -1 1) 0)
        (> (compare 1 -1) 0)
        (not (eq 3 3))
        (eql 3 3)
        (equal 3 3)
    ))

(defun predicates ()
""
    )

(defun lists ()
""
    )

(defun vectors ()
    ""
    )

(defun sequences ()
""
    )

(defun prototypes ()
""

)


