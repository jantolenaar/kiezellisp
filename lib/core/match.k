
;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; match.k
;;;;

(defpackage "lisp"
    (:export
        "case-match"
        "ecase-match"
        "if-match"
        "when-match"
    ))

(in-package "lisp")

(defmacro case-match (expr &rest clauses)
    (do-case-match expr clauses false))

(defmacro ecase-match (expr &rest clauses)
    (do-case-match expr clauses true))

(defun do-case-match (expr clauses error)
    (let $temp (gentemp "expr"))
    `(block case-match
        (let ,$temp ,expr)
        ,@(map match-clause clauses)
        ,@(if error '((throw-error "Unhandled case-match.")))))

(defmacro if-match (pattern expr then-form &optional else-form)
    (cond
        (else-form
            `(case-match ,expr
                (,pattern ,then-form)
                (otherwise ,else-form)))
        (true
            `(case-match ,expr
                (,pattern ,then-form)))))

(defmacro when-match (pattern expr &body forms)
    `(case-match ,expr
        (,pattern ,@forms)))

(defun match-clause (clause)
    (let $variables (vector))
    (let pattern (first clause))
    (let original-body (rest clause))
    (let body (if (< 1 (length original-body))
                    `(do ,@original-body)
                    (first original-body)))
    (let test-form (encode-pattern $temp pattern))
    (cond
        ($variables
            `(do
            	,@(map #(list 'var %1) $variables)
                (if ,test-form (leave case-match ,body))))
        ((= test-form true)
            `(leave case-match ,body))
        (true
            `(if ,test-form (leave case-match ,body)))))

(def encode-handlers (new
    :quote 'encode-quote
    :guard 'encode-guard
    :and 'encode-and
    :or 'encode-or
    :not 'encode-not
    :satisfies 'encode-satisfies
    :cons 'encode-cons
    :vector 'encode-vector
    :list 'encode-list
    :sequence 'encode-sequence
    :enumerable 'encode-enumerable
    :eq 'encode-eq
    :eql 'encode-eql
    :equal 'encode-equal
    := 'encode-equal
    :equal-ci 'encode-equal-ci
    :type 'encode-type
    :regex 'encode-regex
    :property 'encode-property
    ))

(defun encode-pattern (expr pattern)
    (cond
        ((or (eql pattern true) (eql pattern 'otherwise))
            true)
        ((symbol? pattern)
            (encode-symbol expr pattern))
        ((literal? pattern)
            (encode-literal expr pattern))
        ((cons? pattern)
            (let key (first pattern))
            (let args (rest pattern))
            (if-let (proc (elt encode-handlers key))
                (funcall (symbol-value proc) expr args)))))

(defun encode-symbol (expr pattern)
    (when (not (index-of pattern $variables))
        (.add $variables pattern))
    `(do
        (setf ,pattern ,expr)
        true))

(defun encode-quote (expr args)
    (encode-pattern expr `(equal ',(first args))))

(defun encode-literal (expr pattern)
    (encode-pattern expr `(equal ,pattern)))

(defun encode-cons (expr pattern)
    (let pcar (first pattern))
    (let pcdr (second pattern))
    (let temp (gentemp "cons"))
    `(do
        (let ,temp ,expr)
        (and (cons? ,temp)
             ,(encode-pattern `(car ,temp) pcar)
             ,(encode-pattern `(cdr ,temp) pcdr))))

(defun encode-guard (expr args)
    (let sym (first args))
    (let test (second args))
    `(do
        (let ,sym ,expr)
        ,test))

(defun encode-satisfies (expr args)
    (let func (first args))
    (encode-pattern expr `(guard x (,func x))))

(defun encode-vector (expr args)
    (encode-enumerable 'vector? expr args))

(defun encode-list (expr args)
    (encode-enumerable 'list? expr args))

(defun encode-sequence (expr args)
    (encode-enumerable 'sequence? expr args))

(defun get-vector-items (items patterns)
    (loop
        (for p :in patterns)
        (for i :in (range))
        (collecting (encode-pattern `(elt ,items ,i) p))))

(defun encode-enumerable (testf expr args)
    (let items (gentemp "seq"))
    (let patterns (as-vector args))
    `(do
        (let ,items ,expr)
        (and (,testf ,items)
             (do
                (let ,items (as-vector ,items))
                (and (= (length ,items) ,(length patterns))
                     ,@(get-vector-items items patterns))))))

(defun encode-eq (expr args)
    `(eq ,expr ,(first args)))

(defun encode-eql (expr args)
    `(eql ,expr ,(first args)))

(defun encode-equal (expr args)
    `(equal ,expr ,(first args)))

(defun encode-equal-ci (expr args)
    `(equal-ci ,expr ,(first args)))

(defun encode-type (expr args)
    `(type? ,expr ,(first args)))

(defun encode-and (expr args)
    `(and ,@(map #(encode-pattern expr %1) args)))

(defun encode-or (expr args)
    `(or ,@(map #(encode-pattern expr %1) args)))

(defun encode-not (expr args)
    `(not ,(encode-pattern expr (first args))))

(defun encode-regex (expr args)
    (let str (gentemp "str"))
    `(do
        (let ,str ,expr)
        (and (string? ,str)
             (.regex-match ,str ,(first args)))))

(defun encode-property (expr args)
    (let obj (gentemp "obj"))
    (let patterns (map #(flatten %1 1) (partition-all 2 (partition-by keyword? args))))
    `(do
        (let ,obj ,expr)
        (and (prototype? ,obj)
             ,@(map #(get-property-items obj %1) patterns))))

(defun get-property-items (obj args)
    (let name (first args))
    (let patterns (rest args))
    (let prop (gentemp "prop"))
    `(do
        (let ,prop (attr ,obj ,name))
        ,(encode-and prop patterns)))
