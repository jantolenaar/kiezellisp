;;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; language forms (except loop and setf)
;;;;

(defpackage "lisp"
    (:export
        "$tracing"
        "assert"
        "assert-throws-exception"
        "assert-throws-no-exception"
        "call-next-method"
        "case"
        "cond"
        "deftype"
        "defonce"
        "if-let"
        "new"
        "otherwise"
        "return-if"
        "return-or-throw"
        "trace"
        "try"
        "try-finally"
        "try-match"
        "typecase"
        "using"
        "when"
        "when-let"
        "with-input-from-string"
        "with-output-to-string"
        "macroexpand-all"
    ))

(in-package "lisp")

;;;
;;; prototype creation
;;;

(defmacro new (&rest args)
    `(prototype.new ,@args))

(defmacro deftype (name type)
    `(set-find-type ',name ,type))

;;;
;;; more persistent variables
;;;

(defmacro defonce (sym value &optional doc-string)
    `(if (defined? (quote ,sym))
          (quote ,sym)
        (def ,sym ,value ,doc-string)))

;;;
;;; conditions
;;;

(defmacro when (test &body forms)
    `(if ,test (do ,@forms)))

(defmacro return-if (test &optional val)
    `(if ,test (return ,val)))

(defmacro if-let ((sym val) then &optional else)
    `(do
        (var ,sym ,val)
        (if ,sym ,then ,else)))

(defmacro when-let ((sym val) &body forms)
    `(do
        (var ,sym ,val)
        (when ,sym ,@forms)))

(defun cond-compiler (forms)
    (when forms
        (var form (first forms))
        (if (eql (first form) true)
              `(do ,@(rest form))
            (if (rest form)
                  `(if ,(first form) (do ,@(rest form)) ,(recur (rest forms)))
                (do
                    (var temp (gentemp))
                    `(if-let (,temp ,(first form)) ,temp ,(recur (rest forms))))))))

(defmacro cond (&body forms)
     (cond-compiler forms))

(defun case-keylist-helper (sym keys)
    (cons 'or (as-list (map (lambda (key) `(= ,sym ',key)) keys))))

(defun case-key-helper (sym keys)
    (if (list? keys)
          (case-keylist-helper sym keys)
        (if (or (= keys 'true) (= keys 'otherwise))
            `true
            `(= ,sym ',keys))))

(defun case-clause-helper (sym clause)
    (cons (case-key-helper sym (first clause)) (rest clause)))

(defmacro case (keyform &rest clauses)
    (var sym (gentemp))
    `(do
        (var ,sym ,keyform)
        (cond ,@(map (partially-apply case-clause-helper sym) clauses))))

(defun typecase-helper (sym type)
    (if (or (= type 'true) (= type 'otherwise))
        `true
        `(type? ,sym ',type)))

(defun typecase-clause-helper (sym clause)
    (cons (typecase-helper sym (first clause)) (rest clause)))

(defmacro typecase (keyform &rest clauses)
    (var sym (gentemp))
    `(do
        (var ,sym ,keyform)
        (cond ,@(map (partially-apply typecase-clause-helper sym) clauses))))


;;;
;;; trace functions
;;;

(def $tracing false
    "Enables/disables tracing by the `trace` macro.")

(defmacro trace (expr)
    (when $debug-mode
        (var result (gentemp))
        `(when $tracing
            (var ,result ,expr)
            (write-line (string
                            ,(write-to-string expr :escape true)
                            " => "
                            (write-to-string ,result :escape true)) :stream $stdlog))))

;;;
;;; tail recursion
;;;

(defmacro tailcall (proc &rest args)
    `(return (system.create-tailcall ,proc ,@args)))


;;;
;;; generic methods
;;;

(defmacro call-next-method ()
    `(system.call-next-method __lambdas__ __args__))

;;;
;;; exception handling
;;;

(defun return-or-throw (try-and-catch-result)
    (if (exception? try-and-catch-result)
          (throw try-and-catch-result)
        try-and-catch-result))

(defmacro try (&body forms)
    (var result (gentemp))
    `(do
        (var ,result (try-and-catch ,@forms))
        (if (not (exception? ,result))
            ,result)))

(defmacro using ((variable resource) &body forms)
    `(do
        (var ,variable ,resource)
        (try-finally
            (do ,@forms)
            (system.dispose ,variable))))

;;;
;;; assert
;;;

(defun do-assert (expr)
    `(when (not ,expr)
        (throw (assert-failed-exception.new
                    ,(string.format "Failed condition: {0} " expr)))))

(defmacro assert (&rest exprs)
    (when $debug-mode
        `(do ,@(map do-assert exprs))))

(defmacro assert-throws-exception (expr)
    (when $debug-mode
        (var result (gentemp))
        `(do
            (var ,result (try-and-catch ,expr))
            (when (not (exception? ,result))
                (throw (assert-failed-exception.new
                            ,(string.format "Failed throws-exception: {0} " expr)))))))


(defmacro assert-throws-no-exception (expr)
    (when $debug-mode
        (var result (gentemp))
        `(do
            (var ,result (try-and-catch ,expr))
            (when (exception? ,result)
                (throw (assert-failed-exception.new
                            ,(string.format "Failed throws-no-exception: {0} " expr)))))))


;;;
;;; string input/output
;;;

(defmacro with-input-from-string ((variable text) &body forms)
    `(using (,variable (make-reader ,text))
        ,@forms))

(defmacro with-output-to-string ((variable) &body forms)
    `(using (,variable (string-writer.new))
        ,@forms
        (.to-string ,variable)))

