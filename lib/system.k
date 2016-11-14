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
(load "core/macros2")
(load "core/struct")
(load "core/match")
(load "core/threading")

#if kiezellisp-repl
(load "core/text-window")
#endif

(load "core/pprint")

#if repl
(load "core/help")
#endif
