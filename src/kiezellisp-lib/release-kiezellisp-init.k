
;;; Copyright (C) Jan Tolenaar. See the file LICENSE for details.

;;;
;;; kiezellisp-init.k
;;;

#if unix
#if flatpak
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
(require "shell")

#if repl
(set-help-path "../help")
#endif
