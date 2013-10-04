;;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

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


(defun get (url)
    (using (response (get-response url))
        (var type (.content-type response))
        (var result (as-prototype response))
        (using (stream (.get-response-stream response))
            (var encoding (encoding.get-encoding (.character-set response)))
            (using (reader (stream-reader.new stream encoding))
                (setf (.content result) (.read-to-end reader))))
        result))

        
(defun get-response (url)
    (var request (web-request.create (url-with-format url)))
    (.get-response request))

(defun url-with-format (url)
    (if (and (string? url) 
             (not (.starts-with url "http://")))
         (string "http://" url)
       url))

(defun ddg (query)
    (var url (string "http://api.duckduckgo.com/?q=" (string.url-encode query) "&format=json"))
    (var result (get url))
    (var data (.json-decode (.content result)))
    data)
            
    