#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Colecci�n de zonas para el �rbol
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public Class ColZonasDN
    Inherits ArrayListValidable(Of ZonaDN)

    'M�todos de la colecci�n
    '

End Class


<Serializable()> _
Public Class ColZonasALVDN
    Inherits ArrayListValidable


    Public Sub New()
        MyBase.New(New ValidadorTipos(GetType(ZonaDN), True))
    End Sub

    'M�todos de la colecci�n
    '

End Class
