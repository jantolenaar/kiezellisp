;;;; Copyright (C) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; prerequisites: apt install lftp
;;;;

(require "shell")

(defpackage "lftp"
    (:shadow
        "run")
    (:export
        "with-ftp-connection"
        "open"
        "put"
        "mirror"
        "run"
     ))

(in-package "lftp")

(defmacro with-ftp-connection ((handle) &body body)
    (let commands (gensym "commands"))
    `(do       
        (let ,commands 
            (with-output-to-string (,handle)
                (print-line ,handle
                    """
                    set cmd:show-status no
                    set cmd:verbose yes
                    set ftp:use-mdtm-overloaded true
                    set sftp:auto-confirm yes
                    set ssl:verify-certificate false
                    """)
                (do ,@body)))
        (let tempfile (path:get-temp-file-name))
        (file:write-all-text tempfile ,commands)
        (shell:run "lftp" "-f" tempfile)
        (file:delete tempfile)))

(defun open (conn user password host)
    (print-line conn "open -u ${user},${password} ${host}"))

(defun put (conn local-path remote-path)
    (print-line conn "put ${local-path} -o ${remote-path}"))

(defun mirror (conn local-folder remote-folder)
    (print-line conn "mirror -vvv -R -e ${local-folder} ${remote-folder}"))

(defmacro run (&body args)
    (do-run-lftp args))

(def prologue #q END
set cmd:show-status no
set cmd:verbose yes
set ftp:use-mdtm-overloaded true
set sftp:auto-confirm yes
set ssl:verify-certificate false 
END)

(defun do-run-lftp (args)
    (var user)
    (var password)
    (var host)
    (var buffer)   
    (letfun open-buffer ()
        (unless buffer
            (setf buffer (string-writer:new))
            (print buffer prologue)
            (say buffer "open -u ${user},${password} ${host}")))
    (letfun close-buffer ()
        (setf buffer null))
    (loop
        (break-if (not args))
        (let action (pop args))
        (case action
            (:user
                (setf user (pop args)))
            (:password
                (setf password (pop args)))
            (:host
                (setf host (pop args)))
            (:verbatim
                (open-buffer)
                (say buffer (pop args)))
            (:put
                (open-buffer)
                (say buffer "put" (pop args) "-o" (pop args)))
            (:mirror
                (open-buffer)
                (say buffer "mirror -vvv -R -e" (pop args) (pop args)))
            (:run
                (let tempfile (path:get-temp-file-name))
                (file:write-all-text tempfile (.to-string buffer))
                (shell:run "lftp" "-f" tempfile)
                (file:delete tempfile)
                (close-buffer))
            (:show
                (say (.to-string buffer)))
        )))