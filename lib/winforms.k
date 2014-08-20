;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

(require :constructor-factory)

(reference "System.Windows.Forms" )

(defpackage :winforms)

(in-package :winforms)

(import "System.ComponentModel.Container" )
(import "System.Windows.Forms.Application" )
(import "System.Windows.Forms.Control" )
(import "System.Windows.Forms.Form" )
(import "System.Windows.Forms.Menu" )
(import "System.Windows.Forms.MainMenu" )
(import "System.Windows.Forms.MenuItem" )
(import "System.Windows.Forms.MessageBox" )
(import "System.Windows.Forms.ListBox" )
(import "System.Windows.Forms.ListView" )
(import "System.Windows.Forms.ListViewItem" )
(import "System.Windows.Forms.Button" )
(import "System.Windows.Forms.TextBox" )
(import "System.Windows.Forms.UserControl" )
(import "System.Windows.Forms.DockStyle")
(import "System.Windows.Forms.ColumnHeader")
(import "System.Windows.Forms.View")
(import "System.Windows.Forms.FlowLayoutPanel")
(import "System.Windows.Forms.DataGridView")
(import "System.Windows.Forms.DataGridViewColumn")
(import "System.Windows.Forms.DataGridViewTextBoxColumn")
(import "System.Windows.Forms.DataGridViewCheckBoxColumn")
(import "System.Windows.Forms.BindingSource")
(import "System.Windows.Forms.DataGridViewCellStyle")
(import "System.Windows.Forms.DataGridViewContentAlignment")
(import "System.Windows.Forms.DataGridViewRowsAddedEventArgs")
(import "System.Windows.Forms.SplitContainer")
(import "System.Windows.Forms.Orientation")
(import "System.Windows.Forms.RichTextBox")
(import "System.Windows.Forms.SaveFileDialog")
(import "System.Windows.Forms.OpenFileDialog")
(import "System.Windows.Forms.DialogResult")
(import "System.Windows.Forms.MessageBoxButtons")
(import "System.Windows.Forms.MessageBoxIcon")
(import "System.Windows.Forms.Keys")

(import "System.Drawing.Size")
(import "System.Drawing.Color")
(import "System.Drawing.SystemBrushes")
(import "System.Drawing.Font")
(import "System.Drawing.FontStyle")


(def tags '(
        form 
        list-view 
        user-control 
        data-grid-view 
        { :name data-grid-view-text-box-column :alias text-box-column }
        { :name data-grid-view-check-box-column :alias check-box-column }
        flow-layout-panel
        button
        main-menu
        menu-item
        column-header
        { :name data-grid-view-cell-style :alias cell-style }
        split-container
        rich-text-box
        save-file-dialog
        open-file-dialog
    ))

(constructor-factory:make-constructors tags)


