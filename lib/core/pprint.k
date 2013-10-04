;;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

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

(def $pprint-left-margin 0)
(def $pprint-right-margin 80)
(def $pprint-indent-size 4)


(def pprint-form-settings
    ;; The number is the number of expressions to keep on the same line as the keyword.
    '(
      (do 0)
      (break 1)
      (return 1)
      (if 1)
      (if-match 2)
      (and 1)
      (or 1)
      (when 1)
      (when-match 2)
      (while 1)
      (loop 0)
      (defun 2)
      (defmethod 2)
      (defmacro 2)
      (defmulti 2)
      (lambda 1)
      (λ 1)
      (cond 0)
      (case 1)
      (case-match 1)
      (typecase 1)
      (setq 2)
      (setf 2)
      (set 2)
      (def 2 :pprint)
      (var 2)
      ;(quote 1)
      (tagbody 0)
      (throw 1)
      (try-and-catch 0)
      (try-finally 0)
      (new 1 :align)
     ))

(defun pprint (expr &key (left $pprint-left-margin)
                         (right $pprint-right-margin))
"""
The pretty printer must write to $stdout and obey the settings of
$print-escape and $print-base.
"""
    (var $print-compact true)
    (var $pprint-left-margin (or left 0))
    (var $pprint-right-margin (or right 80))
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
        ((vector? expr)
            (pprint-sequence expr "[" "]"))
        ((prototype? expr)
            (pprint-prototype expr))
        (true
            (pprint-object expr))))

(defun pprint-cons (expr)

    (var a (first expr))
    (var b (second expr))

    (when (eq a 'quote)
        (print "'")
        (var $pprint-left-margin (inc $pprint-left-margin))
        (pprint-expr b)
        (return))

    (when (and (eq a '.)
               (string? b))
            (print "." b)
            (return))

    (var sym (and (symbol? a)
                  a))

    (var config (rest (find sym pprint-form-settings :key first)))

    (cond
        ((not config)
            (var func-like (and sym
                                (defined? sym)
                                (function? (symbol-value sym))))
            (if func-like
                  (pprint-code expr '(1 :align))
                (pprint-sequence expr "(" ")")
            ))
        (true
            (pprint-code expr config))))

(defun pprint-sequence (expr leftpar rightpar)
    (var text (write-to-string expr))
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
    (var $pprint-left-margin (+ $pprint-left-margin (.length leftpar)))
    (pprint-items-vertically seq false)
    (print rightpar))

(defun pprint-code (expr config)
    (var text (write-to-string expr))
    (cond
        ((<= (+ $pprint-left-margin (length text)) $pprint-right-margin)
            (write text :escape false))
        (true
            (pprint-formatted-code expr config))))

(defun pprint-formatted-code (expr config)
    (var num-items (first config))
    (print "(")
    (var head (write-to-string (first expr)))
    (var tail (rest expr))
    (write head :escape false)
    (var $pprint-left-margin (if (find :align (rest config))
                    (+ $pprint-left-margin 1 (.length head) 1)
                  (+ $pprint-left-margin $pprint-indent-size)))
    (var left $pprint-left-margin)
    (loop
        (for i :in (series num-items))
        (print " ")
        (if (and (= i num-items)
                 (find :pprint (rest config)))
            (pprint (first tail) :left (inc left))
          (do
            (var text (write-to-string (first tail)))
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
        (var head (string ":" key " "))
        (print head)
        (var $pprint-left-margin (+ $pprint-left-margin (.length head)))
        (pprint-expr (attr seq key))))

(defun pprint-left-margin ()
    (write "" :width $pprint-left-margin :escape false))


