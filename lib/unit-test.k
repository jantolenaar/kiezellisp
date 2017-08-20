;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; unit-test.k
;;;;

(defpackage "unit-test"
    (:export
        "run-all"
        "run-function"
        "run-package"
    ))

(in-package "unit-test")

(defun run-function (name)
    (let result (try-and-catch (funcall name)))
    (if (exception? result)
            (do
                (write "FAIL" 
                    :escape false 
                    :color 'white 
                    :background-color 'dark-red)
                (print-line " " name)
                (print-line (unwind-to-assert-failed-exception result)))
        (do
            (write "OK  " 
                :escape false 
                :color 'dark-green)
            (print-line " " name))))

(defun unwind-to-assert-failed-exception (ex)
    (cond
        ((type? ex 'assert-failed-exception)
            (.message ex))
        ((.inner-exception ex)
            (unwind-to-assert-failed-exception (.inner-exception ex)))
        (true
            (.message ex))))


(defun test-function? (x)
    (and
        (defined? x)
        (function? (symbol-value x))))

(defun test-package? (x)
    (.starts-with x "unit-test-"))

(defun run-package (name)
	(chain (~)
		(list-exported-symbols name)
		(filter test-function? ~)
		(each run-function ~)))

(defun run-all ()
	(chain (~)
		(list-all-packages)
		(filter test-package? ~)
		(each run-package ~)))
		
