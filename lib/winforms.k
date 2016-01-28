;;;; Copyright (c) Jan Tolenaar. See the file LICENSE for details.

(pushnew :winforms $features)

(require :constructor-factory)

(reference "System.Windows.Forms" )
(reference "System.Drawing")

(defpackage :winforms)

(in-package :winforms)


(import "System.ComponentModel.Container" )
(import "System.Drawing.SolidBrush")
(import "System.Drawing.Brushes")
(import "System.Drawing.Color")
(import "System.Drawing.Font")
(import "System.Drawing.FontStyle")
(import "System.Drawing.Size")
(import "System.Drawing.SystemBrushes")
(import "System.Windows.Forms.Application" )
(import "System.Windows.Forms.BindingSource")
(import "System.Windows.Forms.Button" )
(import "System.Windows.Forms.ColumnHeader")
(import "System.Windows.Forms.Control" )
(import "System.Windows.Forms.DataGridView")
(import "System.Windows.Forms.DataGridViewCellStyle")
(import "System.Windows.Forms.DataGridViewCheckBoxColumn")
(import "System.Windows.Forms.DataGridViewColumn")
(import "System.Windows.Forms.DataGridViewContentAlignment")
(import "System.Windows.Forms.DataGridViewRowsAddedEventArgs")
(import "System.Windows.Forms.DataGridViewTextBoxColumn")
(import "System.Windows.Forms.DialogResult")
(import "System.Windows.Forms.DockStyle")
(import "System.Windows.Forms.FlowLayoutPanel")
(import "System.Windows.Forms.Form" )
(import "System.Windows.Forms.Keys")
(import "System.Windows.Forms.ListBox" )
(import "System.Windows.Forms.ListView" )
(import "System.Windows.Forms.ListViewItem" )
(import "System.Windows.Forms.MainMenu" )
(import "System.Windows.Forms.Menu" )
(import "System.Windows.Forms.MenuItem" )
(import "System.Windows.Forms.MessageBox" )
(import "System.Windows.Forms.MessageBoxButtons")
(import "System.Windows.Forms.MessageBoxIcon")
(import "System.Windows.Forms.OpenFileDialog")
(import "System.Windows.Forms.Orientation")
(import "System.Windows.Forms.Padding")
(import "System.Windows.Forms.RichTextBox")
(import "System.Windows.Forms.SaveFileDialog")
(import "System.Windows.Forms.SplitContainer")
(import "System.Windows.Forms.FormStartPosition")
(import "System.Windows.Forms.Shortcut")
(import "System.Windows.Forms.TabControl")
(import "System.Windows.Forms.TabPage")
(import "System.Windows.Forms.TextBox" )
(import "System.Windows.Forms.TreeView" )
(import "System.Windows.Forms.UserControl" )
(import "System.Windows.Forms.View")


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
        tab-control
        tab-page
        tree-view
        text-box
    ))

(constructor-factory:make-constructors tags)


