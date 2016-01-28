
;;; Copyright (C) Jan Tolenaar. See the file LICENSE for details.

;;;
;;; kiezellisp-init.k
;;;

#+(and unix graphical-mode)
(setq $stderr "/tmp/stdlog")

#+(and windows graphical-mode)
(setq $stderr "d:/temp/stdlog")

#if unix
(let project-path "/home/jan/projects/kiezellisp/")
#elif wine
(let project-path "z:/home/jan/projects/kiezellisp/")
#elif windows
(let project-path "d:/projects/kiezellisp/")
#endif

(letfun make-path (x)
    (if (.starts-with x "/")
          x
        (string project-path x)))

(apply set-load-path (map make-path (list
                "private/lib"
                "private/app"
                "contrib/lib"
                "lib"
                "app" )))

(setq $repl-force-it true)

(require "system")
(require "shell")
(require "unit-test")

(apply set-help-path (map make-path (list
                "help" 
                "/home/jan/documents/help")))

