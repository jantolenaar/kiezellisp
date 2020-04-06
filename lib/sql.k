;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

;;;;
;;;; sql.k
;;;;
;;;; Provides ADO.NET bindings for Microsoft SqlServer, OleDb, Odbc and for MySQL.
;;;;
;;;; Database provider types:
;;;;
;;;;     System.Data.SqlClient.SqlClientFactory
;;;;     System.Data.OleDb.OleDbFactory
;;;;     System.Data.Odbc.OdbcFactory
;;;;
;;;;     assembly: "MySql.Data, Version=1.0.7.30072, Culture=neutral, PublicKeyToken=c5687fc88969c44d"
;;;;     System.Data.MySqlClient.MySqlFactory
;;;;

(package "sql")

(public "$provider"
        "create-table"
        "execute-non-query"
        "execute-reader"
        "execute-scalar"
        "get-database"
        "get-non-text-fields"
        "get-schema"
        "get-text-fields"
        "with-open-database" )

(import "System.Data.Common.DbProviderFactories")
(import "System.Data.ConnectionState")
(import "System.Data.DataSet")
(import "System.Data.DataTable")
(import "System.Data.SchemaType")
(import "System.DBNull")


(defun fill-table (reader row-type)
    (let recs (vector))
    (while (.read reader)
        (let rec (new row-type))
        (loop
            (for index in (range 0 (.field-count reader)))
            (let key (.get-name reader index))
            (let value (.get-value reader index))
            (let value2 (convert-value-to-lisp value))
            (setf (elt rec (.to-lower key)) value2))
        (.add recs rec))
    (return recs))

(defun make-schema (table-name table keys)
    (let schema (new :table-name table-name
                     :keys keys
                     :column-defs (vector)
                     :columns (vector)
                     :identity null
                     :row-type null ))
    (foreach (column (.columns table))
        (let rec (new :name (.tolower (.column-name column))
                      :allow-db-null (.allow-db-null column)
                      :auto-increment (.auto-increment column)
                      :max-length (if (/= -1 (.max-length column)) (.max-length column))
                      :read-only (.read-only column)
                      :expression (.expression column)))
        (when (.auto-increment rec)
            (setf (.identity schema) rec))
        (.add (.column-defs schema) rec)
        (.add (.columns schema) (.name rec)))
    (return schema))

(def $provider null)

(defun get-database (provider-name connection-data)
    (let formatter #(string (.replace (.to-lower (.trim (.to-string (.key %1)) "|")) "-" "_")
                                "="
                                (.value %1)))
    (let connection-string (string:join ";" (map formatter connection-data)))
    (let provider (db-provider-factories:get-factory provider-name))
    (setf $provider provider)
    (let database (.create-connection provider))
    (setf (.connection-string database) connection-string)
    database)


(defmacro with-open-database ((database) &rest body)
    (let conn (gentemp "conn"))
    `(do
        (let ,conn ,database)
        (if (= (.state ,conn) connection-state:OPEN)
              (do ,@body)
            (do
                (.open ,conn)
                (try
                    ,@body
                    (finally (.close ,conn)))))))

(defun get-primary-keys (database table-name)
    (var keys null)
    (let filter (as-array (list (.database database)
                                        null
                                        table-name
                                        null)
                                   'string))
    (let get-collection-name #(.to-lower (elt %1 "collectionname")))
    (let rows1 (.rows (.get-schema database)))
    (let collections (map get-collection-name rows1))
    (when (index-of "indexcolumns" collections)
        (setf keys (vector))
        (let rows (.rows (.get-schema database "indexcolumns" filter)))
        (loop
            (for row in rows)
            (let index-name (elt row "index_name"))
            (let column-name (elt row "column_name"))
            (when (not (.starts-with index-name "_wa_sys"))
                (.add keys (.to-lower column-name))))
        keys))


(defun get-schema (database table-name)
    (with-open-database (database)
        (using (adapter (.create-data-adapter $provider))
            (let text "select * from [<%=table-name%>]")
            (let command (get-command database text))
            (.set_select-command adapter command)
            (let dataset (data-set:new))
            (let tables (.fill-schema adapter dataset schema-type:MAPPED))
            (let keys (get-primary-keys database table-name))
            (make-schema table-name (elt tables 0) keys))))

(defun get-text-fields (database table-name)
    (chain (~)
        (get-schema database table-name)
        (.column-defs ~)
        (filter .max-length ~)
        (map .name ~)))

(defun get-non-text-fields (database table-name)
    (chain (~)
        (get-schema database table-name)
        (.column-defs ~)
        (filter (lambda (x) (not (.max-length x))) ~)
        (map .name ~)))

(defun execute-non-query (database text parameters)
    (with-open-database (database)
        (let command (get-command database text parameters))
        (.execute-non-query command)))

(defun execute-scalar (database text &key parameters)
    (with-open-database (database)
        (let command (get-command database text parameters))
        (.execute-scalar command)))

(defun execute-reader (database text &key parameters row-type)
    (let command (get-command database text parameters))
    (with-open-database (database)
        (using (reader (.execute-reader command))
            (fill-table reader (or row-type (new))))))

(defun create-table (database table-name columns)
    (let format-column (lambda (c) (string "[" (first c) "] " (second c))))
    (let comma ",")
    (let text (string "create table [" table-name "] (" (string:join ")" (map format-column columns) comma)))
    (let parameters null)
    (execute-non-query database text parameters))

(defun add-parameter (command name value)
    (let param (.create-parameter command))
    (setf (.parameter-name param) name)
    (setf (.value param) (convert-value-from-lisp value))
    (.add (.parameters command) param))

(defun get-command (database command-text &optional parameters)
    ;(let command-builder (database:db-provider-factory:create-command-builder))
    ;(let prefix command-builder:quote-prefix)
    ;(let suffix command-builder:quote-suffix)
    (let prefix "[")
    (let suffix "]")
    (let command (.create-command database))
    (setf (.command-text command) (.replace (.replace command-text "[" prefix) "]" suffix))
    (foreach (pair parameters)
        (add-parameter command (string "@" (.key pair)) (.value pair)))
    (return command))


;;;;
;;;; utility functions
;;;;

(defun convert-value-to-lisp (value)
    (cond
        ((convert:is-dbnull value) null)
        ((string? value) (.trim value))
        (true value)))

(defun convert-value-from-lisp (value)
    (if (= value null)
          dbnull:VALUE
        value))



