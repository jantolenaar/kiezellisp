;;;; Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

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

(defpackage "sql"
    (:export
        "with-open-database"
        "get-database"
        "execute-non-query"
        "execute-reader"
        "execute-scalar"
        "create-table"
        "get-schema"
        "get-text-fields"
        "get-non-text-fields"
        "$provider"
    ))

(in-package "sql")

(import "System.Data.DataTable")
(import "System.Data.DataSet")
(import "System.Data.SchemaType")
(import "System.Data.ConnectionState")
(import "System.Data.Common.DbProviderFactories")
(import "System.DBNull")


(defun fill-table (reader row-type)
    (var recs (vector))
    (while (.read reader)
        (var rec (new row-type))
        (loop
            (for index in (range 0 (.field-count reader)))
            (var key (.get-name reader index))
            (var value (.get-value reader index))
            (var value2 (convert-value-to-lisp value))
            (setf (elt rec (.to-lower key)) value2))
        (.add recs rec))
    (return recs))

(defun make-schema (table-name table keys)
    (var schema (new :table-name table-name
                     :keys keys
                     :column-defs (vector)
                     :columns (vector)
                     :identity null
                     :row-type null ))
    (foreach (column (.columns table))
        (var rec (new :name (.tolower (.column-name column))
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
    (var formatter (λx (string (.replace (.to-lower (.trim (.to-string (.key x)) "|")) "-" "_")
                                "="
                                (.value x))))
    (var connection-string (string.join ";" (map formatter connection-data)))
    (var provider (db-provider-factories.get-factory provider-name))
    (setf $provider provider)
    (var database (.create-connection provider))
    (setf (.connection-string database) connection-string)
    database)


(defmacro with-open-database ((database) &rest body)
    (var conn (gentemp "conn"))
    `(do
        (var ,conn ,database)
        (if (= (.state ,conn) connection-state.OPEN)
              (do ,@body)
            (do
                (.open ,conn)
                (try-finally
                    (do ,@body)
                    (.close ,conn))))))

(defun get-primary-keys (database table-name)
    (var keys null)
    (var filter (as-array (list (.database database)
                                        null
                                        table-name
                                        null)
                                   'string))
    (var get-collection-name (λr (.to-lower (elt r "collectionname"))))
    (var rows1 (.rows (.get-schema database)))
    (var collections (map get-collection-name rows1))
    (when (find "indexcolumns" collections)
        (setf keys (vector))
        (var rows (.rows (.get-schema database "indexcolumns" filter)))
        (loop
            (for row in rows)
            (var index-name (elt row "index_name"))
            (var column-name (elt row "column_name"))
            (when (not (.starts-with index-name "_wa_sys"))
                (.add keys (.to-lower column-name))))
        keys))


(defun get-schema (database table-name)
    (with-open-database (database)
        (using (adapter (.create-data-adapter $provider))
            (var text "select * from [<%=table-name%>]")
            (var command (get-command database text))
            (.set_select-command adapter command)
            (var dataset (data-set.new))
            (var tables (.fill-schema adapter dataset schema-type.MAPPED))
            (var keys (get-primary-keys database table-name))
            (make-schema table-name (elt tables 0) keys))))

(defun get-text-fields (database table-name)
    (do 
        (get-schema database table-name)
        (.column-defs ~)
        (filter .max-length ~)
        (map .name ~)))

(defun get-non-text-fields (database table-name)
    (do
        (get-schema database table-name)
        (.column-defs ~)
        (filter (lambda (x) (not (.max-length x))) ~)
        (map .name ~)))

(defun execute-non-query (database text parameters)
    (with-open-database (database)
        (var command (get-command database text parameters))
        (.execute-non-query command)))

(defun execute-scalar (database text &key parameters)
    (with-open-database (database)
        (var command (get-command database text parameters))
        (.execute-scalar command)))

(defun execute-reader (database text &key parameters row-type)
    (var command (get-command database text parameters))
    (with-open-database (database)
        (using (reader (.execute-reader command))
            (fill-table reader (or row-type (new))))))

(defun create-table (database table-name columns)
    (var format-column (lambda (c) (string "[" (first c) "] " (second c))))
    (var comma ",")
    (var text (string "create table [" table-name "] (" (string.join ")" (map format-column columns) comma)))
    (var parameters null)
    (execute-non-query database text parameters))

(defun add-parameter (command name value)
    (var param (.create-parameter command))
    (setf (.parameter-name param) name)
    (setf (.value param) (convert-value-from-lisp value))
    (.add (.parameters command) param))

(defun get-command (database command-text &optional parameters)
    ;(var command-builder (database.db-provider-factory.create-command-builder))
    ;(var prefix command-builder.quote-prefix)
    ;(var suffix command-builder.quote-suffix)
    (var prefix "[")
    (var suffix "]")
    (var command (.create-command database))
    (setf (.command-text command) (.replace (.replace command-text "[" prefix) "]" suffix))
    (foreach (pair parameters)
        (add-parameter command (string "@" (.key pair)) (.value pair)))
    (return command))


;;;;
;;;; utility functions
;;;;

(defun convert-value-to-lisp (value)
    (cond
        ((convert.is-dbnull value) null)
        ((string? value) (.trim value))
        (true value)))

(defun convert-value-from-lisp (value)
    (if (= value null)
          dbnull.VALUE
        value))



