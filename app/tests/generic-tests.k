

(defmulti f (a b))

(defmethod f (a b)
    (print-line "any: " a " any: " b)
    (print-line))

(defmethod f ((a number) b)
    (print-line "number: " a " any: " b)
    (call-next-method))

(defmethod f ((a (eql 12)) (b number))
    (print-line "twelve: " a " number: " b)
    (call-next-method))

(defmethod f ((a (eql 12)) (b number))
    (print-line "twelve: " a " number2: " b)
    (call-next-method))

(defmethod f ((a (eql 12)) (b string))
    (print-line "twelve: " a " string: " b)
    (call-next-method))

(f "a" 1)
(f 12 1)
(f 1 1)
(f 12 "a")



