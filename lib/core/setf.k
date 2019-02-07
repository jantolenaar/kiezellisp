;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; setf expansion
;;;;

(defpackage "lisp"
    (:export
        "get-setf-expansion"
        "define-modify-macro"
        "decf"
        "defsetf"
        "incf"
        "pop"
        "push"
        "pushnew"
        "setf"
    ))

(in-package "lisp")

(defun get-setf-expansion (place)
    (let place (macroexpand place))
    (cond
        ((list? place)
            (let head (first place))
            (let tail (rest place))
            (cond
                ((symbol? head)
                    (let update-fn (elt expanders head))
                    (if (not update-fn)
                        (throw-error "setf: no setf expander for: " (write-to-string head)))
                    (let vars (map #(gentemp "arg${%1}") (series (length tail))))
                    (let vals tail)
                    (let store-var (gentemp "new"))
                    (let reader-form (cons head vars))
                    (let writer-form `(,update-fn ,@vars ,store-var))
                    (list vars vals store-var reader-form writer-form))
                ;; next part handles (setf (.name obj) x) which
                ;; reads as (setf ((. "name") obj) x)
                ((and (list? head)
                      (= '. (first head))
                      (string? (second head)))
                    (let member (second head))
                    (let vars (list (gentemp)))
                    (let vals tail)
                    (let args (rest vals))
                    (let store-var (gentemp "new"))
                    (let reader-form `(attr ,(first vars) ,member))
                    (let writer-form `(set-attr ,(first vars) ,member ,@args ,store-var))
                    (list vars vals store-var reader-form writer-form))

                (true
                    (throw-error "setf: no setf expander for: " (write-to-string (first place)))

                )))
        ((symbol? place)
            (let vars null)
            (let vals null)
            (let store-var (gentemp "new"))
            (let reader-form place)
            (let writer-form `(setq ,reader-form ,store-var))
            (list vars vals store-var reader-form writer-form))
        (true
            (throw-error "setf: invalid place: " (write-to-string place)))))

(def expanders (new))

(defmacro setf (place value)
    (let place (macroexpand place))
    (cond
        ((symbol? place)
            `(setq ,place ,value))
        ((list? place)
            (cond
                ((symbol? (first place))
                    (if-let (update-fn (elt expanders (first place)))
                          `(,update-fn ,@(cdr place) ,value)
                        (throw-error "setf: no setf expander for: " (write-to-string (first place)))))
                ;; next part handles (setf (.name obj) x) which is (setf ((. name) obj) x)
                ((and (list? (first place))
                      (= '. (first (first place))))
                    (let target (second place))
                    (let member (second (first place)))
                    (let args (rest (rest place)))
                    `(set-attr ,target ,member ,@args ,value))
                ;; otherwise error
                (true (throw-error "setf: no setf expander for: " (write-to-string (first place))))))
        (true
            (throw-error "setf: invalid place: " (write-to-string place)))))

(defmacro defsetf (name func)
    `(set-elt expanders ',name ',func))

(defsetf car set-car)
(defsetf cdr set-cdr)
(defsetf left set-car)
(defsetf right set-cdr)
(defsetf rest set-cdr)
(defsetf first set-first)
(defsetf second set-second)
(defsetf third set-third)
(defsetf fourth set-fourth)
(defsetf fifth set-fifth)
(defsetf elt set-elt)
(defsetf attr set-attr)
(defsetf symbol-value set-symbol-value)
(defsetf find-type set-find-type)

;;;
;;; increment and decrement macros
;;;

(defmacro define-modify-macro (name args func)
    (let arg-names (system:parse-parameter-list args))
    `(defmacro ,name (place ,@args)
        (with-multiple-let (vars vals store-var reader-form writer-form)
                (get-setf-expansion place)
	        `(do
	            ,@(map #`(let ,%1 ,%2) vars vals)
	            (let ,store-var (,,func ,reader-form ,,@arg-names))
	            ,writer-form))))

(define-modify-macro incf (&optional (delta 1)) '+)
(define-modify-macro decf (&optional (delta 1)) '-)

;;;
;;; push and pop macros (not optimized)
;;;

(defmacro pop (place)
    (with-multiple-let (vars vals store-var reader-form writer-form)
                        	(get-setf-expansion place)
	    (let temp (gentemp "list"))
	    `(do
	        ,@(map #`(let ,%1 ,%2) vars vals)
	        (let ,temp ,reader-form)
	        (let ,store-var (cdr ,temp))
	        ,writer-form
	        (car ,temp))))

(defmacro push (item place)
    (with-multiple-let (vars vals store-var reader-form writer-form)
                      	  (get-setf-expansion place)
	    `(do
	        ,@(map #`(let ,%1 ,%2) vars vals)
	        (let ,store-var (cons ,item ,reader-form))
	        ,writer-form)))

(defmacro pushnew (item place)
    (with-multiple-let (vars vals store-var reader-form writer-form)
                      	  (get-setf-expansion place)
	    `(do
	        ,@(map #`(let ,%1 ,%2) vars vals)
	        (var ,store-var (adjoin ,item ,reader-form))
	        ,writer-form)))
