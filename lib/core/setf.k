;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.


;;;;
;;;; setf expansion
;;;;

(defpackage "lisp"
    (:export
        "decf"
        "defsetf"
        "incf"
        "pop"
        "push"
        "pushnew"
        "setf"
    ))

(in-package "lisp")

(def expanders (new))

(defmacro setf (place value)
    (cond
        ((symbol? place)
            `(setq ,place ,value))
        ((list? place)
            (cond
                ;; standard expansion handles macros and defsetf registered functions.
                ((symbol? (first place))
                    (multiple-let (new-place flag) (macroexpand-1 place))
                    (if flag
                          `(setf ,new-place ,value)
                        (if-let (update-fn (elt expanders (first place)))
                              `(,update-fn ,@(cdr place) ,value)
                            (throw (lisp-exception:new "Undefined handler for: {0}" (first place))))))
                ;; next part handles (setf (.name obj) x) which is (setf ((. name) obj) x)
                ((and (list? (first place))
                      (= '. (first (first place))))
                    `(set-attr ,(second place) ,(second (first place)) ,value))
                ;; otherwise error
                (true (throw (lisp-exception:new "Undefined handler for: {0}" (first place))))))))

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

(defmacro incf (place &optional delta)
    (if (= delta null)
          `(setf ,place (inc ,place))
        `(setf ,place (+ ,place ,delta))))


(defmacro decf (place &optional delta)
    (if (= delta null)
          `(setf ,place (dec ,place))
        `(setf ,place (- ,place ,delta))))

;;;
;;; push and pop macros (not optimized)
;;;

(defmacro pop (place)
    (let temp (gentemp))
    `(do
        (let ,temp (car ,place))
        (setf ,place (cdr ,place))
        ,temp))

(defmacro push (item place)
    `(setf ,place (cons ,item ,place)))

(defmacro pushnew (item place)
    (let temp-item (gentemp))
    (let temp-place (gentemp))
    `(do
        (let ,temp-item ,item)
        (let ,temp-place ,place)
        (if (find ,temp-item ,temp-place)
              ,temp-place
            (setf ,place (cons ,temp-item ,temp-place)))))
        