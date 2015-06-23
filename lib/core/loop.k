;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; loop.k
;;;;


(defpackage "lisp"
    (:export
        "break"
        "break-if"
        "continue"
        "continue-if"
        "finish"
        "finish-if"
        "foreach"
        "loop"
        "while"
    ))

(in-package "lisp")

(def $env null)

(defmacro loop (&body forms &environment env)
    (let $env (or env (make-environment)))
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
    (var $continue-label-used false)
    (var $finish-label-used false)
    (var $break-label-used false)
    (each do-form forms)
    (add-accumulator-steps)
    (loop-macroexpand-all (generate-code)))

(defmacro while (test &body forms)
    `(loop
        (break-if (not ,test))
        ,@forms))

(defmacro foreach ((v seq) &body forms)
    `(loop
        (for ,v :in ,seq)
        ,@forms))

(defmacro break (&optional value)
    (setf $break-label-used true)
    `(goto break-label ,value))

(defmacro break-if (test &optional value)
    `(if ,test (break ,value)))

(defmacro continue ()
    (setf $continue-label-used true)
    `(goto continue-label))

(defmacro continue-if (test)
    `(if ,test (continue)))

(defmacro finish ()
    (setf $finish-label-used true)
    `(goto finish-label))

(defmacro finish-if (test)
    `(if ,test (finish)))
    

;;;
;;; internal functions below this line
;;;

(def loop-clauses (new
    :collect 'collect-clause
    :collect-each 'collect-each-clause
    :count 'count-clause
    :sum 'sum-clause
    :multiply 'multiply-clause
    :minimize 'minimize-clause
    :maximize 'maximize-clause
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

(def loop-ignorables '(defun defmacro defmulti defmethod lambda λ letfun))
;(def loop-ignorables null)

(defun loop-macroexpand-1 (form env)
    (when (and (cons? form)
               (symbol? (first form))
               (not (find-name-in-environment (first form) env)))
        (when (find (first form) loop-ignorables)
            (return (list form false)))
        (when-let (proc (elt loop-clauses (first form)))
            (return (list (funcall (symbol-value proc) form) true))))
    (return (macroexpand-1 form env)))

(defun loop-macroexpand (form env)
    (label continue-label)
    (let result (loop-macroexpand-1 form env))
    (setf form (first result))
    (if (second result)
        (goto continue-label))
    (return form))

(defun loop-macroexpand-all (form)
    (code-walk form loop-macroexpand $env))

(defmacro prologue ()
    `(merging-do
        (var arg1)
        (var arg2)
        (var arg3)
        (var arg4)
        (declare (ignore arg1 arg2  arg3 arg4))
        (letfun match (x) 
            (let seq (pattern-match x form))
            (when (list? seq)
                (multiple-setf (arg1 arg2 arg3 arg4) seq)
                true))))

(defun generate-code ()
    `(do
        ,@$initial-steps
        (label test-label)
        ,@$tests
        (do?
            ,@$body)
        ,@(if $continue-label-used '((label continue-label)))
        ,@$next-steps
        (goto test-label)
        ,@(if $finish-label-used '((label finish-label)))
        ,@$final-steps
        ,@(if $break-label-used '((label break-label)))))

(defun pattern-match (pattern form)
    (cond
        ((and (= null pattern)
              (= null form))
             null)
        ((= (first pattern) '?rest)
            (let result (recur (rest pattern) null))
            (if (list? result)
                  (cons form result)
                false))
        ((or (= null pattern)
             (= null form))
            false)
        ((= (first pattern) '?)
            (let result (recur (rest pattern) (rest form)))
            (if (list? result)
                  (cons (first form) result)
                false))
        ((and (symbol? (first form))
              (= (symbol-name (first form))
                 (symbol-name (first pattern))))
            (recur (rest pattern) (rest form)))
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
    (do-initially (rest form)))

(defun finally-clause (form)
    (do-finally (rest form)))

(defun for-clause (form)
    (prologue)
    (cond
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
            (throw-error "Invalid <for> clause in loop"))))

(defun generate-clause (form)
    (prologue)
    (cond
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
            (throw-error "Invalid <generate> clause in loop"))))

(defun with-clause (form)
    (prologue)
    (cond
        ((match '(:with ? :initially ? :then ?))
            (do-init-then arg1 arg2 arg3))
        (true
            (throw-error "Invalid <with> clause in loop"))))

(defun do-code (form)
    ;;; this expansion gets the accumulator stuff out.
    (loop-macroexpand-all form)
    ;;; but add unexpanded code since the entire loop body will be expanded
    ;;; in one go thus properly obeying scoping rules.
    (.add $body form)
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

;;;
;;; level 1 accumulator handlers
;;;

(defun next-clause (form)
    (prologue)
    (cond
        ((match '(next ?))
            (let sym arg1)
            (check-symbol sym)
            (let range-var (prefix-symbol "%range" sym))
            (setf $finish-label-used true)
            `(do
                (if (not (ienumerator:move-next ,range-var)) 
                    (goto finish-label))
                (setf ,arg1 (ienumerator:current ,range-var))
                ,arg1))
        (true
            (throw-error "Invalid <next> clause in loop"))))

(defun collect-clause (form)
    (prologue)
    (cond
        ((match '(collect ?))
            (pushnew '%accumulator $collect-vars)
            `(.add %accumulator ,arg1))
        (true
            (throw-error "Invalid <collect> clause in loop"))))

(defun collect-each-clause (form)
    (prologue)
    (cond
        ((match '(collect-each ?))
            (pushnew '%accumulator $collect-vars)
            `(.add-range %accumulator ,arg1))
        (true
            (throw-error "Invalid <collect-each> clause in loop"))))

(defun collecting-clause (form)
    (prologue)
    (cond
        ((match '(collecting ? :into ?))
            (check-symbol arg2)
            (pushnew arg2 $collect-vars)
            `(.add ,arg2 ,arg1))
        ((match '(collecting ?))
            (pushnew '%accumulator $collect-vars)
            `(.add %accumulator ,arg1))
        (true
            (throw-error "Invalid <collecting> clause in loop"))))

(defun count-clause (form)
    (prologue)
    (cond
        ((match '(count ?))
            (pushnew '%accumulator $summing-vars)
            `(if ,arg1 
                (incf %accumulator)))
        (true
            (throw-error "Invalid <count> clause in loop"))))

(defun counting-clause (form)
    (prologue)
    (cond
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
            (throw-error "Invalid <summing> clause in loop"))))

(defun sum-clause (form)
    (prologue)
    (cond
        ((match '(sum ?))
            (pushnew '%accumulator $summing-vars)
            `(incf %accumulator ,arg1))
        (true
            (throw-error "Invalid <sum> clause in loop"))))

(defun summing-clause (form)
    (prologue)
    (cond
        ((match '(summing ? :into ?))
            (check-symbol arg2)
            (pushnew arg2 $summing-vars)
            `(incf ,arg2 ,arg1))
        ((match '(summing ?))
            (pushnew '%accumulator $summing-vars)
            `(incf %accumulator ,arg1))
        (true
            (throw-error "Invalid <summing> clause in loop"))))

(defun multiply-clause (form)
    (prologue)
    (cond
        ((match '(multiply ?))
            (pushnew '%accumulator $multiplying-vars)
            `(setf %accumulator (* %accumulator ,arg1)))
        (true
            (throw-error "Invalid <multiply> clause in loop"))))

(defun multiplying-clause (form)
    (prologue)
    (cond
        ((match '(multiplying ? :into ?))
            (check-symbol arg2)
            (pushnew arg2 $multiplying-vars)
            `(setf ,arg2 (* ,arg2 ,arg1)))
        ((match '(multiplying ?))
            (pushnew '%accumulator $multiplying-vars)
            `(setf %accumulator (* %accumulator ,arg1)))
        (true
            (throw-error "Invalid <multiplying> clause in loop"))))

(defun maximize-clause (form)
    (prologue)
    (cond
        ((match '(maximize ?))
            (do-maximizing '%accumulator arg1))
        (true
            (throw-error "Invalid <maximize> clause in loop"))))

(defun maximizing-clause (form)
    (prologue)
    (cond
        ((match '(maximizing ? :into ?))
            (do-maximizing arg2 arg1))
        ((match '(maximizing ?))
            (do-maximizing '%accumulator arg1))
        (true
            (throw-error "Invalid <maximizing> clause in loop"))))

(defun minimize-clause (form)
    (prologue)
    (cond
        ((match '(minimize ?))
            (do-minimizing '%accumulator arg1))
        (true
            (throw-error "Invalid <minimize> clause in loop"))))

(defun minimizing-clause (form)
    (prologue)
    (cond
        ((match '(minimizing ? :into ?))
            (do-minimizing arg2 arg1))
        ((match '(minimizing ?))
            (do-minimizing '%accumulator arg1))
        (true
            (throw-error "Invalid <minimizing> clause in loop"))))

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
    (add-final-steps))

(defun add-initial-collect-steps ()
    (each initial-collect-step $collect-vars))

(defun add-final-steps ()
    (when (or (find '%accumulator $collect-vars)
              (find '%accumulator $summing-vars)
              (find '%accumulator $multiplying-vars)
              (find '%accumulator $maximizing-vars)
              (find '%accumulator $minimizing-vars))
        (.add $final-steps `%accumulator)))

(defun add-initial-summing-steps ()
    (each initial-summing-step $summing-vars))

(defun add-initial-multiplying-steps ()
    (each initial-multiplying-step $multiplying-vars))

(defun add-initial-minimaximizing-steps ()
    (each initial-minimaximizing-step $maximizing-vars)
    (each initial-minimaximizing-step $minimizing-vars))

(defun initial-collect-step (sym)
    (.add $initial-steps `(var ,sym (vector))))

(defun initial-summing-step (sym)
    (.add $initial-steps `(var ,sym 0)))

(defun initial-multiplying-step (sym)
    (.add $initial-steps `(var ,sym 1)))

(defun initial-minimaximizing-step (sym)
    (.add $initial-steps `(var ,sym null)))


