
(reference "System.Windows.Forms" )

(defpackage "win"
    (:export
        "add-menu-list"
        "add-menu-items"
        "make-main-form"
        "run"
    )
    (:shadow
        "run"
    ))

(in-package "win")

(import "System.ComponentModel.Container" )
(import "System.Windows.Forms.Application" )
(import "System.Windows.Forms.Form" )
(import "System.Windows.Forms.Menu" )
(import "System.Windows.Forms.MainMenu" )
(import "System.Windows.Forms.MenuItem" )
(import "System.Windows.Forms.MessageBox" )


(defmethod add-menu-items ((this menu) menus)
    (foreach (menu menus)
        (add-menu-item this menu)))


(defmethod add-menu-item ((this menu) menu)
    (var head (menu-item.new (first menu)))
    (.add (.menu-items this) head)
    (var tail (second menu))
    (when tail
        (cond
            ((list? tail)
                (add-menu-items head tail))
            (true
                (add-event-handler menu-item.EVENT-CLICK head tail)))))

(deftype main-form-base (new))

(deftype main-form (new 'main-form-base
    :components null
    :form null))

(defun make-main-form ()
    (var this (new 'main-form))
    (var form (form.new))
    (setf (.form this) form)
    (setf (.components this) (container.new))
    (setf (.menu form) (main-menu.new (.components this)))
    this)

(defmethod add-menu-items ((this main-form) menus)
    (var main-menu (.menu (.form this)))
    (add-menu-items main-menu menus))

(defmethod run ((this main-form))
    (application.run (.form this))
    (environment.exit 0))

;;;
;;; application code below this line
;;;

(in-package "user")

(defun main ()
    (var x (make-form))
    (win.run x))

(defun make-form ()
    (var this (win.make-main-form))
    (var menus '(("File" (("Open..." on-open)
                          ("Close" on-close)
                          ("Exit" on-exit)))
                 ("Edit" (("Copy")
                          ("Cut")
                          ("Paste")))))
    (win.add-menu-items this menus)
    this)

(defun on-exit (object event)
    (message-box.show "exit clicked")
    (application.exit))

(defun on-open (object event)
    (message-box.show "open clicked"))

(defun on-close (object event)
    (message-box.show "close clicked"))

