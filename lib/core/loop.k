;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; loop.k
;;;;


(package "lisp")

(public "foreach"
        "loop"
        "loopv"
        "while")

(def $env null)

(defmacro loop (&body forms &environment env)
    (loop-imp forms env true))

(defmacro loopv (&body forms &environment env)
    (loop-imp forms env false))


(defun loop-imp (forms env list-result?)
    (let $list-result? list-result?)
    (let $env (make-environment env))
    (let $initial-steps (vector))
    (let $tests (vector))
    (let $body (vector))
    (let $next-steps (vector))
    (let $final-steps (vector))
    (var $collect-vars null)
    (var $summing-vars null)
    (var $multiplying-vars null)
    (var $maximizing-vars null)
    (var $minimizing-vars null)
    (when (null? forms)
        (throw-error "LOOP body cannot be empty"))
    (each do-form forms)
    (add-accumulator-steps)
    (generate-code)
    #;(loop-macroexpand-all ~))

(defmacro while (test &body forms)
    `(loop
        (break-if (not ,test))
        ,@forms))

(defmacro foreach ((v seq) &body forms)
    `(loop
        (for ,v :in ,seq)
        ,@forms))

;;;
;;; internal functions below this line
;;;

(def loop-clauses (new
    :break 'break-clause
    :break-if 'break-if-clause
    :continue 'continue-clause
    :continue-if 'continue-if-clause
    :finish 'finish-clause
    :finish-if 'finish-if-clause
    :collect-each 'collect-each-clause
    :collecting 'collecting-clause
    :counting 'counting-clause
    :summing 'summing-clause
    :multiplying 'multiplying-clause
    :minimizing 'minimizing-clause
    :maximizing'maximizing-clause
    :next 'next-clause
    ))

(def loop-top-level-clauses (new
    :for 'for-clause
    :initially 'initially-clause
    :finally 'finally-clause
    :generate 'generate-clause
    :with 'with-clause
    ))

(each public (prototype:keys loop-clauses))
(each public (prototype:keys loop-top-level-clauses))

(defun loop-macroexpand-1 (form env)
    (when (and (cons? form)
               (symbol? (first form))
               (/= 'loop (first form)))
        (when-let (proc (elt loop-clauses (first form)))
            (return (list (funcall (symbol-value proc) form) true))))
    (return (macroexpand-1 form env)))

(defun loop-macroexpand (form env)
    (block LOOP
        (let result (loop-macroexpand-1 form env))
        (setf form (first result))
        (if (second result)
            (redo LOOP))
        (return form)))

(defun loop-macroexpand-all (form)
    (code-walk form loop-macroexpand $env))

(defun loop-macroexpand-list (forms)
    (map loop-macroexpand-all forms))

(defmacro define-clause-pattern (name &body forms)
    `(defun ,name (form)
        (var arg1)
        (var arg2)
        (var arg3)
        (var arg4)
        (declare (ignore arg1 arg2 arg3 arg4))
        (letfun match (x)
            (let seq (pattern-match x form))
            (when (list? seq)
                (multiple-setf (arg1 arg2 arg3 arg4) seq)
                true))
        (cond ,@forms)))

(defun generate-code ()
    `(block LOOP
        ,@$initial-steps
        (block TESTS
            ,@$tests
            (block MAIN
                ,@$body)
            ,@$next-steps
            (redo TESTS))
        ,@$final-steps))

(defun pattern-match (pattern form)
    (cond
        ((and (= null pattern)
              (= null form))
             null)
        ((= (first pattern) '?rest)
            (let result (self (rest pattern) null))
            (if (list? result)
                  (cons form result)
                false))
        ((or (= null pattern)
             (= null form))
            false)
        ((= (first pattern) '?)
            (let result (self (rest pattern) (rest form)))
            (if (list? result)
                  (cons (first form) result)
                false))
        ((and (symbol? (first form))
              (= (symbol-name (first form))
                 (symbol-name (first pattern))))
            (self (rest pattern) (rest form)))
        (true
            false)))


(defun do-form (form)
    (if (and (list? form)
             (symbol? (first form)))
        (when-let (proc (elt loop-top-level-clauses (first form)))
            (funcall (symbol-value proc) form)
            (return)))
    (do-code form))

(defun initially-clause (form)
    (do-initially (loop-macroexpand-list (rest form))))

(defun finally-clause (form)
    (do-finally (loop-macroexpand-list (rest form))))

(define-clause-pattern for-clause 
    ((match '(:for ? :times))
        (do-enumerator (gentemp "times") `(range-enumerator 0 ,arg1 1)))
    ((match '(:for ? :from ? :to ? :by ?))
        (do-enumerator arg1 `(series-enumerator ,arg2 ,arg3 ,arg4)))
    ((match '(:for ? :from ? :to ?))
        (do-enumerator arg1 `(series-enumerator ,arg2 ,arg3 1)))
    ((match '(:for ? :from ? :downto ?))
        (do-enumerator arg1 `(series-enumerator ,arg2 ,arg3 -1)))
    ((match '(:for ? :from ? :above ?))
        (do-enumerator arg1 `(range-enumerator ,arg2 ,arg3 -1)))
    ((match '(:for ? :from ? :below ?))
        (do-enumerator arg1 `(range-enumerator ,arg2 ,arg3 1)))
    ((match '(:for ? :from ?))
        (do-enumerator arg1 `(range-enumerator ,arg2 int32:MAX-VALUE 1)))
    ((match '(:for ? :in ?))
        (do-enumerator arg1 arg2))
    ((match '(:for ? :in-list ? :by ?))
        (do-enumerator arg1 `(in-list-enumerator ,arg2 ,arg3)))
    ((match '(:for ? :in-list ?))
        (do-enumerator arg1 `(in-list-enumerator ,arg2 cdr)))
    ((match '(:for ? :on-list ? :by ?))
        (do-enumerator arg1 `(on-list-enumerator ,arg2 ,arg3)))
    ((match '(:for ? :on-list ?))
        (do-enumerator arg1 `(on-list-enumerator ,arg2 cdr)))
    (true
        (throw-error "Invalid <for> clause in loop")))

(define-clause-pattern generate-clause
        ((match '(:generate ? :from ? :to ? :by ?))
            (do-generator arg1 `(series-enumerator ,arg2 ,arg3 ,arg4)))
        ((match '(:generate ? :from ? :to ?))
            (do-generator arg1 `(series-enumerator ,arg2 ,arg3 1)))
        ((match '(:generate ? :from ? :downto ?))
            (do-generator arg1 `(series-enumerator ,arg2 ,arg3 -1)))
        ((match '(:generate ? :from ? :above ?))
            (do-generator arg1 `(range-enumerator ,arg2 ,arg3 -1)))
        ((match '(:generate ? :from ? :below ?))
            (do-generator arg1 `(range-enumerator ,arg2 ,arg3 1)))
        ((match '(:generate ? :from ?))
            (do-generator arg1 `(range-enumerator ,arg2 int32:MAX-VALUE 1)))
        ((match '(:generate ? :in ?))
            (do-generator arg1 arg2))
        ((match '(:generate ? :in-list ? :by ?))
            (do-generator arg1 `(in-list-enumerator ,arg2 ,arg3)))
        ((match '(:generate ? :in-list ?))
            (do-generator arg1 `(in-list-enumerator ,arg2 cdr)))
        ((match '(:generate ? :on-list ? :by ?))
            (do-generator arg1 `(on-list-enumerator ,arg2 'arg3)))
        ((match '(:generate ? :on-list ?))
            (do-generator arg1 `(on-list-enumerator ,arg2 cdr)))
        (true
            (throw-error "Invalid <generate> clause in loop")))

(define-clause-pattern with-clause
        ((match '(:with ? :initially ? :then ?))
            (do-init-then arg1 arg2 arg3))
        (true
            (throw-error "Invalid <with> clause in loop")))

(defun do-code (form)
    (.add $body (loop-macroexpand-all form))
    true)

(defun do-initially (forms)
    (.add-range $initial-steps forms))

(defun do-finally (forms)
    (.add $final-steps `(do ,@forms))
    true)

(defun do-init-then (sym init-form then-form)
    (.add $initial-steps `(var ,sym ,init-form))
    (.add $next-steps `(setf ,sym ,then-form))
    true)


(defun do-enumerator (sym range-expr)
    (do-generator sym range-expr)
    (.add $tests (next-clause `(next ,sym)))
    true)

(defun check-symbol (sym)
    (if (not (symbol? sym))
        (throw-error "Expected a symbol instead of: " (with-output-to-string (s) (write sym :stream s)))))

(defun do-generator (sym range-expr)
    (check-symbol sym)
    (let range-var (prefix-symbol "%range" sym))
    (.add $initial-steps `(let ,range-var (system:get-safe-enumerator ,range-expr)))
    (.add $initial-steps `(var ,sym null))
    true)

(defun prefix-symbol (prefix sym)
    (make-symbol prefix "-" (if sym (symbol-name sym) "temp")))

(defun tail-symbol (sym)
    (prefix-symbol sym :tail))

;;;
;;; level 1 accumulator handlers
;;;

(define-clause-pattern next-clause
        ((match '(next ?))
            (let sym arg1)
            (check-symbol sym)
            (let range-var (prefix-symbol "%range" sym))
            `(do
                (if (not (ienumerator:move-next ,range-var))
                    (leave TESTS))
                (setf ,arg1 (ienumerator:current ,range-var))
                ,arg1))
        (true
            (throw-error "Invalid <next> clause in loop")))

(define-clause-pattern break-clause
        ((match '(break))
            `(leave LOOP))
        ((match '(break ?))
            `(leave LOOP ,arg1))
        (true
            (throw-error "Invalid <break> clause in loop")))

(define-clause-pattern break-if-clause
        ((match '(break-if ?))
            `(if ,arg1 (break)))
        ((match '(break-if ? ?))
            `(if ,arg1 (break ,arg2)))
        (true
            (throw-error "Invalid <break-if> clause in loop")))

(define-clause-pattern continue-clause
        ((match '(continue))
            `(leave MAIN))
        (true
            (throw-error "Invalid <continue> clause in loop")))

(define-clause-pattern continue-if-clause
        ((match '(continue-if ?))
            `(if ,arg1 (continue)))
        (true
            (throw-error "Invalid <continue-if> clause in loop")))

(define-clause-pattern collect-each-clause
        ((match '(collect-each ?))
            (pushnew '%accumulator $collect-vars)
            `(.add-range %accumulator ,arg1))
        (true
            (throw-error "Invalid <collect-each> clause in loop")))

(defun do-collect (sym val)
    (if $list-result?
        (do
            (let tail-sym (tail-symbol sym))
            `(cond
                ((null? ,tail-sym)
                    (setf ,tail-sym (cons ,val null))
                    (setf ,sym ,tail-sym))
                (true
                    (setf (cdr ,tail-sym) (cons ,val null))
                    (setf ,tail-sym (cdr ,tail-sym)))))
        (do
            `(do
                (if (null? ,sym)
                    (setf ,sym (vector)))
                (.add ,sym ,val)))))

(define-clause-pattern collecting-clause
        ((match '(collecting ? :into ?))
            (check-symbol arg2)
            (pushnew arg2 $collect-vars)
            (do-collect arg2 arg1))
        ((match '(collecting ?))
            (pushnew '%accumulator $collect-vars)
            (do-collect '%accumulator arg1))
        (true
            (throw-error "Invalid <collecting> clause in loop")))

(define-clause-pattern counting-clause
        ((match '(counting ? :into ?))
            (check-symbol arg2)
            (pushnew arg2 $summing-vars)
            `(if ,arg1
                (incf ,arg2)))
        ((match '(counting ?))
            (pushnew '%accumulator $summing-vars)
            `(if ,arg1
                (incf %accumulator)))
        (true
            (throw-error "Invalid <counting> clause in loop")))

(define-clause-pattern finish-clause
        ((match '(finish))
            `(leave TESTS))
        (true
            (throw-error "Invalid <finish> clause in loop")))

(define-clause-pattern finish-if-clause
        ((match '(finish-if ?))
            `(if ,arg1 (finish)))
        (true
            (throw-error "Invalid <finish-if> clause in loop")))

(define-clause-pattern summing-clause
        ((match '(summing ? :into ?))
            (check-symbol arg2)
            (pushnew arg2 $summing-vars)
            `(incf ,arg2 ,arg1))
        ((match '(summing ?))
            (pushnew '%accumulator $summing-vars)
            `(incf %accumulator ,arg1))
        (true
            (throw-error "Invalid <summing> clause in loop")))

(define-clause-pattern multiplying-clause
        ((match '(multiplying ? :into ?))
            (check-symbol arg2)
            (pushnew arg2 $multiplying-vars)
            `(setf ,arg2 (* ,arg2 ,arg1)))
        ((match '(multiplying ?))
            (pushnew '%accumulator $multiplying-vars)
            `(setf %accumulator (* %accumulator ,arg1)))
        (true
            (throw-error "Invalid <multiplying> clause in loop")))

(define-clause-pattern maximizing-clause
        ((match '(maximizing ? :into ?))
            (do-maximizing arg2 arg1))
        ((match '(maximizing ?))
            (do-maximizing '%accumulator arg1))
        (true
            (throw-error "Invalid <maximizing> clause in loop")))

(define-clause-pattern minimizing-clause
        ((match '(minimizing ? :into ?))
            (do-minimizing arg2 arg1))
        ((match '(minimizing ?))
            (do-minimizing '%accumulator arg1))
        (true
            (throw-error "Invalid <minimizing> clause in loop")))

;;;
;;; level 2 accumulator handlers
;;;

(defun do-minimizing (variable value)
    (check-symbol variable)
    (pushnew variable $minimizing-vars)
    (let temp-var (gentemp variable))
    `(do
        (var ,temp-var ,value)
        (if (or (null? ,variable)
                (and (not (null? ,temp-var))
                     (< ,temp-var ,variable)))
            (setf ,variable ,temp-var))))

(defun do-maximizing (variable value)
    (check-symbol variable)
    (pushnew variable $maximizing-vars)
    (let temp-var (gentemp variable))
    `(do
        (var ,temp-var ,value)
        (if (or (null? ,variable)
                (and (not (null? ,temp-var))
                     (> ,temp-var ,variable)))
            (setf ,variable ,temp-var))))

;;;
;;; initial and final code generators
;;;

(defun add-accumulator-steps ()
    (add-initial-collect-steps)
    (add-initial-summing-steps)
    (add-initial-multiplying-steps)
    (add-initial-minimaximizing-steps)
    (add-final-steps))

(defun add-initial-collect-steps ()
    (each initial-collect-step $collect-vars))

(defun add-final-steps ()
    (when (or (index-of '%accumulator $collect-vars)
              (index-of '%accumulator $summing-vars)
              (index-of '%accumulator $multiplying-vars)
              (index-of '%accumulator $maximizing-vars)
              (index-of '%accumulator $minimizing-vars))
        (.add $final-steps `%accumulator)))

(defun add-initial-summing-steps ()
    (each initial-summing-step $summing-vars))

(defun add-initial-multiplying-steps ()
    (each initial-multiplying-step $multiplying-vars))

(defun add-initial-minimaximizing-steps ()
    (each initial-minimaximizing-step $maximizing-vars)
    (each initial-minimaximizing-step $minimizing-vars))

(defun initial-collect-step (sym)
    (.add $initial-steps `(var ,sym null))
    (.add $initial-steps `(var ,(tail-symbol sym) null)))

(defun initial-summing-step (sym)
    (.add $initial-steps `(var ,sym 0)))

(defun initial-multiplying-step (sym)
    (.add $initial-steps `(var ,sym 1)))

(defun initial-minimaximizing-step (sym)
    (.add $initial-steps `(var ,sym null)))
