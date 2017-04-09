;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; curses.k
;;;;
;;;;
;;;; External projects:
;;;;
;;;;     git clone https://github.com/sushihangover/CursesSharp
;;;;

(reference "CursesSharp")

(import "CursesSharp.Curses")
(import "CursesSharp.Pad")
(import "CursesSharp.Panel")
(import "CursesSharp.SoftLabels")
(import "CursesSharp.Stdscr")
(import "CursesSharp.Window")
(import "CursesSharp.Attrs")
(import "CursesSharp.Acs")
(import "CursesSharp.Colors")
(import "CursesSharp.Keys")
(import "CursesSharp.MouseState")

#ignore
(defun curses-demo1 ()
	(curses:init-scr)
	(stdscr:add "Hello World !!!")
	(stdscr:refresh)
	(stdscr:get-char)
	(curses:beep)
	(curses:end-win))

(reference "CursesSharp.Gui")

(import "CursesSharp.Gui.Container")
(import "CursesSharp.Gui.Label")
(import "CursesSharp.Gui.TrimLabel")
(import "CursesSharp.Gui.Entry")
(import "CursesSharp.Gui.Button")
(import "CursesSharp.Gui.CheckBox")
(import "CursesSharp.Gui.ListView")
(import "CursesSharp.Gui.Frame")
(import "CursesSharp.Gui.Dialog")
(import "CursesSharp.Gui.MenuBar")
(import "CursesSharp.Gui.MenuBarItem")
(import "CursesSharp.Gui.MenuItem")
(import "CursesSharp.Gui.ButtonBar")
(import "CursesSharp.Gui.Debug")
(import "CursesSharp.Gui.ErrorDialog")
(import "CursesSharp.Gui.FileViewWidget")
(import "CursesSharp.Gui.MainScreen")
(import "CursesSharp.Gui.MessageBox")
(import "CursesSharp.Gui.Terminal")

#ignore
(defun curses-demo2 ()
	(terminal:init false)
	(let demo (container:new 0 0 (terminal:cols) (terminal:lines)))
	(.add demo (label:new 10 10 "Hello"))
	(.add demo (entry:new 16 10 20 "World"))
	(.add demo (label:new 10 20 "Ctrl-C or ESC to exit"))
	(terminal:run demo)
	(curses:end-win))


