#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Esta interfaz es implementada por una entidad que es la responsable de entidades, que podr�n ser
''' tanto colecciones de empresas (agrupaci�n de empresas) como colecci�n de personal
''' </summary>
''' <remarks></remarks>
Public Interface IResponsableDN

#Region "Propiedades"
    Property EntidadResponsableDN() As IEntidadDN
    'Property ColEntidadesACargoDN() As ArrayListValidable(Of IEntidadDN)

    'Devuelve un clon de la colecci�n de entidades a cargo, por lo que no se podr�n a�adir o eliminar elementos a la lista
    ReadOnly Property ClonColEntidadesACargoDN() As IList(Of IEntidadDN)
#End Region

#Region "M�todos"
    Function ValidarDatosResponsable(ByRef mensaje As String, ByVal pResponsable As IEntidadDN) As Boolean
    Function ValidarEntidadesACargo(ByRef mensaje As String, ByVal pColEntidadesACargoDN As IList(Of IEntidadDN)) As Boolean
#End Region

End Interface
