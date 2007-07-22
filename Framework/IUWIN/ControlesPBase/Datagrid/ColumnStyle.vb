Imports System
Imports System.Drawing
Imports System.Windows.Forms

Namespace Datagrid.Estilo

    Public Enum ControlVertAlignment
        Top
        Centrado
        Bottom
    End Enum

    Public Enum ControlHorizAlignment
        Izquierda
        Centrado
        Derecha
    End Enum




    Public Class ColumnStyle
        Inherits DataGridColumnStyle


#Region "campos"
        Private mEstiloColumna As EstiloColumnaDatagrid
        Private mControlSize As Size
        Private mControlVertAlignment As ControlVertAlignment
        Private mControlHorizAlignment As ControlHorizAlignment
#End Region

#Region "constructor"

        Public Sub New()
            mEstiloColumna = New EstiloColumnaDatagrid(0)
            mControlSize = New Size(Me.Width, 25)
            mControlHorizAlignment = ControlHorizAlignment.Centrado
            mControlVertAlignment = ControlVertAlignment.Centrado
        End Sub

#End Region

#Region "propiedades"

        'obtiene y establece el n� de columna que vamos a considerar ID
        Public ReadOnly Property ColumnaID() As Integer
            Get
                If TypeOf Me.DataGridTableStyle.DataGrid Is DataGridP Then
                    Return CType(Me.DataGridTableStyle.DataGrid, DataGridP).ColumnaID
                Else
                    Return 0
                End If
            End Get
        End Property

        'obtiene el objeto EstiloColumnaDatagrid que representa el estilo
        'de rejilla que existe alrededor de los controles que se muestran
        'en las celdas del datagrid
        Public ReadOnly Property EstiloColumna() As EstiloColumnaDatagrid
            Get
                Return mEstiloColumna
            End Get
        End Property

        'obtiene el ancho m�nimo
        Public Overridable ReadOnly Property MinimunWidth() As Integer
            Get
                Return Me.GetPreferredSize(Nothing, Nothing).Width
            End Get
        End Property

        'obtiene la altura m�nima
        Public Overridable ReadOnly Property MinimunHeight() As Integer
            Get
                Return Me.GetMinimumHeight
            End Get
        End Property

        'obtiene y establece el tama�o predefinido del control (el tama�o del control
        'cuando se dibuja en las celdas del datagrid)
        Public Overridable Property ControlSize() As Size
            Get
                Return mControlSize
            End Get
            Set(ByVal Value As Size)
                mControlSize = Value
            End Set
        End Property

        'obtiene y establece la alineaci�n horizontal del control en la celda
        Public Overridable Property ControlHorizAlignment() As ControlHorizAlignment
            Get
                Return mControlHorizAlignment
            End Get
            Set(ByVal Value As ControlHorizAlignment)
                mControlHorizAlignment = Value
            End Set
        End Property

        'obtiene y establece la alineaci�n vertical del control en la celda
        Public Overridable Property ControlVertAlignment() As ControlVertAlignment
            Get
                Return mControlVertAlignment
            End Get
            Set(ByVal Value As ControlVertAlignment)
                mControlVertAlignment = Value
            End Set
        End Property

#End Region


#Region "m�todos"

        'calcula las bounds del control, teniendo en cuenta la laineaci�n y el estilocolumna
        Protected Overridable Function GetControlBounds(ByVal pCellBounds As Rectangle) As Rectangle
            Dim controlbounds As Rectangle

            Try
                controlbounds = New Rectangle(pCellBounds.X + Me.EstiloColumna.Izquierda, pCellBounds.Y + Me.EstiloColumna.Top, Me.ControlSize.Width, Me.ControlSize.Height)

                Select Case mControlVertAlignment
                    Case ControlVertAlignment.Centrado
                        controlbounds.Y = pCellBounds.Top + (pCellBounds.Height - Me.ControlSize.Height) / 2
                    Case ControlVertAlignment.Bottom
                        controlbounds.Y = pCellBounds.Top + (pCellBounds.Height - (Me.EstiloColumna.Bottom + Me.ControlSize.Height))
                End Select

                Select Case mControlHorizAlignment
                    Case ControlHorizAlignment.Centrado
                        controlbounds.X = pCellBounds.Left + (pCellBounds.Width - Me.ControlSize.Width) / 2
                    Case ControlHorizAlignment.Derecha
                        controlbounds.X = pCellBounds.Left + (pCellBounds.Width - (Me.EstiloColumna.Derecha + Me.ControlSize.Width))
                End Select

                Return controlbounds

            Catch ex As Exception
                Throw ex
            End Try
        End Function


        'Al heredar o implementar la clase abstracta DataGridColumnStyle, tenemos que
        'sobreescribir todos estos m�todos. los m�todos Paint los usamos para redibujar
        'los contenidos de las celdas, por lo que las subclases que hereden de esta
        'deber�n sobrescribirlos a su vez.

        'no hacemos nada
        Protected Overrides Sub Abort(ByVal rowNum As Integer)

        End Sub

        'devolvemos que s� para ocmpletar un proceso de edici�n
        Protected Overrides Function Commit(ByVal dataSource As System.Windows.Forms.CurrencyManager, ByVal rowNum As Integer) As Boolean
            Return True
        End Function

        'prepara la celda para editar
        'no hacemos nada
        Protected Overloads Overrides Sub Edit(ByVal source As System.Windows.Forms.CurrencyManager, ByVal rowNum As Integer, ByVal bounds As System.Drawing.Rectangle, ByVal [readOnly] As Boolean, ByVal instantText As String, ByVal cellIsVisible As Boolean)

        End Sub

        'obtiene la altura m�nima de la celda
        Protected Overrides Function GetMinimumHeight() As Integer
            Return Me.GetPreferredHeight(Nothing, Nothing)
        End Function

        'devuelve la altura predeterminada de la celda
        Protected Overrides Function GetPreferredHeight(ByVal g As System.Drawing.Graphics, ByVal value As Object) As Integer
            Return Me.ControlSize.Height + Me.EstiloColumna.Top + Me.EstiloColumna.Bottom
        End Function

        'devuelve la altura predeterminada de la celda del datagridcolumn
        Protected Overrides Function GetPreferredSize(ByVal g As System.Drawing.Graphics, ByVal value As Object) As System.Drawing.Size
            Dim width As Integer
            Dim height As Integer

            Try
                width = Me.ControlSize.Width + Me.EstiloColumna.Izquierda + Me.EstiloColumna.Derecha
                height = Me.ControlSize.Height + Me.EstiloColumna.Top + Me.EstiloColumna.Bottom

                Return New Size(width, height)
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        'm�todos paint
        'no hacemos nada (se encarga la clase derivada)
        Protected Overloads Overrides Sub Paint(ByVal g As System.Drawing.Graphics, ByVal bounds As System.Drawing.Rectangle, ByVal source As System.Windows.Forms.CurrencyManager, ByVal rowNum As Integer)

        End Sub
        'no hacemos nada (se encarga la clase derivada)
        Protected Overloads Overrides Sub Paint(ByVal g As System.Drawing.Graphics, ByVal bounds As System.Drawing.Rectangle, ByVal source As System.Windows.Forms.CurrencyManager, ByVal rowNum As Integer, ByVal alignToRight As Boolean)

        End Sub
#End Region

    End Class

End Namespace


