.index prog
.usage special-form
.syntax
(prog (binding*) form*)

.description
`prog` allows tail recursion with `reprog`. The `binding` defines
the parameters and initial arguments of the recursion. The `reprog` form
should be in a tail position. If `reprog` is not used, `prog` is similar to
Common Lisp's `prog`.

The code of a `prog` is contained in a `block prog`, Therefore 
`leave prog` returns from the directly enclosing `prog`. The special 
form `reprog` is implemented with `redo prog`.

.examples
(defun fac (n)
    (prog ((n n) (a 1))
        (if (plus? n)
              (reprog (dec n) (* a n))
            a)))
(fac 30)

.see-also
.api
reprog
