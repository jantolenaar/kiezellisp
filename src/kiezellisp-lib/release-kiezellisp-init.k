
;;; Copyright (C) Jan Tolenaar. See the file LICENSE for details.

;;;
;;; kiezellisp-init.k
;;;

(set-load-path "../lib" "../app")

(require "system")
(require "shell")

#if repl
(set-help-path "../help")
#endif
