 ;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.


;;;;
;;;; diagnostics.k
;;;;

(defpackage "lisp"
    (:export
        "decompile"
    ))

(in-package "lisp")

;(import-namespace "System.Reflection.*" :package-name-prefix "reflection-")
(import "System.Reflection.MethodInfo" :package-name-prefix "reflection-")
(import "System.Reflection.PropertyInfo" :package-name-prefix "reflection-")

;(import-namespace "Microsoft.Scripting.Ast.*" :package-name-prefix "ast-")
(import "Microsoft.Scripting.Ast.LightTypedDynamicExpressionN" :package-name-prefix "ast-")

;(import-namespace "System.Linq.Expressions.*" :package-name-prefix "linq-")
(import "System.Linq.Expressions.AssignBinaryExpression" :package-name-prefix "linq-")
(import "System.Linq.Expressions.BlockExpression" :package-name-prefix "linq-")
(import "System.Linq.Expressions.ConditionalExpression" :package-name-prefix "linq-")
(import "System.Linq.Expressions.ConstantExpression" :package-name-prefix "linq-")
(import "System.Linq.Expressions.DynamicExpression" :package-name-prefix "linq-")
(import "System.Linq.Expressions.GotoExpression" :package-name-prefix "linq-")
(import "System.Linq.Expressions.LabelExpression" :package-name-prefix "linq-")
(import "System.Linq.Expressions.LabelTarget" :package-name-prefix "linq-")
(import "System.Linq.Expressions.MethodCallExpression" :package-name-prefix "linq-")
(import "System.Linq.Expressions.ParameterExpression" :package-name-prefix "linq-")
(import "System.Linq.Expressions.PropertyExpression" :package-name-prefix "linq-")
(import "System.Linq.Expressions.PropertyExpression" :package-name-prefix "linq-")
(import "System.Linq.Expressions.ScopeExpression" :package-name-prefix "linq-")
(import "System.Linq.Expressions.TryExpression" :package-name-prefix "linq-")

(import "Kiezel.LambdaClosure")
(import "Kiezel.LambdaDefinition")

(defmulti decompile (code))

(defmethod decompile (code)
    code)

(defmethod decompile ((code (eql null)))
    (declare (ignore code))
    null)

(defmethod decompile ((code cons))
	(decompile (compile code)))

(defmethod decompile ((code lambda-closure))
	(decompile (.definition code)))

(defmethod decompile ((code lambda-definition))
	(decompile (.code code)))

(defmethod decompile ((code ast-light-typed-dynamic-expression-n))
   `(DYNAMIC ,(decompile (.binder code)) ,@(map decompile (.arguments code))))

;(defmethod decompile ((code linq-scope-expression))
;    (decompile-scope code))

(defmethod decompile ((code linq-block-expression))
    (decompile-block code))

(defun decompile-block (code)	
	(when code
		(let vars (.variables code))
		(let exprs (.expressions code))
		(if vars
			`(BLOCK (VARIABLES ,@(map decompile vars)) ,@(map decompile exprs))
			`(BLOCK ,@(map decompile exprs)))))

(defun decompile-scope (code)   
    (when code
        (let vars (.variables code))
        (let exprs (.expressions code))
        (if vars
            `(SCOPE (VARIABLES ,@(map decompile vars)) ,@(map decompile exprs))
            `(SCOPE ,@(map decompile exprs)))))

(defmethod decompile ((code linq-parameter-expression))
	(decompile-name code))

(defmethod decompile ((code linq-assign-binary-expression))
	`(ASSIGN ,(decompile (.left code)) ,(decompile (.right code))))

(defmethod decompile ((code linq-label-expression))
	(let default (decompile (.default-value code)))
	(if (null? default)
		`(LABEL ,(decompile (.target code)))
		`(LABEL ,(decompile (.target code)) ,default)))

(defmethod decompile ((code linq-label-target))
	(decompile-name code))

(defun decompile-name (code)
	(if (.name code)
		(make-symbol (.name code))
		'unnamed))

(defmethod decompile ((code linq-method-call-expression))
	`(CALL ,(decompile (.method code)) ,@(map decompile (.arguments code))))

(defmethod decompile ((code reflection-method-info))
	(decompile-name code))

(defmethod decompile ((code linq-constant-expression))
	(.value code))

(defmethod decompile ((code linq-dynamic-expression))
	`(DYNAMIC ,(decompile (.binder code)) ,@(map decompile (.arguments code))))

(defmethod decompile ((code linq-property-expression))
	`(,(decompile (.member code)) ,(decompile (.expression code))))

(defmethod decompile ((code reflection-property-info))
	`(. ,(.name code)))

(defmethod decompile ((code linq-goto-expression))
	(let value (decompile (.value code)))
	(if (null? value)
		`(GOTO ,(decompile (.target code)))
		`(GOTO ,(decompile (.target code)) ,value)))

(defmethod decompile ((code linq-conditional-expression))
	`(IF ,(decompile (.test code)) ,(decompile (.if-true code)) ,(decompile (.if-false code))))

(defmethod decompile ((code linq-try-expression))
	`(TRY ,(decompile (.body code))
		 ,@(map decompile-catch (.handlers code))
		 ,@(map decompile-finally (if (.finally code) (list (.finally code))))))

(defun decompile-catch (code)
	`(CATCH (,(decompile (.variable code)) ,(decompile (.test code))) ,(decompile (.body code))))

(defun decompile-finally (code)
	`(FINALLY ,(decompile code)))

