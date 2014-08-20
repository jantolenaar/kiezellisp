
;;; Copyright (C) Jan Tolenaar. See the file LICENSE for details.

;;;
;;; kiezellisp-init.k
;;;

(if (find :windows-mode $features)
    (setq $stdlog "d:/temp/stdlog"))

(set-load-path "d:/projects/kiezellisp-private/lib"
               "d:/projects/kiezellisp-private/app"
               "d:/projects/kiezellisp/contrib/lib"
               "d:/projects/kiezellisp/lib"
               "d:/projects/kiezellisp/app" )

(setq $load-verbose $interactive-mode)

(require "system")
(require "shell")
(require "unit-test")

