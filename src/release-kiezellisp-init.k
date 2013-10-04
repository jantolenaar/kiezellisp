
;;; Copyright (C) 2013 Jan Tolenaar. See the file LICENSE for details.

;;;
;;; kiezellisp-init.k
;;;

;(import "System.ConsoleColor")
;(setq $standout-color console-color.YELLOW)
;(setq $standout-background-color console-color.BLUE)

(set-load-path "../lib" "../app")

(setq $load-verbose $interactive-mode)

(require "system")
(require "shell")


