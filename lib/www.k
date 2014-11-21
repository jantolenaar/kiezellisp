;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; www.k
;;;;

(defpackage "www"
	(:export
	    "get"
        "ddg"))


(in-package "www")

(import "System.Net.WebRequest")
(import "System.Net.HttpStatusCode")
(import "System.Net.CookieContainer")
(import "System.Uri")


(defun get (url &key cookie-container headers)
    (using (response (get-response url cookie-container headers))
        (let type (.content-type response))
        (let result (as-prototype response))
        (declare (ignore type))
        (using (stream (.get-response-stream response))
            (let encoding (encoding:get-encoding (.character-set response)))
            (using (reader (stream-reader:new stream encoding))
                (setf (.content result) (.read-to-end reader))))
        result))

        
(defun get-response (url cookie-container headers)
    (let request (web-request:create (url-with-format url)))
    (loop
        (for item on-list headers by cddr)
        (.add (.headers request) (first item) (second item)))
    (describe (.headers request))
    (setf (.cookie-container request) cookie-container)
    (.get-response request))

(defun url-with-format (url)
    (if (and (string? url) 
             (not (.starts-with url "http://")))
         (string "http://" url)
       url))

(defun ddg (query)
    (let url (string "http://api.duckduckgo.com/?q=" (string:url-encode query) "&format=json"))
    (let result (get url))
    (let data (.json-decode (.content result)))
    data)
            
    