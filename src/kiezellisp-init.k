
;;; Copyright (C) Jan Tolenaar. See the file LICENSE for details.

;;;
;;; kiezellisp-init.k
;;;

#+(and unix graphical-mode)
(setq $stdlog "/tmp/stdlog")

#+(and windows graphical-mode)
(setq $stdlog "d:/temp/stdlog")

#+unix
(let project-path "/home/jan/projects/kiezellisp/")

#+windows
(let project-path "d:/projects/kiezellisp/")

(letfun make-path (x)
    (string project-path x))

(apply set-load-path (map make-path (list
                "private/lib"
                "private/app"
                "contrib/lib"
                "lib"
                "app" )))

(setq $load-verbose $interactive-mode)

(require "system")
(require "shell")
(require "unit-test")

