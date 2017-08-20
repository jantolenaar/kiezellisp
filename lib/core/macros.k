;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; language forms (except loop and setf)
;;;;


(defpackage "lisp"
    (:export
        "$tracing"
        "assert"
        "assert-throws-exception"
        "assert-throws-no-exception"
        "block"
        "call-next-method"
        "case"
        "chain"
        "cond"
        "defonce"
        "deftype"
        "ecase"
        "etypecase"
        "if-let"
        "ignore-errors"
        "macroexpand-all"
        "multiple-setf"
        "new"
        "otherwise"
        "return-from"
        "return-if"
        "return-or-throw"
        "trace"
        "try-and-catch"
        "typecase"
        "unless"
        "using"
        "when"
        "when-let"
        "with"
        "with-input-from-string"
        "with-multiple-let"
        "with-multiple-var"
        "with-output-to-string"
    ))

(in-package "lisp")

;;;
;;; removable do; eliminates do block when it is not necessary.
;;;


(defun declaration? (form)
    (and (cons? form)
         (index-of (first form) '(let var letfun letmacro let-symbol-macro
                                  future lazy))))

(defmacro do? (&body forms)
    (let count (length forms))
    (if (or (zero? count)
            (and (= 1 count)
                 (not (declaration? (first forms)))))
            (first forms)
        `(do ,@forms)))

;;;
;;; prototype creation
;;;

(defmacro new (&rest args)
    `(prototype:new ,@args))

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
;;; binding
;;;

(defmacro with ((sym value) &body forms)
    `(do (let (,sym ,value) ,@forms)))

(defmacro chain ((&optional (x '~)) &body forms)
	`(do (var ,x) ,@(map #(list 'setq x %1) forms) ,x))

;;;
;;; conditions
;;;

(defmacro unless (test &body forms)
    `(if ,test null (do? ,@forms)))

(defmacro when (test &body forms)
    `(if ,test (do? ,@forms)))

(defmacro return-if (test &optional val)
    `(if ,test (return ,val)))

(defmacro if-let ((sym val) then &optional else)
    `(do
        (let ,sym ,val)
        (if ,sym ,then ,else)))

(defmacro when-let ((sym val) &body forms)
    `(do
        (let ,sym ,val)
        (when ,sym ,@forms)))

(defun cond-compiler (forms)
    (when forms
        (let form (first forms))
        (if (eql (first form) true)
              `(do? ,@(rest form))
            (if (rest form)
                  `(if ,(first form) (do? ,@(rest form)) ,(self (rest forms)))
                (do
                    (let temp (gentemp))
                    `(if-let (,temp ,(first form)) ,temp ,(self (rest forms))))))))

(defmacro cond (&body forms)
     (cond-compiler forms))

(defun case-keylist-helper (sym keys)
    (cons 'or (as-list (map (lambda (key) `(= ,sym ,key)) keys))))

(defun case-key-helper (sym keys)
    (if (and (list? keys)
             (/= (first keys) 'quote))
          (case-keylist-helper sym keys)
        (if (or (= keys 'true) (= keys 'otherwise))
            `true
            `(= ,sym ,keys))))

(defun case-clause-helper (sym clause)
    (cons (case-key-helper sym (first clause)) (rest clause)))

(defmacro case (keyform &rest clauses)
    (let sym (gentemp))
    `(do
        (let ,sym ,keyform)
        (cond ,@(map (partially-apply case-clause-helper sym) clauses))))

(defmacro ecase (keyform &rest clauses)
    (let sym (gentemp))
    `(do
        (let ,sym ,keyform)
        (cond
            ,@(map (partially-apply case-clause-helper sym) clauses)
            (true (throw-error "Unhandled case.")))))

(defun typecase-helper (sym type)
    (if (or (= type 'true) (= type 'otherwise))
        `true
        `(type? ,sym ',type)))

(defun typecase-clause-helper (sym clause)
    (cons (typecase-helper sym (first clause)) (rest clause)))

(defmacro typecase (keyform &rest clauses)
    (let sym (gentemp))
    `(do
        (let ,sym ,keyform)
        (cond ,@(map (partially-apply typecase-clause-helper sym) clauses))))

(defmacro etypecase (keyform &rest clauses)
    (let sym (gentemp))
    `(do
        (let ,sym ,keyform)
        (cond
            ,@(map (partially-apply typecase-clause-helper sym) clauses)
            (true (throw-error "Unhandled typecase.")))))

;;;
;;; trace functions
;;;

(def $tracing false
    "Enables/disables tracing by the `trace` macro.")

(defmacro trace (expr)
    (when $debugging
        (let result (gentemp))
        `(when $tracing
            (let ,result ,expr)
            (let message (string
                            ,(write-to-string expr :escape true)
                            " => "
                            (write-to-string ,result :escape true)))
            (print-trace message))))

;;;
;;; generic methods
;;;

(defmacro call-next-method ()
    `(system:call-next-method __lambdas__ __args__))

;;;
;;; exception handling
;;;

(defmacro try-and-catch (&body forms)
    `(try
        ,@forms
        (catch (ex) ex)))

(defun return-or-throw (result)
    (if (exception? result)
        (throw result)
        (return result)))

(defmacro ignore-errors (&body forms)
    `(try
        ,@forms
        (catch () null)))

(defmacro using ((variable resource) &body forms)
    `(do
        (let ,variable ,resource)
        (try
            ,@forms
            (finally (system:dispose ,variable)))))

;;;
;;; assert
;;;

(defun do-assert (expr)
    `(when (not ,expr)
        (throw (assert-failed-exception:new
                    ,(string:format "Failed condition: {0} " expr)))))

(defmacro assert (&rest exprs)
    (when $debugging
        `(do? ,@(map do-assert exprs))))

(defmacro throws-exception? (expr)
    `(try
        ,expr
        false
        (catch (ex) true)))

(defmacro assert-throws-exception (expr)
    (when $debugging
        `(if (not (throws-exception? expr))
            (throw (assert-failed-exception:new
                            ,(string:format "Failed throws-exception: {0} " expr))))))


(defmacro assert-throws-no-exception (expr)
    (when $debugging
        `(if (throws-exception? expr)
            (throw (assert-failed-exception:new
                            ,(string:format "Failed throws-no-exception: {0} " expr))))))


;;;
;;; string input/output
;;;

(defmacro with-input-from-string ((variable text) &body forms)
    `(using (,variable (string-reader:new ,text))
        ,@forms))

(defmacro with-output-to-string ((variable) &body forms)
    `(using (,variable (string-writer:new))
        ,@forms
        (.to-string ,variable)))

;;;
;;; named blocks
;;;

(defmacro block (tag &body forms)
    `(do
        ,@forms
        (label ,tag dummy-value)))

(defmacro return-from (tag &optional value)
    `(goto ,tag ,value))

;;;
;;; lists
;;;

(defmacro pop (place)
    (let temp (gentemp))
    `(do
        (var ,temp (car ,place))
        (setf ,sym (cdr ,place))
        ,temp))

(defmacro push (item place)
    `(setf ,place (cons ,item ,place)))

(defmacro pushnew (item place)
    `(if (index-of ,item ,place)
            ,place
        (push item ,place)))

;;;
;;; multiple macros
;;;

(defmacro with-multiple-let ((&rest vars) form &body forms)
	(with-multiple-thingy 'let vars form forms))

(defmacro with-multiple-var ((&rest vars) form &body forms)
	(with-multiple-thingy 'var vars form forms))

(defun with-multiple-thingy (thingy vars form forms)
    (let temp (gentemp))
    (let count (length vars))
    (letfun do-var (index place)
        `(,thingy ,place (elt ,temp ,index)))
    `(do
        (let ,temp (as-multiple-elements ,form ,count))
        ,@(map-indexed
            do-var vars)
        ,@forms))

(defmacro multiple-setf ((&rest places) form)
    (let temp (gentemp))
    (let count (length places))
    (letfun do-var (index place)
        `(setf ,place (elt ,temp ,index)))
    `(do
        (let ,temp (as-multiple-elements ,form ,count))
        ,@(map-indexed
            do-var places)))
;;;
;;; compiler macros
;;;

(define-compiler-macro + (&rest numbers &whole original-form)
    (if (and (= (length numbers) 2)
             (number? (second numbers))
             (= (second numbers) 1))
        `(inc ,(first numbers))
        original-form))

(define-compiler-macro - (&rest numbers &whole original-form)
    (if (and (= (length numbers) 2)
             (number? (second numbers))
             (= (second numbers) 1))
        `(dec ,(first numbers))
        original-form))
