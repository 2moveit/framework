<Serializable()> _
Public Class RedondeoOperadorDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IOperadorDN

    Public Sub New()
        Me.mNombre = "redondeo"
    End Sub

    ''' <summary>
    ''' m�todo que devuelve el resultado de una operaci�n de redondeo
    ''' </summary>
    ''' <param name="valor1">Valor a redondear</param>
    ''' <param name="valor2">N�mero de d�gitos de precisi�n para el redondeo</param>
    ''' <returns>valor resultante de la operaci�n de redondeo del valor1 con los decimales indicados por valor2</returns>
    ''' <remarks></remarks>
    Public Function Ejecutar(ByVal valor1 As Object, ByVal valor2 As Object) As Object Implements IOperadorDN.Ejecutar
        Dim aux1 As Double
        Dim aux2 As Integer

        If Not Double.TryParse(valor1, aux1) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("El valor a truncar debe ser un n�mero entero")
        End If

        If Not Integer.TryParse(valor2, aux2) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("El valor de los decimales a truncar debe ser un n�mero entero")
        End If

        Return Math.Round(aux1, aux2)

    End Function

End Class
