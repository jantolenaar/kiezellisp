;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

(require "html")

(defpackage "html"
	(:export
		"MAGIC-NUMBER"
		"make-table"
		"format-html-list"
		"get-default-stylesheet")
    (:use "html-elements"))

(in-package "html")

(def MAGIC-NUMBER -424242)

(defun make-table (layout table &key grouping blank-when-zero)

    (var row-number 0)
    (var row null)

    (letfun get-class (x)
        (cond
            ((= x MAGIC-NUMBER) "solid")
            ((not grouping) "border")
            ((= 0 (% row-number grouping)) "even")
            (true "odd")))

    (letfun format-header-col (x)
        (if (not (.width x))
                (<th> :class "data" (.caption x))
                (<th> :class "data" :style "width:<%=(.width x)%>em;" (.caption x))))

    (letfun format-header-row ()
        (<tr> (map format-header-col layout)))

    (letfun format-data-col (col)
        (let format (or (.format col) "{0}"))
        (let numeric (.starts-with format "{0:"))
        (let value1 (attr row (.expr col)))
        (let value2
            (if (and blank-when-zero
                     numeric
                     (number? value1)
                     (= 0 value1))
                  null
                value1))
        (let value3
            (cond
                ((null? value2) "&nbsp;")
                ((= "" value2) "&nbsp;")
                ((= value2 MAGIC-NUMBER) "x")
                (true (string:format format value2))))
        (let align
            (if (.centered col) "center" (if numeric "right" "left")))
        (if (= value3 "&nbsp;")
            (<td> :class (get-class value2) value3)
            (<td> :class (get-class value2) :style "text-align:<%=align%>" value3)))

    (letfun format-data-row (r)
        (setf row r)
        (setf row-number (+ 1 row-number))
        (<tr> (map format-data-col layout)))

    (<table> :cellspacing 0 :class "data"
            (format-header-row)
            (map format-data-row table)))

(defun format-html-list (column table)
    (<ul>
        (map (λr (<li> (elt r column))) table)))

(defun get-default-stylesheet ()

		"""
			table.toc
			{
				font-size: smaller;
				margin-left: 10em;
			}

			div.after
			{
				page-break-after: always;
			}

			table.data
			{
				font-family: monospace;
				/*white-space: nowrap;*/
			}

			th.data
			{
				padding-right: 16px;
			}

			td.even
			{
				padding-right: 3ex;
				padding-bottom: 1ex;
			}

			td.odd
			{
				padding-right: 3ex;
			}

			td.border
			{
				border: solid 1px gray;
			}

			td.solid
			{
				border: solid 1px gray;
				background: gray;
			}

			li
			{
				margin: 1em;
			}

			span.t1 .t2 .t3 .t4. .t5 .t6 .t7 .t8 .t9
			{
				border: solid 1px gray;
				padding-left: 16px;
				padding-right: 16px;
			}

			span.nowrap
			{
				white-space: nowrap;
			}

			span.t1
			{
				color:white;
				background: blue;
			}

			span.t2
			{
				color:white;
				background: red;
			}

			span.t3
			{
				background: yellow;
			}

			span.t4
			{
				color:white;
				background: green;
			}

			span.t5
			{
				color:white;
				background: purple;
			}

			span.t6
			{
				background: orange;
			}

			span.t7
			{
				background: pink;
			}

			span.t8
			{
				background: white;
			}

			span.t9
			{
				background: lightblue;
			}								"""
)
