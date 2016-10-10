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

#if graphical-mode
(load "core/terminal")
#endif

(load "core/pprint")
(load "core/help")
