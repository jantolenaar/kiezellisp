;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.


(defpackage "ftp"
    (:export
        "get-bytes"
        "get-file"
        "make-credentials"
        "put-bytes"
        "put-file"
    ))

(in-package "ftp")

(import "System.Byte")
(import "System.IO.FileMode")
(import "System.IO.FileStream")
(import "System.IO.MemoryStream")
(import "System.Net.NetworkCredential")
(import "System.Net.WebRequest")
(import "System.Net.WebRequestMethods+Ftp" :package-name "ftp-methods")

(defun copy-stream (stream-from stream-to)
    (let max-size (* 1024 1024))
    (let buffer (create-array 'byte max-size))
    (var size null)
    (while (not (zero? (setf size (.read stream-from buffer 0 max-size))))
        (.write stream-to buffer 0 size)))

(defun make-credentials (&key user password)
    (network-credential:new user password))

(defun get-bytes (remote-filename host &key credentials (passive true))
    (let request (web-request:create (string "ftp://" host "/" remote-filename)))
    (when credentials (setf (.credentials request) credentials))
    (setf (.method request) ftp-methods:DOWNLOAD-FILE)
    (when (not passive) (setf (.use-passive request) passive))
    (using (response (.get-response request))
        (using (stream-in (.get-response-stream response))
            (using (stream-out (memory-stream:new))
                (copy-stream stream-in stream-out)
                (.to-array stream-out)))))


(defun get-file (local-filename remote-filename host &key credentials (passive true))
    (let request (web-request:create (string "ftp://" host "/" remote-filename)))
    (when credentials (setf (.credentials request) credentials))
    (setf (.method request) ftp-methods:DOWNLOAD-FILE)
    (when (not passive) (setf (.use-passive request) passive))
    (using (response (.get-response request))
        (using (stream-in (.get-response-stream response))
            (using (stream-out (file-stream:new local-filename file-mode:CREATE))
                (copy-stream stream-in stream-out)))
        (.status-description response)))

(defun put-bytes (bytes remote-filename host &key credentials (passive true))
    (let request (web-request:create (string "ftp://" host "/" remote-filename)))
    (when credentials (setf (.credentials request) credentials))
    (setf (.method request) ftp-methods:UPLOAD-FILE)
    (when (not passive) (setf (.use-passive request) passive))
    (using (stream-in (memory-stream:new bytes))
        (using (stream-out (.get-request-stream request))
            (copy-stream stream-in stream-out)))
    (using (response (.get-response request))
        (.status-description response)))

(defun put-file (local-filename remote-filename host &key credentials (passive true))
    (let request (web-request:create (string "ftp://" host "/" remote-filename)))
    (when credentials (setf (.credentials request) credentials))
    (setf (.method request) ftp-methods:UPLOAD-FILE)
    (when (not passive) (setf (.use-passive request) passive))
    (using (stream-in (file-stream:new local-filename file-mode:OPEN))
        (using (stream-out (.get-request-stream request))
            (copy-stream stream-in stream-out)))
    (using (response (.get-response request))
        (.status-description response)))


