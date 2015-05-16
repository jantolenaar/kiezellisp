;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; system.k
;;;;

(in-package "lisp")

(load "core/clr")
(load "core/package")
(load "core/functions")
(load "core/macros")
(load "core/setf")
(load "core/loop")
(load "core/struct")
(load "core/match")
(load "core/threading")
(load "core/pprint")
(load "core/help")

(when $interactive-mode
    (set-console-key-binding 'f9 '() (λx x "("))
    (set-console-key-binding 'f10 '() (λx x ")"))
    (set-console-key-binding 'f12 '() (λx x "λ"))
    )



