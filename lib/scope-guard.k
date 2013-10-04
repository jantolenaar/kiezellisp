;;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; scope-guard
;;;;
;;;; Based on the scope feature of the D programming language.
;;;;

;;; example
;;;
;;; (load "scope-guard")
;;; (use-package "scope-guard")
;;;
;;; (with-scope
;;;      (var x 1)
;;;      (scope-exit-pass (print-line "pass"))
;;;      (scope-exit (print-line "exit"))
;;;      (scope-exit-fail (print-line "fail"))
;;;      'y)
;;;

(defpackage "scope-guard"
    (:export
        "with-scope"
        "scope-exit-pass"
        "scope-exit-fail"
        "scope-exit"
    ))

(in-package "scope-guard")

(defmacro with-scope (&body forms)
    (var $scope-list (gentemp "scope-list"))
    (var scope-result (gentemp "scope-result"))
    `(do
        (var ,$scope-list null)
        (var ,scope-result (try-and-catch (do ,@(force (map translate-scope-form forms)))))
        (run-exit-scopes ,$scope-list ,scope-result)
        (return-or-throw ,scope-result)))

(defun translate-scope-form (form)
    (or (maybe-translate-scope-form form)
        form))

(defun maybe-translate-scope-form (form)
    (if (list? form)
        (case (first form)
            (scope-exit-pass `(setf ,$scope-list (cons ,(make-exit-pass-trigger (rest form)) ,$scope-list)))
            (scope-exit-fail `(setf ,$scope-list (cons ,(make-exit-fail-trigger (rest form)) ,$scope-list)))
            (scope-exit      `(setf ,$scope-list (cons ,(make-exit-trigger      (rest form)) ,$scope-list))))))

(defun make-exit-fail-trigger (forms)
    (var failing (gentemp "failing"))
    `(lambda (,failing)
        (when ,failing
            ,@forms)))

(defun make-exit-pass-trigger (forms)
    (var failing (gentemp "failing"))
    `(lambda (,failing)
        (when (not ,failing)
            ,@forms)))

(defun make-exit-trigger (forms)
    (var failing (gentemp "failing"))
    `(lambda (,failing)
        ,@forms))

(defun run-exit-scopes (scope-list result)
    (var failing (exception? result))
    (loop
        (for func in scope-list)
        (funcall func failing)))


 
