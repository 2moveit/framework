Public Interface IControlPBase

#Region "propiedades"
    'las propiedades que definen el formato y comportamiento est�tico
    Property PropiedadesControl() As PropiedadesControles.PropiedadesControlP
    'el mensaje de error de validaci�n que muestre el control
    Property MensajeError() As String
    'el texto que aparecer� en el tooltip
    Property ToolTipText() As String
    ReadOnly Property FormularioPadre() As Form
#End Region

End Interface