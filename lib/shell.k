;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; shell.k
;;;;

(import "System.Diagnostics.Process")
(import "System.Environment")
(import "System.Environment+SpecialFolder")

(defpackage "shell"
    (:export
        "here"
        "cd"
        "dir"
        "with-directory"
        "make-safe-argument-string"
        "pwd"
        "run-windows-command"
        "run"
        "env"
        "path"
        "get-special-folder"
        "home"
        "windows"
        "system"
        "local-application-data"
        "roaming-application-data"
        "my-documents"
        "robocopy-mirror"
    )
    (:shadow
        "run"
        "path"))


(in-package "shell")

(defun pwd ()
    (print-line (here))
    (void))

(defun here ()
    (path:get-unix-name (environment:current-directory)))

(defun cd (path)
    (environment:set-current-directory path)
    (void))

(defmacro with-directory ((path) &body forms)
    `(do
        (let old-path (here))
        (cd ,path)
        (try 
            ,@forms
            (finally (cd old-path)))))

#+windows-nt
(defun dir ()
    (run "cmd.exe" "/c" "dir")
    (void))

#+unix
(defun dir ()
    (run "ls")
    (void))

(defun env ()
    (as-prototype-ignore-case (environment:get-environment-variables)))

(defun path ()
    (.split (path:get-unix-name (.path (env))) (string #\semicolon)))

(defun home ()
    (get-special-folder :home))

(defun local-application-data ()
    (get-special-folder :local-application-data))

(defun roaming-application-data ()
    (get-special-folder :roaming-application-data))

(defun windows ()
    (get-special-folder :windows))

(defun system ()
    (get-special-folder :system))

(defun my-documents ()
    (get-special-folder :my-documents))

(defun get-special-folder (special-folder)
    (path:get-unix-name (environment:get-folder-path
        (case special-folder
            (:home
                special-folder:USER-PROFILE)
            (:local-application-data
                special-folder:LOCAL-APPLICATION-DATA)
            (:roaming-application-data
                special-folder:APPLICATION-DATA)
            (:windows
                special-folder:WINDOWS)
            (:system
                special-folder:SYSTEM)
            (:my-documents
                special-folder:MY-DOCUMENTS)
            (true
                special-folder)))))

(defun run-windows-command (cmd)
    (let proc (process:new))
    (let info (.start-info proc))
    (setf (.file-name info) cmd)
    (setf (.redirect-standard-output info) false)
    (setf (.redirect-standard-error info) false)
    (setf (.redirect-standard-input info) false)
    (setf (.use-shell-execute info) true)
    (.start proc)
    (void))

(defun enclose-in-quotes (arg)
    (cond
        ((find #\double-quote arg)
            (string "\"" arg "\""))
        ((find #\space arg)
            (string "\"" arg "\""))
        (true
            arg)))

(defun make-safe-argument-string (args)
    (string:join " " (map enclose-in-quotes args)))

(defun run (program &rest arguments)
    (let proc (process:new))
    (let info (.start-info proc))
    (setf (.file-name info) program)
    (setf (.redirect-standard-output info) true)
    (setf (.redirect-standard-error info) true)
    (setf (.redirect-standard-input info) true)
    (setf (.use-shell-execute info) false)
    (setf (.arguments info) (make-safe-argument-string arguments))
    (.start proc)
    (.close (.standard-input proc))
    (let result (.read-to-end (.standard-output proc)))
    (.wait-for-exit proc)
    (print result)
    (= 0 (.exit-code proc)))

(defun run-with-input (cmd &key stdin)
    (let proc (process:new))
    (let info (.start-info proc))
    (setf (.file-name info) "cmd.exe")
    (setf (.redirect-standard-output info) true)
    (setf (.redirect-standard-error info) true)
    (setf (.redirect-standard-input info) true)
    (setf (.use-shell-execute info) false)
    (setf (.arguments info) (string "/c \"" cmd "\""))
    (.start proc)
    (when stdin
        (.write (.standard-input proc) stdin))
    (.close (.standard-input proc))
    (let result (.read-to-end (.standard-output proc)))
    (.wait-for-exit proc)
    (print result)
    (= 0 (.exit-code proc)))


(defun robocopy-mirror (source destination)
    ;(let $stdout null)
    (run "robocopy.exe" source destination "/MIR" "/MT" "/NS" "/NC" "/NDL" "/NP" "/NJH" "/NJS"))
    
(defun back-tick-readhandler (stream char count)
    (declare (ignore count))
    (let seq (map string (.read-delimited-list stream (string char))))
    `(with-output-to-string ($stdout)
        (shell:run ,@seq)))

(set-dispatch-macro-character #\# #\` back-tick-readhandler)

