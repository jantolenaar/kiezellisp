
(defun fibo (a b) 
    (cons a (delay (fibo b (+ a b)))))
