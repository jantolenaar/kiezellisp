;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; print-formatter.k
;;;;

(defpackage "print"
    (:export
    	"make-field"
        "print-records"))

(in-package "print")

(defun make-field-from-list (field)
    (var name null)
    (var title null)
    (var width null)
    (let f1 (first field))
    (let f2 (second field))
    (let f3 (third field))
    (cond
        ((symbol? f1)
            (setf name f1))
        ((string? f1)
            (setf title f1))
        ((number? f1)
            (setf width f1)))
    (cond
        ((symbol? f2)
            (setf name f2))
        ((string? f2)
            (setf title f2))
        ((number? f2)
            (setf width f2)))
    (cond
        ((symbol? f3)
            (setf name f3))
        ((string? f3)
            (setf title f3))
        ((number? f3)
            (setf width f3)))
    (when (not title)
        (setf title (symbol-name name)))
    (when (not width)
        (setf width 10))
    (when (not name)
        (setf name (make-symbol title)))
    (new :name name :title title :width width))

(defun make-field (field)
    (make-field-from-list (if (list? field)
                                field
                              (list field))))

(defun print-records (records fields)
    (let $print-escape false)
    (let fields (map make-field fields))
    (var counter 0)
    (print-headers fields)
    (print-separators fields)
    (foreach (v records)
        (cond
            ((= v null)
                (write-line))
            ((string? v)
                (print-separators fields))
            (true
                (setf counter (inc counter))
                (foreach (f fields)
                    (write (attr v (.name f)) :width (.width f))
                    (write " "))
                    (write "\n"))))
    (write "\n"))

(defun print-headers (fields)
    (write "\n")
    (foreach (f fields)
        (write (.title f) :width (.width f))
        (write " "))
    (write "\n"))

(defun print-separators (fields)
    (foreach (f fields)
        (write (string:repeat "=" (math:abs (.width f))))
        (write " "))
    (write "\n"))


