
;;; Copyright (C) Jan Tolenaar. See the file LICENSE for details.

;;;
;;; kiezellisp-init.k
;;;

(set-load-path "../lib" "../app")

(setq $load-verbose $interactive-mode)

(require "system")
(require "shell")

#+windows
(set-online-help null "http://www.kiezellisp.nl")

#+unix
(set-online-help "firefox" "http://www.kiezellisp.nl")
 
