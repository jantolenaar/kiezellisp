
;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

;;;
;;; kiezellisp-init.k
;;;

(import "System.ConsoleColor")
(setq $standout-color console-color.DARK-MAGENTA)
(setq $standout-background-color console-color.WHITE)

(set-load-path "d:/projects/kiezellisp-private/lib"
               "d:/projects/kiezellisp-private/app"
               "d:/projects/kiezellisp/contrib/lib"
               "d:/projects/kiezellisp/lib"
               "d:/projects/kiezellisp/app" )

(setq $load-verbose $interactive-mode)

(require "system")
(require "shell")


