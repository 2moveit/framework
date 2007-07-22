Public Interface IControlES
    Inherits IControlPBase
    Inherits ivalidadormodificable

#Region "propiedades"
    Property Formateador() As AuxIU.IFormateador 'formaeador
#End Region

#Region "validaci�n"
    'evento de error de validaci�n
    Event ErrorValidacion(ByVal sender As Object, ByVal e As EventArgs)

    Event Validado(ByVal sender As Object, ByVal e As EventArgs)

    Sub ErrorValidando(ByVal mensaje As String)
    'este sub es el que desencadena el error de validaci�n.
    'lo declaramos p�blico para que pueda ser provocado desde
    'fuera

    'funci�n de validaci�n
    Sub OnValidating(ByVal e As System.ComponentModel.CancelEventArgs)

#End Region

End Interface