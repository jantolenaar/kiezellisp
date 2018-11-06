
;;; Copyright (C) Jan Tolenaar. See the file LICENSE for details.

;;;
;;; kiezellisp-init-sample.k
;;;

#if unix
#if bsd
(set-assembly-path "/home/jan/bin"
                   "/usr/local/lib/mono/4.5")
#elif flatpak
(set-assembly-path "/home/jan/bin"
                   "/app/lib/mono/4.5")
#else
(set-assembly-path "/home/jan/bin"
                   "/usr/lib/mono/4.5")
#endif
#endif

#if windows
(set-assembly-path "c:/windows/microsoft.net/framework64/v4.0.30319")
#endif

#if (and unix (not repl))
(setq $stderr (open-log "/home/jan/temp/stdlog"))
(setq $stdlog (open-log "/home/jan/temp/stdlog"))
#endif

#if (and windows (not repl))
(setq $stderr (open-log "d:/temp/stdlog"))
(setq $stdlog (open-log "d:/temp/stdlog"))
#endif


#if unix
(def project-path "/home/jan/projects/")
#elif wine
(def project-path "z:/home/jan/projects/")
#elif windows
(def project-path "f:/")
#endif

(defun make-path (x)
    (if (.starts-with x "/")
          x
        (string project-path x)))

(apply set-load-path (map make-path (list
                "kiezellisp-private/lib"
                "kiezellisp-private/app"
                "kiezellisp/lib"
                "kiezellisp/app" )))

(require "system")

#if repl

(require "shell")
(require "unit-test")
(require "diagnostics")
(setq $repl-force-it true)
(set-debug-level 2)

#endif
