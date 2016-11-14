;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; textwindow.k
;;;;

(import "Kiezel.TextWindow")
(import "Kiezel.FrameType")
(import "Kiezel.TextStyle")
(import "System.Windows.Forms.Keys")

(defpackage "lisp"
    (:export
        "with-window-suspended"
        "show-log-window"
        "hide-log-window"
        ))

(in-package "lisp")

(defmacro with-window-suspended ((win) &body forms)
    (let temp (gentemp))
    `(do
        (let ,temp ,win)
        (try
            (setf (.output-suspended ,temp) true)
            ,@forms
            (finally
                (setf (.output-suspended ,temp) false)))))

(def stderr-saved null)
(def stdlog-saved null)
(def log-window null)

(defun hide-log-window ()
    (when log-window
        (setf $stderr stderr-saved)
        (setf $stdlog stdlog-saved)
        (.close log-window)
        (setf log-window null)))

(defun show-log-window ()
    (when (not log-window)
        (setf log-window (text-window:open :caption "Kiezellisp Log" :top 3 :left 5))
        (setf stderr-saved $stderr)
        (setf stdlog-saved $stdlog)
        (setf $stderr log-window)
        (setf $stdlog log-window)
        (.bring-to-front $stdscr)))
