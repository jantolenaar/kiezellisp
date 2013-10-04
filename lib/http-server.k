;;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; http-server.k
;;;;

(defpackage "http-server"
	(:export
		"setup"
		"start"
		"stop"
		"get"
		"create-prefix-dispatcher"
		"create-exact-dispatcher"
		"write-response-error"
		"write-response-text"
		"write-response-bytes"
		"write-response-file"
		"$request"
		"$response"))

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
        (setf $listener (http-listener.new))
        (foreach (prefix $prefixes)
            (.add (.prefixes $listener) prefix))
        (.start $listener)
        (loop 
            (var context (.get-context $listener))
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
    (var call-prefix-handler 
        (lambda url
            (funcall handler url virtual-root args)))
    (var try-get-prefix-handler
        (lambda url
            (if (or (= url virtual-root)
                    (and (.starts-with url virtual-root)
                         (= (elt url (length virtual-root)) #\/)))
                call-prefix-handler)))
    try-get-prefix-handler)

(defun create-exact-dispatcher (handler virtual-root args)
    (var call-exact-handler 
        (lambda url
            (funcall handler url virtual-root args)))
    (var try-get-exact-handler 
        (lambda url
            (if (= url virtual-root)
                call-exact-handler)))
    try-get-exact-handler)

;;;
;;; context handler
;;;

(defun process-context (context)
    (var $request (.request context))
    (using ($response (.response context))
        (setf $$request $request)
        (setf $$response $response)
        (var url (.trim-end (.local-path (.url $request)) "/"))
        (var result (try-and-catch (find-and-call-url-handler url)))
        (when (exception? result)
            (var diag (get-diagnostics result))
            (write-response-text diag))))

(defun find-and-call-url-handler (url)
    (var handler (get-url-handler url))
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
    (var $request null)
    (var $response null)
    (find-and-call-url-handler (.trim-end url "/")))

;;;
;;; response writers
;;;

(defun write-response-text (text &key content-type)
    (if $response
          (write-response-bytes (.get-bytes (encoding.utf8) text) :content-type content-type)
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
    (var content-type (or content-type (mime-types.get-mime-type path)))
    (var bytes (file.read-all-bytes path))
    (write-response-bytes bytes :content-type content-type))
