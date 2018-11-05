" Vim syntax file
" Language:    Kiezel
" Maintainer:  Jan Tolenaar
" Last Change: July 31, 2018
" Version:     7
" URL:	       http://www.kiezellisp.nl
"

" ---------------------------------------------------------------------
"  Load Once:
" For vim-version 5.x: Clear all syntax items
" For vim-version 6.x: Quit when a syntax file was already loaded
if version < 600
  syntax clear
elseif exists("b:current_syntax")
  finish
endif

setlocal iskeyword=33,36-38,42-43,45-58,60-90,92,94-95,97-122,124,126
set ignorecase

syn match lispCall      "(" nextgroup=lispForm,lispdebug

syn keyword lispForm contained var let letfun letmacro let-symbol-macro lazy future
syn keyword lispForm contained undef defmacro defclass defstruct
syn keyword lispForm contained def defonce defconstant defmethod deftype define-compiler-macro
syn keyword lispForm contained defun defun* defmulti defpackage lambda lambda* define-modify-macro
syn keyword lispForm contained declare define-symbol-macro
syn keyword lispForm contained return break continue finish
syn keyword lispForm contained return-if continue-if break-if finish-if if-let when-let
syn keyword lispForm contained if case typecase ecase etypecase cond when unless if-match when-match case-match
syn keyword lispForm contained ecase-match block redo leave lambda new quote
syn keyword lispForm contained ignore-errors try-and-catch try catch finally throw return-or-throw using
syn keyword lispForm contained with-scope scope-exit-pass scope-exit-fail scope-exit
syn keyword lispForm contained loop while foreach do merging-do
syn keyword lispform contained prog reprog self with
syn keyword lispForm contained and or
syn keyword lispForm contained parallel-foreach parallel-list
syn keyword lispForm contained set setf setq psetq psetf
syn keyword lispForm contained with-multiple-let with-multiple-var multiple-setf defsetf

syn keyword lispDebug contained trace breakpoint assert assert-throws-exception assert-throws-no-exception

syn match lispKeyword 	"\<:\k\+\>"

syn match lispAmpersand "\<&\k\+\>"

syn match lispSpecial 	"\<\$\k\+\>"
syn match lispSpecial 	"\<_\k\+_\>"
syn match lispSpecial 	"\<+\k\++\>"
syn match lispSpecial 	"\<\*\k\+\*\>"

syn region lispString   start=+"""+ end=+"""+
syn region lispString   start=+#/+ skip=+//+ end=+/+
syn region lispString   start=+@"+ skip=+""+ end=+"+
syn region lispString	start=+"+ skip=+\\\\\|\\"+ end=+"+
syn region lispString   start=+#q{+ end=+}+
syn region lispString   start=+#q(+ end=+)+
syn region lispString   start=+#q\[+ end=+]+
syn region lispString   start=+#q<+ end=+>+

syn match lispConstant	"#\\\k\+"
syn match lispConstant  "true"
syn match lispConstant  "false"
syn match lispConstant  "null"

syn match lispComment   "#;"
syn match lispComment   "#!.*$"
syn match lispComment	";.*$"
syn region lispCommentRegion start="#|" end="|#" contains=lispCommentRegion
syn match lispConditional "#[+\-]"
syn match lispConditional "#if"
syn match lispConditional "#elif"
syn match lispConditional "#else"
syn match lispConditional "#endif"
syn match lispConditional "#ignore"

" ---------------------------------------------------------------------
" Synchronization:
syn sync lines=100

" ---------------------------------------------------------------------
" Define Highlighting:
" For version 5.7 and earlier: only when not done already
" For version 5.8 and later: only when an item doesn't have highlighting yet
if version >= 508
  command -nargs=+ HiLink hi def link <args>

  HiLink lispCommentRegion lispComment

  delcommand HiLink
endif


let b:current_syntax = "kiezel"


" ---------------------------------------------------------------------
" vim: ts=8 nowrap fdm=marker
"
"
hi lispconstant     guifg=#2377E5 ctermfg=33
hi lispcomment 		guifg=#b22222 ctermfg=124
hi lispform 		guifg=#800080 ctermfg=90
hi lispspecial 		guifg=#b8860b ctermfg=136
hi lispkeyword 		guifg=#da70d6 ctermfg=177
hi lispampersand 	guifg=#228b22 ctermfg=28
hi lispstring 		guifg=#bc8f8f ctermfg=174
hi lispconditional  guifg=#b22222 gui=bold ctermfg=124 cterm=bold
hi lispdebug        guifg=#b22222 gui=bold ctermfg=124 cterm=bold
