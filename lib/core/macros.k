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
        "cond"
        "defonce"
        "deftype"
        "ecase"
        "etypecase"
        "if-let"
        "ignore-errors"
        "macroexpand-all"
        "multiple-let"
        "multiple-setf"
        "multiple-var"
        "new"
        "otherwise"
        "return-from"
        "return-if"
        "return-or-throw"
        "trace"
        "try-and-catch"
        "try-match"
        "typecase"
        "using"
        "when"
        "when-let"
        "with-input-from-string"
        "with-output-to-string"
    ))

(in-package "lisp")

;;;
;;; removable do; eliminates do block when it is not necessary.
;;;

(defun declaration? (form)
    (and (cons? form)
         (find (first form) '(let var hidden-var letfun future lazy))))

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
;;; conditions
;;;

(defmacro when (test &body forms)
    `(if ,test (do? ,@forms)))

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
        (let form (first forms))
        (if (eql (first form) true)
              `(do? ,@(rest form))
            (if (rest form)
                  `(if ,(first form) (do? ,@(rest form)) ,(recur (rest forms)))
                (do
                    (let temp (gentemp))
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
    (when $debug-mode
        (let result (gentemp))
        `(when $tracing
            (let ,result ,expr)
            (write-line (string
                            ,(write-to-string expr :escape true)
                            " => "
                            (write-to-string ,result :escape true)) :stream $stdlog))))

;;;
;;; tail recursion
;;;

(defmacro tailcall (proc &rest args)
    `(return (system:create-tailcall ,proc ,@args)))


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
    (when $debug-mode
        `(do? ,@(map do-assert exprs))))

(defmacro throws-exception? (expr)
    `(try
        ,expr
        false
        (catch (ex) true)))

(defmacro assert-throws-exception (expr)
    (when $debug-mode
        `(if (not (throws-exception? expr))
            (throw (assert-failed-exception:new
                            ,(string:format "Failed throws-exception: {0} " expr))))))


(defmacro assert-throws-no-exception (expr)
    (when $debug-mode
        `(if (throws-exception? expr)
            (throw (assert-failed-exception:new
                            ,(string:format "Failed throws-no-exception: {0} " expr))))))


;;;
;;; string input/output
;;;

(defmacro with-input-from-string ((variable text) &body forms)
    `(using (,variable (lisp-reader:new ,text))
        ,@forms))

(defmacro with-output-to-string ((variable) &body forms)
    `(using (,variable (string-writer:new))
        ,@forms
        (.to-string ,variable)))

;;;
;;; named blocks
;;;

(defun make-exit-label (tag)
    tag
    ;(make-symbol (string (symbol-name (or tag :nil)) "-exit") "keyword")
    )

(defmacro block (tag &body forms)
    `(do
        ,@forms
        (label ,(make-exit-label tag))))

(defmacro return-from (tag &optional value)
    `(goto ,(make-exit-label tag) ,value))

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
    `(if (find ,item ,place)
            ,place
        (push item ,place)))

;;;
;;; multiple var macros
;;;

(defmacro multiple-let ((&rest vars) form)
    (multiple-declare 'let vars form))

(defmacro multiple-var ((&rest vars) form)
    (multiple-declare 'var vars form))

(defun multiple-declare (decl-keyword vars form)
    (let temp (gentemp))
    (let count (length vars))
    (letfun do-var (index name)
        `(,decl-keyword ,name (elt ,temp ,index)))
    `(merging-do
        (let ,temp (as-multiple-elements ,form ,count))
        ,@(map-indexed do-var vars)))

    
(defmacro multiple-setf ((&rest places) form)
    (let temp (gentemp))
    (let count (length places))
    (letfun do-var (index place)
        `(setf ,place (elt ,temp ,index)))
    `(do
        (let ,temp (as-multiple-elements ,form ,count))
        ,@(map-indexed do-var places)))
