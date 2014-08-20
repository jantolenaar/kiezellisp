
;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

(import "Microsoft.Win32.Registry")

(defun set-registry-value (key value-name value)
    (registry:set-value key value-name value))

(defun set-user-env (variable value)
    (set-registry-value @"HKEY_CURRENT_USER\Environment" variable value))

(defun set-system-env (variable value)
    (set-registry-value @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" variable value))

(defun make-path (sequence)
    (string:join ";" sequence))

(defun set-user-path (&rest args)
    (set-user-env "PATH" (make-path args)))

(defun set-system-path (&rest args)
    (set-system-env "PATH" (make-path args)))

(defun main ()
    (set-user-path)
    (set-system-path
        @"c:\bin"
        @"c:\bin\sysinternals"
        @"c:\Program Files (x86)\pandoc\bin"
        @"c:\Program Files (x86)\git\bin"
        @"c:\windows\system32"
        @"c:\windows"
        @"c:\windows\System32\Wbem"
        @"c:\windows\System32\WindowsPowerShell\v1.0\"
        @"c:\cygwin\bin"
        @"c:\windows\microsoft.net\framework\v4.0.30319"
    ))
