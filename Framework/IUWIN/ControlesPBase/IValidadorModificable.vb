Public Interface IValidadorModificable
    'este interface hereda de ivalidador, pero permite que le establezcamos
    'la propiedad validador, como si fu�semos personas normales
    'pq los controles necesitan tener un constructor sin par�metros
    'para poder hacer un initalize correctamente


    Property Validador() As Framework.DatosNegocio.IValidador
    Property MensajeErrorValidacion() As String


End Interface