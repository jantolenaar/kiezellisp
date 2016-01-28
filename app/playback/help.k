(help)
;; The 'help' command starts the help system.
;; Press ENTER to start.
<enter>
;; We can scroll with page/ctrl+arrow-up/down.
;; Press PAGE-DOWN to go down... 
<page-down>
;; ...then press PAGE-UP to go up.
<page-up>
;; Press ESC to leave the help system.
<escape>
(help 'car)
;; Now we start 'help' with the symbol 'car' as an argument.
;; Press ENTER to start. 
<enter>
;; After viewing we want to know about 'caar' too.
;; Type a slash and then 'caar'.
/caar
;; Press ENTER to start.
<enter>
;; Now we have two help screens open.
;; Use left and right arrows to move between them.
;; Press LEFT to go left...
<left>
;; ...then press RIGHT to go right.
<right>
;; Press ESC to leave the help system.
<escape>
(map reverse (reductions
;; We can also start help by pressing F1.
<home><right><right><right><right><right><right><right>
;; First move to the target word, then press F1.
<f1>
;; Press ESC to leave the help system.
<escape>
<escape>



