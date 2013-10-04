;;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; loop.k
;;;;


(defpackage "lisp"
    (:export
        "break"
        "break-if"
        "collect"
        "collect-each"
        "continue"
        "continue-if"
        "foreach"
        "loop"
        "while"
    ))

(in-package "lisp")

(defmacro break (&optional val)
    (if val
          `(do
              (setf $loop-result ,val)
              (go break-label))
       `(go break-label)))

(defmacro continue ()
    `(go continue-label))

(defmacro continue-if (test)
    `(if ,test (continue)))

(defmacro break-if (test &optional val)
    (if val
         `(if ,test (break ,val))
       `(if ,test (break))))

(defmacro xxx-simple-while (test &body forms)
    `(tagbody
        continue-label
        (break-if (not ,test))
        (do ,@forms)
        (continue)
        break-label))

(defun collect (&rest items)
    (collect-each items))

(defun collect-each (items)
    (var data (as-list items))
    (if data
        (if $loop-tail
              (setf $loop-tail (last (setf (cdr $loop-tail) data)))
            (setf $loop-tail (last (setf $loop-result data))))))

(defmacro loop (&body forms)
    (var $decls (vector '(var $loop-result null) '(var $loop-tail null)))
    (var $tests (vector))
    (var $pre-steps (vector))
    (var $body (vector))
    (var $post-steps (vector))
    (var $finally-steps (vector))
    (var $continue-used false)
    (var $break-used false)
    (each do-form forms)
    (generate-code))

(defmacro while (test &body forms)
    `(loop
        (break-if (not ,test))
        ,@forms))

(defmacro foreach ((v seq) &body forms)
    `(loop
        (for ,v in ,seq)
        ,@forms))

;;;
;;; internal functions below this line
;;;

(defun generate-code ()
    `(do
        ,@(as-list $decls)
        (tagbody
            test-label
            ,@(as-list $tests)
            (do
                ,@(as-list $pre-steps)
                (tagbody (do ,@$body) continue-label)
                ,@(as-list $post-steps))
            (go test-label)
            break-label)
        ,@(as-list $finally-steps)
        $loop-result))

(defun pattern-match (pattern form)
    (cond
        ((and (= null pattern)
              (= null form))
             null)
        ((= (first pattern) '?rest)
            (var result (pattern-match (rest pattern) null))
            (if (= result false)
                  false
                (cons form result)))
        ((or (= null pattern)
             (= null form))
            false)
        ((= (first pattern) '?)
            (var result (pattern-match (rest pattern) (rest form)))
            (if (= result false)
                  false
                (cons (first form) result)))
        ((and (symbol? (first form))
              (= (symbol-name (first form))
                 (symbol-name (first pattern))))
            (pattern-match (rest pattern) (rest form)))
        (true 
            false)))


(defun do-form (form)
    (var $1)
    (var $2)
    (var $3)
    (var $4)
    (var match (λx (var seq (pattern-match x form))
                   (setq ($1 $2 $3 $4) (or seq null))))
    (cond
        ((not (list? form))
            (do-code form))
        ((match '(:repeat ?))
            (do-repeat $1))
        ((match '(:finally ?rest))
            (do-finally $1))
        ((match '(:with ? ?))
            (do-with $1 $2))
        ((match '(:for ? :in ?))
            (do-enumerator $1 $2))
        ((match '(:for ? :in-list ? :by ?))
            (do-in-list $1 $2 $3))
        ((match '(:for ? :in-list ?))
            (do-in-list $1 $2 'cdr))
        ((match '(:for ? :on-list ? :by ?))
            (do-on-list $1 $2 $3))
        ((match '(:for ? :on-list ?))
            (do-on-list $1 $2 'cdr))
        ((match '(:for ? :initially ? :then ?))
            (do-init-then $1 $2 $3))
        ((match '(:for ? :initially ?))
            (do-init $1 $2))
        ((match '(:for ? = ?))
            (do-eval $1 $2))
        ((match '(:for ?rest))
            (do-bad-for))
        (true
            (do-code form))))

(defun do-code (form)
    (.add $body form)
    true)

(defun do-bad-for ()
    (throw-error "Invalid for construction in loop"))

(defun do-repeat (count)
    (do-enumerator null (list 'range 0 count)))

(defun do-finally (forms)
    (.add $finally-steps `(do ,@forms))
    true)

(defun do-with (sym val)
    (.add $decls `(var ,sym ,val))
    true)

(defun do-on-list (sym list-form next-func)
    (var list-var (gentemp))
    (.add $decls `(var ,list-var ,list-form))
    (.add $tests `(break-if (empty? ,list-var)))
    (.add $pre-steps `(var ,sym ,list-var))
    (.add $post-steps `(setq ,list-var (,next-func ,list-var)))
    true)

(defun do-in-list (sym list-form next-func)
    (var list-var (gentemp))
    (.add $decls `(var ,list-var ,list-form))
    (.add $tests `(break-if (empty? ,list-var)))
    (.add $pre-steps `(var ,sym (first ,list-var)))
    (.add $post-steps `(setq ,list-var (,next-func ,list-var)))
    true)

(defun do-init-then (sym init-form then-form)
    (var list-var (gentemp))
    (.add $decls `(var ,list-var ,init-form))
    (.add $pre-steps `(var ,sym ,list-var))
    (.add $post-steps `(setq ,list-var ,sym))
    (.add $post-steps `(setq ,list-var ,then-form))
    true)

(defun do-init (sym init-form)
    (var list-var (gentemp))
    (.add $decls `(var ,list-var ,init-form))
    (.add $pre-steps `(var ,sym ,list-var))
    (.add $post-steps `(setq ,list-var ,sym))
    true)

(defun do-eval (sym form)
    (.add $pre-steps `(var ,sym ,form))
    true)

(defun do-enumerator (sym range-expr)
    (var range-var (gentemp))
    (.add $decls `(var ,range-var (system.get-safe-enumerator ,range-expr)))
    (.add $tests `(break-if (not (ienumerator.move-next ,range-var))))
    (if sym
        (.add $pre-steps `(var ,sym (ienumerator.current ,range-var))))
    true)




