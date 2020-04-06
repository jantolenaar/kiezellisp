;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; unit-tests-core.k
;;;;

(require "unit-test")

(package "unit-test-core")

(public "test-numbers"
        "test-predicates" )

(defun test-numbers ()
    (assert 
        (not (eq 1 1))
        (eql 1 1)
        (= 1 1)
        (not (eq 1 1.0))
        (not (eql 1 1.0))
        (= 1 1.0)
        (< 1 2)
        (<= 1 2)
        (> 2 1)
        (>= 2 1)
        (= (compare 1 2) -1)
        (= (compare 1 1) 0)
        (= (compare 2 1) 1)
        (= (inc 1) 2)
        (= (dec 2) 1)
        (= (* 3 4) 12)
        (= (/ 12 3) 4)
        (= (% 12 5) 2)
        (= 1 (* 3 1/3))

    ))        

(defun test-predicates ()
    (assert
        (number? 1)
        (not (number? "aaa"))
        (integer? 1)
        (not (integer? 1/3))
        (ratio? 1/3)
        (rational? 1/3)
        (not (ratio? 1))
        (rational? 1)
        (not (ratio? (* 3 1/3)))
        (rational? (* 3 1/3))
        (integer? (* 3 1/3))
        (complex? #c(1 1))
        (not (complex? 1))
        (complex? (* #c(0 1) #c(0 1)))
        

    ))

