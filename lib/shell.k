;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; shell.k
;;;;

(import "System.Diagnostics.Process")
(import "System.Environment")
(import "System.Environment+SpecialFolder")

(defpackage "shell"
    (:export
        "$sudo"
        "$home"
        "$local-application-data"
        "$my-documents"
        "$roaming-application-data"
        "cd"
        "dir"
        "env"
        "expand-path-name"
        "get-special-folder"
        "here"
        "make-safe-argument-string"
        "path"
        "pwd"
        "robocopy-mirror"
        "run"
        "sudo"
        "with-directory"
        ; already defined internally
        "exec"
        "exec-with-detach"
        "exec-with-io-redirect"
    )
    (:shadow
        "path"
        "run"
    ))


(in-package "shell")

(def $sudo false)

(defun run (&rest args)
    (if $sudo
        (apply exec "sudo" args)
        (apply exec args)))

(defun pwd ()
    (say (here)))

(defun here ()
    (path:get-unix-name (environment:current-directory)))

(defun cd (path)
    (environment:set-current-directory path))

(defmacro with-directory ((path) &body forms)
    `(do
        (let old-path (here))
        (cd ,path)
        (try 
            ,@forms
            (finally (cd old-path)))))

#if windows

(defun dir ()
    (run "cmd.exe" "/c" "dir"))

#elif unix

(defun dir ()
    (run "ls"))

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

#if windows

(defun robocopy-mirror (source destination)
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

(defmacro sudo (&rest forms)
    `(do
        (let $sudo true)
        ,@forms))

