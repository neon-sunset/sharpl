(check (Bit F:F))

(check 3 (3:4 0))
(check 4 (3:4 1))
(check 3:4:5 (1:2:3:4:5 2))

(check 1:4:3
  (let [foo 1:2:3]
    (foo 1 4)))

(check [1 2 3]
  [1:2:3*])

(check 1:2:3
  (Pair [1 2 3]*))

(check 3
  (length 1:2:3))

(check 1:2:3
  (let [foo 2:3] 
    (push foo 1)
    foo))

(check 1
  (peek 1:2:3)) 

(check 1:[2:3]
  (let [foo 1:2:3] 
    (pop foo):[foo]))