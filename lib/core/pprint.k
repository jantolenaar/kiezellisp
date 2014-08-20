;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; pprint.k
;;;;

(defpackage "lisp"
    (:export
        "$pprint-left-margin"
        "$pprint-right-margin"
        "pprint"        
    ))

(in-package "lisp")

(defconstant default-right-margin 80)
(defconstant default-indent-size 4)

(def $pprint-left-margin 0)
(def $pprint-right-margin default-right-margin)
(def $pprint-indent-size default-indent-size)


(def pprint-form-settings
    ;; The number is the number of expressions to keep on the same line as the keyword.
    '(
      (and 1)
      (block 1)
      (break 1)
      (break-if 1)
      (case 1)
      (case-match 1)
      (catch 1)
      (cond 0)
      (continue 0)
      (continue-if 0)
      (def 2 :pprint)
      (defconstant 2 :pprint)
      (defmacro 2)
      (defmethod 2)
      (defmulti 2)
      (defun 2)
      (do 0)
      (ecase 1)
      (ecase-match 1)
      (etypecase 1)
      (future 2)
      (if 1)
      (if-match 2)
      (ignore-errors 0)
      (lambda 1)
      (lazy 2)
      (let 2)
      (letfun 2)
      (loop 0)
      (new 1 :align)
      (or 1)
      (quote 1)
      (return 1)
      (return-from 2)
      (return-if 1)
      (return-or-throw 1)
      (set 2)
      (setf 2)
      (setq 2)
      (throw 1)
      (try 0)
      (try-and-catch 0)
      (typecase 1)
      (let 2)
      (when 1)
      (when-match 2)
      (while 1)
      (λ 1)
     ))

(defun pprint (expr &key (left $pprint-left-margin) (right $pprint-right-margin))
"""
The pretty printer must write to $stdout and obey the settings of
$print-escape and $print-base.
"""
    (let $print-compact true)
    (let $pprint-left-margin (or left 0))
    (let $pprint-right-margin (or right default-right-margin))
    (pprint-expr expr))

;;;
;;; This wires Kiezellisp's internal WRITE to PPRINT.
;;;

(setq $pprint-hook pprint)

;;;
;;; Pretty printing implementation.
;;;

(defun pprint-expr (expr)
    (cond
        ((cons? expr)
            (pprint-cons expr))
        ((ilist? expr)
            (pprint-sequence expr "[" "]"))
        ((prototype? expr)
            (pprint-prototype expr))
        (true
            (pprint-object expr))))

(defun pprint-cons (expr)

    (let a (first expr))
    (let b (second expr))
    (let c (third expr))

    (when (eq a 'quote)
        (print "'")
        (let $pprint-left-margin (inc $pprint-left-margin))
        (pprint-expr b)
        (return))

    (when (and (eq a '.)
               (string? b)
               (null? c))
            (print "." b)
            (return))

    (when (and (eq a '?)
               (string? b)
               (null? c))
            (print "?" b)
            (return))

    (let sym (and (symbol? a)
                  a))

    (let config (rest (find sym pprint-form-settings :key first)))

    (cond
        ((not config)
            (let func-like (and sym
                                (defined? sym)
                                (function? (symbol-value sym))))
            (if func-like
                  (pprint-code expr '(1 :align))
                (pprint-sequence expr "(" ")")
            ))
        (true
            (pprint-formatted-code expr config))))

(defun pprint-sequence (expr leftpar rightpar)
    (let text (write-to-string expr))
    (cond
        ((<= (+ $pprint-left-margin (length text)) $pprint-right-margin)
            (write text :escape false))
        (true
            (pprint-formatted-list expr leftpar rightpar))))

(defun pprint-object (expr)
    (write expr :force false))

(defun pprint-prototype (expr)
    (pprint-formatted-list expr "{ " " }"))

(defun pprint-formatted-list (seq leftpar rightpar)
    (print leftpar)
    (let $pprint-left-margin (+ $pprint-left-margin (.length leftpar)))
    (pprint-items-vertically seq false)
    (print rightpar))

(defun pprint-code (expr config)
    (let text (write-to-string expr))
    (cond
        ((<= (+ $pprint-left-margin (length text)) $pprint-right-margin)
            (write text :escape false))
        (true
            (pprint-formatted-code expr config))))

(defun pprint-formatted-code (expr config)
    (let num-items (first config))
    (print "(")
    (let head (write-to-string (first expr)))
    (var tail (rest expr))
    (write head :escape false)
    (let $pprint-left-margin 
            (if (find :align (rest config))
                  (+ $pprint-left-margin 1 (.length head) 1)
                (+ $pprint-left-margin $pprint-indent-size)))
    (var left $pprint-left-margin)
    (loop
        (for i :in (series num-items))
        (print " ")
        (if (= i num-items)
            (pprint (first tail) :left (inc left))
          (do
            (let text (write-to-string (first tail)))
            (write text :escape false)
            (setf left (+ left 1 (.length text)))))
        (setf tail (rest tail)))
    (pprint-items-vertically tail true)
    (print ")"))


(defun pprint-items-vertically (seq newline)
    (cond
        ((cons? seq)
            (pprint-cons-items-vertically seq newline))
        ((prototype? seq)
            (pprint-prototype-items-vertically seq newline))
        (true
            (pprint-vector-items-vertically seq newline))))

(defun pprint-cons-items-vertically (seq newline)
    (loop
        (break-if (= null seq))
        (when newline
            (print-line)
            (pprint-left-margin))
        (setf newline true)
        (pprint-expr (car seq))
        (break-if (not (forced? seq)))
        (setf seq (cdr seq)))
    (when seq
        (print-line)
        (pprint-left-margin)
        (print "...")))

(defun pprint-vector-items-vertically (seq newline)
    (loop
        (for item in seq)
        (when newline
            (print-line)
            (pprint-left-margin))
        (setf newline true)
        (pprint-expr item)))

(defun pprint-prototype-items-vertically (seq newline)
    (loop
        (for key in (.keys seq))
        (when newline
            (print-line)
            (pprint-left-margin))
        (setf newline true)
        (let head (string (if (.starts-with key "[") "" ":") key " "))
        (print head)
        (let $pprint-left-margin (+ $pprint-left-margin (.length head)))
        (pprint-expr (attr seq key))))

(defun pprint-left-margin ()
    (write "" :width $pprint-left-margin :escape false))


