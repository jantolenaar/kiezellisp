
;;; Copyright (C) Jan Tolenaar. See the file LICENSE for details.

;;;
;;; kiezellisp-init.k
;;;

#if unix
#if bsd
(set-assembly-path "/usr/local/lib/mono/4.5")
#elif flatpack
(set-assembly-path "/app/lib/mono/4.5")
#else
(set-assembly-path "/usr/lib/mono/4.5")
#endif
#endif

#if windows
(set-assembly-path "c:/windows/microsoft.net/framework64/v4.0.30319")
#endif

(set-load-path "../lib" "../app")

(require "system")

#if repl

(require "shell")
(require "diagnostics")

(set-help-path "../help")

(setq $repl-force-it true)
(set-debug-level 2)

#endif

