;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

(require "xml")

(defpackage "html-elements")
(in-package "html-elements")

(def tags '(html head title meta link style body script
            div span form input button textarea center
            h1 h2 h3 h4 h5 h6
            a p i b code pre img samp big small
            ul ol li
            table thead tbody tr td th
            blockquote
            area base basefont col frame isindex param
            br hr))

(def empty-tags '(area base basefont br col frame hr img input isindex link meta param))

(def long-tags '(script textarea div))

(defun make-html-function (tag)
    (let name (symbol-name tag))
    (let func-name (string "<" name ">"))
    (let func (make-symbol func-name))
    (eval `(do
                (let html-specific-type 
                    (new 'xml::xml-base-type
                        :tag ,name
                        :supports-empty-tag ,(boolean (find tag empty-tags))
                        :has-long-tag ,(boolean (find tag long-tags))))
                (defun ,func (&rest args) 
                    (let target (new html-specific-type
                                    :attributes (new)
                                    :elements (vector)))
                    (xml:add-range target args))))
    (export-symbol func-name))

(each make-html-function tags)


(defpackage "html"
    (:export
        "DOC-TYPE"
        "to-string")
    (:use 
        "html-elements"))

(in-package "html")

(def DOC-TYPE "<!DOCTYPE HTML>\n")

(defun to-string (obj &optional doc-type)
    (xml:to-string obj doc-type))

