Imports Framework.DatosNegocio

<Serializable()> _
Public Class TipoFicheroDN
    Inherits Framework.DatosNegocio.EntidadDN
    'TODO: Habr�a que mapear esta entidad para que el nombre del tipo fuera �nico en base de datos

    Public Overrides Function ToString() As String
        Return MyBase.Nombre
    End Function
End Class





<Serializable()> _
Public Class ColTipoFicheroDN
    Inherits ArrayListValidable(Of TipoFicheroDN)

End Class





