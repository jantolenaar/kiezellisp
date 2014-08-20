;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; xml.k
;;;;

(defpackage "xml"
    (:export
        "add"
        "add-range"
        "to-string"
        "DOC-TYPE"
        "make-xml-function"
    ))

(in-package "xml")

(def DOC-TYPE "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n")

(deftype xml-base-type 
    (new :supports-empty-tag false
         :has-long-tag false))

(defun add (target &rest args)
    (add-range target args))

(defun add-range (target args)
    (let attributes (.attributes target))
    (let elements (.elements target))
    (loop
        (let item (pop args))
        (break-if (not item))
        (cond
            ((keyword? item)
                (let value (pop args))
                (.set-value attributes item value))
            (true
                (.add elements item))))
    target)

(defun to-string (target &optional doc-type)
    (with-output-to-string ($stdout)
        (if doc-type
            (print doc-type))
        (print-any target)))

(defun print-any (obj)
    (cond
        ((type? obj 'xml-base-type)
            (print-xml obj))
        ((and (sequence? obj)
              (not (string? obj)))
            (each print-any obj))
        (true
            (print obj))))
    
(defun print-xml (obj)
    (print "<" (.tag obj))
    (each print-xml-attribute (.attributes obj))
    (print-xml-elements obj))

(defun print-xml-elements (obj)
    (cond
        ((.elements obj)
            (print ">")
            (print-any (.elements obj))
            (print "</" (.tag obj) ">"))
        ((.supports-empty-tag obj)
            (print ">"))
        ((.has-long-tag obj)
            (print "></" (.tag obj) ">"))
        (true
            (print " />"))))

(defun print-xml-attribute (attr)
    (let key (.key attr))
    (let val (.to-string (.value attr)))
    (let quote (if (find #\' val) "\"" "'"))
    (print " " key "=" quote (.html-encode val) quote))

(defun make-xml-function (tag)
    (let name (symbol-name tag))
    (let func-name (string "<" name ">"))
    (let func (make-symbol func-name))
    (eval `(do
                (let xml-specific-type 
                    (new 'xml-base-type
                        :tag ,name
                        :supports-empty-tag false
                        :has-long-tag false))
                (defun ,func (&rest args) 
                    (let target (new xml-specific-type
                                    :attributes (new)
                                    :elements (vector)))
                    (add-range target args))))
    (export-symbol func-name))

