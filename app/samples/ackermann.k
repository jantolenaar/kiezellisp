
(defun ackermann (m n)
    (cond
        ((zero? m) (inc n))
        ((zero? n) (tailcall ackermann (dec m) 1))
        (true (tailcall ackermann (dec m) (ackermann m (dec n))))))
