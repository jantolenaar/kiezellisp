;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; http-server.k
;;;;

(defpackage "http-server"
	(:export
		"$request"
		"$response"
		"create-exact-dispatcher"
		"create-prefix-dispatcher"
		"get"
		"setup"
		"start"
		"stop"
		"write-response-bytes"
		"write-response-error"
		"write-response-file"
		"write-response-text"
		))

(in-package "http-server")

(load "mime-types")

(import "System.Convert")
(import "System.Net.IPAddress" :package-name "ip-address")
(import "System.Text.Encoding")
(import "System.Net.HttpListener")

(def $listener null)

(def $prefixes null)
(def $dispatch-table null)

(def $request null)
(def $response null)

(def $$request null)
(def $$response null)


;;;
;;; setup generic server
;;;

(defun setup (&key prefixes url-dispatch-table)
    (setf $prefixes prefixes)
    (setf $dispatch-table url-dispatch-table))

(defun start ()
    (stop)
    (when $prefixes)
        (setf $listener (http-listener:new))
        (foreach (prefix $prefixes)
            (.add (.prefixes $listener) prefix))
        (.start $listener)
        (loop 
            (let context (.get-context $listener))
            (process-context context)))

(defun stop ()
    (when $listener
        (.stop $listener)
        (.close $listener)
        (setf $listener null)))


;;;
;;; defining url dispatchers
;;;


(defun create-prefix-dispatcher (handler virtual-root args)
    (letfun call-prefix-handler (url)
        (funcall handler url virtual-root args))
    (letfun try-get-prefix-handler (url)
        (if (or (= url virtual-root)
                (and (.starts-with url virtual-root)
                     (= (elt url (length virtual-root)) #\/)))
            call-prefix-handler))
    try-get-prefix-handler)

(defun create-exact-dispatcher (handler virtual-root args)
    (letfun call-exact-handler (url)
        (funcall handler url virtual-root args))
    (letfun try-get-exact-handler (url)
        (if (= url virtual-root)
            call-exact-handler))
    try-get-exact-handler)

;;;
;;; context handler
;;;

(defun process-context (context)
    (let $request (.request context))
    (using ($response (.response context))
        (setf $$request $request)
        (setf $$response $response)
        (let url (.trim-end (.local-path (.url $request)) "/"))
        (let result (try-and-catch (find-and-call-url-handler url)))
        (when (exception? result)
            (let diag (get-diagnostics result))
            (write-response-text diag))))

(defun find-and-call-url-handler (url)
    (let handler (get-url-handler url))
    (funcall handler url))

(defun get-url-handler (url)
    (foreach (try-get-url-handler $dispatch-table)
        (when-let (url-handler (try-get-url-handler url))
            (return url-handler)))
    default-url-handler)

(defun default-url-handler (url)
    (write-response-text "Not implemented: <%= url %>"))

(defun get (url)
    ;; Used for debugging
    (let $request null)
    (let $response null)
    (find-and-call-url-handler (.trim-end url "/")))

;;;
;;; response writers
;;;

(defun write-response-text (text &key content-type)
    (if $response
          (write-response-bytes (.get-bytes (encoding:utf8) text) :content-type content-type)
        (print-line text)))

(defun write-response-bytes (bytes &key content-type)
    (setf (.content-type $response) (or content-type "text/plain"))
    (setf (.content-length64 $response) (as-int64 (.length bytes)))
    (using (stream (.output-stream $response))
        (.write stream bytes 0 (.length bytes)))
    (.close $response))


(defun write-response-error (code msg)
    (setf (.status-code $response) code)
    (write-response-text msg))


(defun write-response-file (path &key content-type)
    (let content-type (or content-type (mime-types:get-mime-type path)))
    (let bytes (file:read-all-bytes path))
    (write-response-bytes bytes :content-type content-type))
