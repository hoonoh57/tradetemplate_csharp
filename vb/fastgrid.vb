Imports System.Windows.Forms
Imports System.Drawing
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Linq

    Public Class FastGrid
        Inherits Control

        Private _vScrollBar As VScrollBar
        Private _rowHeight As Integer = 24
        Private _headerHeight As Integer = 30

        ' Internal Data Model (Hybrid Support)
        Public Class GridColumn
            Public Name As String
            Public Title As String
            Public Width As Integer
        End Class
        Private _columns As New List(Of GridColumn)()
        Private _rows As New List(Of Dictionary(Of String, String))()

        Public ReadOnly Property Columns As List(Of GridColumn)
            Get
                Return _columns
            End Get
        End Property

        ' Events
        Public Event CellDoubleClick As EventHandler(Of DataGridViewCellEventArgs)
        Public Event CellClick As EventHandler(Of DataGridViewCellEventArgs)
        Public Event CellValueNeeded As EventHandler(Of CellValueNeededEventArgs)

        Public Sub New()
            Me.DoubleBuffered = True
            Me.ResizeRedraw = True

            _vScrollBar = New VScrollBar()
            _vScrollBar.Dock = DockStyle.Right
            _vScrollBar.Visible = False
            Me.Controls.Add(_vScrollBar)

            AddHandler _vScrollBar.Scroll, AddressOf OnScroll
        End Sub

        ' ==========================================
        ' Public API
        ' ==========================================
        Public Sub AddColumn(name As String, title As String, width As Integer)
            _columns.Add(New GridColumn With {.Name = name, .Title = title, .Width = width})
            Me.Invalidate()
        End Sub

        Public Sub AddRow(rowData As Dictionary(Of String, String))
            _rows.Add(New Dictionary(Of String, String)(rowData))
            UpdateScroll()
            Me.Invalidate()
        End Sub

        Public Sub ClearRows()
            _rows.Clear()
            _selectedRowIndex = -1
            UpdateScroll()
            Me.Invalidate()
        End Sub

        Public Property RowCount As Integer
            Get
                Return Math.Max(_rows.Count, _virtualRowCount)
            End Get
            Set(value As Integer)
                _virtualRowCount = value
                UpdateScroll()
                Me.Invalidate()
            End Set
        End Property

        Private _virtualRowCount As Integer = 0
        Private _selectedRowIndex As Integer = -1

        ' ==========================================
        ' Valid Virtual Mode Implementation
        ' ==========================================
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            Dim g = e.Graphics
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

            ' 1. Draw Header
            Dim headerRect As New Rectangle(0, 0, Me.Width, _headerHeight)
            Using b As New SolidBrush(Color.FromArgb(40, 40, 40))
                g.FillRectangle(b, headerRect)
            End Using
            g.DrawLine(Pens.Gray, 0, _headerHeight - 1, Me.Width, _headerHeight - 1)

            Dim hX As Integer = 0
            For Each col In _columns
                Dim cellRect As New Rectangle(hX, 0, col.Width, _headerHeight)
                TextRenderer.DrawText(g, col.Title, Me.Font, cellRect, Color.Silver, TextFormatFlags.VerticalCenter Or TextFormatFlags.HorizontalCenter)
                g.DrawLine(Pens.DimGray, cellRect.Right, 0, cellRect.Right, _headerHeight)
                hX += col.Width
            Next

            ' 2. Draw Data Rows
            Dim scrollOffset = _vScrollBar.Value
            Dim visibleRows = GetVisibleRowCount() + 1
            Dim totalRows = Me.RowCount

            Dim firstRow = scrollOffset
            Dim lastRow = Math.Min(totalRows - 1, firstRow + visibleRows)

            For i = firstRow To lastRow
                Dim y = _headerHeight + (i - firstRow) * _rowHeight
                Dim rowRect As New Rectangle(0, y, Me.Width - (If(_vScrollBar.Visible, _vScrollBar.Width, 0)), _rowHeight)

                ' Background
                If i = _selectedRowIndex Then
                    Using b As New SolidBrush(Color.FromArgb(60, 60, 80))
                        g.FillRectangle(b, rowRect)
                    End Using
                    g.DrawRectangle(Pens.DodgerBlue, rowRect.X, rowRect.Y, rowRect.Width - 1, rowRect.Height - 1)
                ElseIf i Mod 2 = 0 Then
                    g.FillRectangle(Brushes.Black, rowRect)
                Else
                    Using b As New SolidBrush(Color.FromArgb(20, 20, 20))
                        g.FillRectangle(b, rowRect)
                    End Using
                End If

                ' Cell Values
                Dim x As Integer = 0
                For c = 0 To _columns.Count - 1
                    Dim col = _columns(c)
                    Dim cellRect As New Rectangle(x, y, col.Width, _rowHeight)

                    Dim val As String = ""
                    Dim textColor As Color = Color.WhiteSmoke

                    ' First try to get from internal _rows data
                    If i < _rows.Count AndAlso _rows(i).ContainsKey(col.Name) Then
                        val = _rows(i)(col.Name)
                    Else
                        ' Virtual Mode fallback
                        Dim args As New CellValueNeededEventArgs(i, c)
                        RaiseEvent CellValueNeeded(Me, args)
                        If args.Value IsNot Nothing Then val = args.Value.ToString()
                        If Not args.TextColor.IsEmpty Then textColor = args.TextColor
                    End If

                    TextRenderer.DrawText(g, val, Me.Font, cellRect, textColor, TextFormatFlags.VerticalCenter Or TextFormatFlags.Right)
                    g.DrawLine(Pens.DimGray, cellRect.Right, cellRect.Top, cellRect.Right, cellRect.Bottom)
                    x += col.Width
                Next
                g.DrawLine(Pens.DimGray, rowRect.Left, rowRect.Bottom, rowRect.Right, rowRect.Bottom)
            Next
        End Sub

        ' ... Scroll and Mouse Logic ...
        Private Sub UpdateScroll()
            Dim totalRows = Me.RowCount
            Dim visibleRows = GetVisibleRowCount()
            If totalRows > visibleRows Then
                _vScrollBar.Visible = True
                _vScrollBar.Maximum = totalRows - 1
                _vScrollBar.LargeChange = visibleRows
            Else
                _vScrollBar.Visible = False
                _vScrollBar.Value = 0
            End If
        End Sub

        Private Function GetVisibleRowCount() As Integer
            Return Math.Max(1, (Me.Height - _headerHeight) \ _rowHeight)
        End Function

        Private Sub OnScroll(sender As Object, e As ScrollEventArgs)
            Me.Invalidate()
        End Sub

        Protected Overrides Sub OnResize(e As EventArgs)
            MyBase.OnResize(e)
            UpdateScroll()
        End Sub

        Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
            If _vScrollBar.Visible Then
                Dim newValue = _vScrollBar.Value - (e.Delta \ 120) * 3
                newValue = Math.Max(0, Math.Min(newValue, _vScrollBar.Maximum - _vScrollBar.LargeChange + 1))
                _vScrollBar.Value = newValue
                Me.Invalidate()
            End If
            MyBase.OnMouseWheel(e)
        End Sub

        Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
            MyBase.OnMouseClick(e)
            Dim y = e.Y - _headerHeight
            If y < 0 Then Return
            Dim clickedRowIndex = _vScrollBar.Value + (y \ _rowHeight)
            If clickedRowIndex >= 0 AndAlso clickedRowIndex < Me.RowCount Then
                _selectedRowIndex = clickedRowIndex
                Me.Invalidate()
                RaiseEvent CellClick(Me, New DataGridViewCellEventArgs(0, clickedRowIndex))
            End If
        End Sub

        Protected Overrides Sub OnMouseDoubleClick(e As MouseEventArgs)
            MyBase.OnMouseDoubleClick(e)
            Dim y = e.Y - _headerHeight
            If y < 0 Then Return
            Dim clickedRowIndex = _vScrollBar.Value + (y \ _rowHeight)
            If clickedRowIndex >= 0 AndAlso clickedRowIndex < Me.RowCount Then
                RaiseEvent CellDoubleClick(Me, New DataGridViewCellEventArgs(0, clickedRowIndex))
            End If
        End Sub
    End Class

    Public Class CellValueNeededEventArgs
        Inherits EventArgs
        Public Property RowIndex As Integer
        Public Property ColumnIndex As Integer
        Public Property Value As Object
        Public Property TextColor As Color

        Public Sub New(r As Integer, c As Integer)
            RowIndex = r
            ColumnIndex = c
            TextColor = Color.Empty
        End Sub
    End Class
