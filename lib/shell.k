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
        "exec"
        "with-directory"
        "make-safe-argument-string"
        "pwd"
        "run-windows-command"
        "run"
        "env"
        "path"
        "get-special-folder"
        "$home"
        "$local-application-data"
        "$roaming-application-data"
        "$my-documents"
        "robocopy-mirror"
        "expand-path-name"
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

#if windows

(defun dir ()
    (run "cmd.exe" "/c" "dir")
    (void))

#elif unix

(defun dir ()
    (run "ls")
    (void))

#endif

(defun env ()
    (as-prototype-ci (environment:get-environment-variables)))

(defun path ()
    (.split (path:get-unix-name (.path (env))) (string #\semicolon)))

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

(def $home (get-special-folder :home))

(def $local-application-data
    (get-special-folder :local-application-data))

(def $roaming-application-data
    (get-special-folder :roaming-application-data))

(def $my-documents
    (get-special-folder :my-documents))

(defun run-windows-command (cmd &rest arguments)
    (let proc (process:new))
    (let info (.start-info proc))
    (setf (.file-name info) cmd)
    (setf (.redirect-standard-output info) false)
    (setf (.redirect-standard-error info) false)
    (setf (.redirect-standard-input info) false)
    (setf (.use-shell-execute info) true)
    (setf (.arguments info) (make-safe-argument-string arguments))
    (.start proc)
    (void))

(defun enclose-in-quotes (arg)
    (cond
        ((null? arg)
            "")
        ((find #\double-quote arg)
            (string "\"" arg "\""))
        ((find #\space arg)
            (string "\"" arg "\""))
        (true
            arg)))

(defun make-safe-argument-string (args)
    (string:join " " (map enclose-in-quotes args)))

(defun run (program &rest arguments)
    (exec program :stdin null :return-stdout false :args arguments))

(defun exec (program &key stdin return-stdout args)
    (let proc (process:new))
    (let info (.start-info proc))
    (setf (.file-name info) program)
    (setf (.redirect-standard-output info) true)
    (setf (.redirect-standard-error info) true)
    (setf (.redirect-standard-input info) true)
    (setf (.use-shell-execute info) false)
    (setf (.arguments info) (make-safe-argument-string args))
    (.start proc)
    (when stdin
        (.write (.standard-input proc) stdin))
    (.close (.standard-input proc))
    (let result (.read-to-end (.standard-output proc)))
    (.wait-for-exit proc)
    (let ok (zero? (.exit-code proc)))
    (if return-stdout 
        (if ok result)
          (if ok (do (print result) true))))

#if windows

(defun robocopy-mirror (source destination)
    ;(let $stdout null)
    (let source (path:get-windows-name source))
    (let destination (path:get-windows-name destination))
    (run "robocopy.exe" source destination "/MIR" "/MT" "/NS" "/NC" "/NDL" "/NP" "/NJH" "/NJS"))

#elif unix

(defun robocopy-mirror (source destination)
    ;;; TODO handle appending of slash???
    (run "rsync" "-av" (string source "/") destination))

#endif 

(defun expand-path-name (path)
    (if (.starts-with path "~")       
          (do         
            (let parts (as-list (.split (path:get-unix-name path) "/")))
            (string:join "/" (cons (replace-head (first parts)) (rest parts))))
        path))

(defun replace-head (head)
    (case head
        (("~" "~home") $home)
        ("~local-application-data" $local-application-data)
        ("~roaming-application-data" $roaming-application-data)
        ("~my-documents" $my-documents)
        (true head)))

