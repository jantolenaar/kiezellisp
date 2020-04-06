;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; common-mark.k
;;;;
;;;;
;;;; External projects:
;;;;
;;;;     git clone https://github.com/Knagis/CommonMark.NET
;;;;


(package "common-mark")

(public "convert-to-html")

(reference "CommonMark.dll")

(import "CommonMark.CommonMarkConverter")

(defun convert-to-html (str)
    (common-mark-converter:convert str null))
