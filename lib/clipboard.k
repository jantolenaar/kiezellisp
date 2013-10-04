;;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

(reference "c:/program files (x86)/reference assemblies/microsoft/framework/.netframework/v4.0/PresentationCore")

(import "System.Windows.Clipboard" :package-name "windows-clipboard")

(defpackage "clipboard"
    (:export
        "paste"
        "copy"
    ))

(in-package "clipboard")

(defun paste ()
    "Returns the contents of the clipboard as a string"
    (windows-clipboard.get-text))

(defun copy (str)
    "Copies the given string to the clipboard"
    (windows-clipboard.set-text str))



