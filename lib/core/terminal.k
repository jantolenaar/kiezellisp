;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.


(import "Kiezel.Terminal")
(import "Kiezel.Window")
(import "Kiezel.TerminalKeys")
(import "Kiezel.FrameType")
(import "Kiezel.TextStyle")

(defpackage "window"
    (:export
        "open"))

(in-package "window")

(defun open (&key (left 0) (top 0)
                  (width 0) (height 0) (buffer-height -1)
                  (parent null)
                  (border false)
                  (caption null)
                  (fore-color (terminal:default-fore-color))
                  (back-color (terminal:default-back-color))
                  (scroll-enabled missing-value)
                  (tab-completion-enabled missing-value)
                  (auto-refresh-enabled missing-value)
                  (html-enabled missing-value))
    (let win (window:create left top
                            width height buffer-height
                            fore-color back-color
                            parent border))
    (setf (.caption win) caption)
    (when (supplied? scroll-enabled)
        (setf (.scroll-enabled win) scroll-enabled))
    (when (supplied? tab-completion-enabled)
        (setf (.tab-completion-enabled win) tab-completion-enabled))
    (when (supplied? auto-refresh-enabled)
        (setf (.auto-refresh-enabled win) auto-refresh-enabled))
    (when (supplied? html-enabled)
        (setf (.html-enabled win) html-enabled))
    win)
