
;;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; match.k
;;;;

(defpackage "lisp"
    (:export 
        "="
        "and"
        "case-match"
        "cons"
        "enumerable"
        "eq"
        "eql"
        "equal"
        "equal-ci"
        "guard"
        "if-match"
        "type"
        "list"
        "not"
        "or"
        "property"
        "quote"
        "regex"
        "satisfies"
        "sequence"
        "vector"
        "when-match"
    ))

(in-package "lisp")

(defmacro case-match (expr &rest clauses)
    (var $temp (gentemp "expr"))
    `(do
        (var ,$temp ,expr)
        (var $loop-result null)
        (tagbody
            (do 
                ,@(map match-clause clauses))
            break-label)
        $loop-result))

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
    (var $variables (vector))
    (var pattern (first clause))
    (var original-body (rest clause))
    (var body (if (< 1 (length original-body)) 
                    `(do ,@original-body) 
                    (first original-body)))
    (var test-form (encode-pattern $temp pattern))
    (force (cond
        ($variables
            `(do
                (var ,(as-list $variables) null)
                (break-if ,test-form ,body)))
        ((= test-form true)
            `(break ,body))
        (true
            `(break-if ,test-form ,body)))))


(defun encode-pattern (expr pattern)
    (cond
        ((or (eql pattern true) (eql pattern 'otherwise))
            true)
        ((symbol? pattern)
            (encode-symbol expr pattern))
        ((literal? pattern)
            (encode-literal expr pattern))
        ((cons? pattern)
            (var key (first pattern))
            (var args (rest pattern))
            (case key
                (quote
                    (encode-quote expr args))
                (guard
                    (encode-guard expr args))
                (and
                    (encode-and expr args))
                (or
                    (encode-or expr args))
                (not
                    (encode-not expr args))
                (satisfies
                    (encode-satisfies expr args))
                (cons
                    (encode-cons expr args))
                (vector
                    (encode-vector expr args))
                (list
                    (encode-list expr args))
                (sequence
                    (encode-sequence expr args))
                (enumerable
                    (encode-enumerable 'enumerable? expr args))
                (eq 
                    (encode-eq expr args))
                (eql 
                    (encode-eql expr args))
                ((equal =) 
                    (encode-equal expr args))
                (equal-ci
                    (encode-equal-ci expr args))
                (type 
                    (encode-type expr args))
                (regex 
                    (encode-regex expr args))
                (property
                    (encode-property expr args))
            ))))

(defun encode-symbol (expr pattern)
    (if (not (find pattern $variables))
        (.add $variables pattern))
    `(do
        (setf ,pattern ,expr)
        true))

(defun encode-quote (expr args)
    (encode-pattern expr `(equal ',(first args))))

(defun encode-literal (expr pattern)
    (encode-pattern expr `(equal ,pattern)))

(defun encode-cons (expr pattern)
    (var pcar (first pattern))
    (var pcdr (second pattern))
    (var temp (gentemp "cons"))
    `(do
        (var ,temp ,expr)
        (and (cons? ,temp)
             ,(encode-pattern `(car ,temp) pcar)
             ,(encode-pattern `(cdr ,temp) pcdr))))

(defun encode-guard (expr args)
    (var sym (first args))
    (var test (second args))
    `(do
        (var ,sym ,expr)
        ,test))

(defun encode-satisfies (expr args)
    (var func (first args))
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
        (collect (encode-pattern `(elt ,items ,i) p))))

(defun encode-enumerable (testf expr args)
    (var items (gentemp "seq"))
    (var patterns (as-vector args))
    `(do
        (var ,items ,expr)
        (and (,testf ,items)
             (do
                (var ,items (as-vector ,items))
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
    `(and ,@(map (λx (encode-pattern expr x)) args)))

(defun encode-or (expr args)
    `(or ,@(map (λx (encode-pattern expr x)) args)))

(defun encode-not (expr args)
    `(not ,(encode-pattern expr (first args))))

(defun encode-regex (expr args)
    (var str (gentemp "str"))
    `(do
        (var ,str ,expr)
        (and (string? ,str)
             (.regex-match ,str ,(first args)))))

(defun encode-property (expr args)
    (var obj (gentemp "obj"))
    (var patterns (map (λx (flatten x 1)) (partition-all 2 (partition-by keyword? args))))
    `(do
        (var ,obj ,expr)
        (and (prototype? ,obj)
             ,@(map (λx (get-property-items obj x)) patterns))))

(defun get-property-items (obj args)
    (var name (first args))
    (var patterns (rest args))
    (var prop (gentemp "prop"))
    `(do
        (var ,prop (attr ,obj ,name))
        ,(encode-and prop patterns)))


