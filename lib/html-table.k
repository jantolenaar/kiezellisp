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

(defstruct column
    "Defines a column of the html table"
    binder
    width
    centered
    caption
    format)

(defun make-table (layout table &key grouping blank-when-zero)

    (var row-number 0)
    (var row null)

    (letfun get-class (x)
        (cond
            ((= x MAGIC-NUMBER) "solid")
            ((not grouping) "border")
            ((= 1 (% row-number grouping)) "top")
            ((= 0 (% row-number grouping)) "bot")
            (true "mid")))

    (letfun format-header-col (x)
        (let w (column-width x))
        (let c (column-caption x))
        (if (not w)
                (<th> :class "data" c)
                (<th> :class "data" :style "width:<%=w%>em;" c)))

    (letfun format-header-row ()
        (<tr> (map format-header-col layout)))

    (letfun format-data-col (col)
        (let format (or (column-format col) "{0}"))
        (let numeric (.starts-with format "{0:"))
        (let value1 ((column-binder col) row))
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
            (if (column-centered col) "center" (if numeric "right" "left")))
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
        (map #(<li> (elt %1 column)) table)))

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

            th, td
            {
                padding-right: 1ex;
                padding-left: 1ex;
            }

            td
            {
                border-left: solid 1px gray;
                border-right: solid 1px gray;
            }

			td.top
			{
                border-top: solid 1px gray;
			}

			td.bot
			{
                border-bottom: solid 1px gray;
			}

            td.mid
            {
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

			span.nowrap
			{
				white-space: nowrap;
			}
        """
)
