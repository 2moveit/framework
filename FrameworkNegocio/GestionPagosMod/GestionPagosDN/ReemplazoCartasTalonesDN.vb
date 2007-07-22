

<Serializable()> Public Class ReemplazosTextoCartasDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"

    Protected mTextoOriginal As String
    Protected mTextoReemplazo As CampoDestino

#End Region

#Region "Constructores"
    'Public Sub New()

    'End Sub
#End Region

#Region "Propiedades"

    Public Property TextoOriginal() As String
        Get
            Return Me.mTextoOriginal
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(value, Me.mTextoOriginal)
        End Set
    End Property

    Public Property TextoReemplazo() As CampoDestino
        Get
            Return Me.mTextoReemplazo
        End Get
        Set(ByVal value As CampoDestino)
            Me.CambiarValorVal(value, Me.mTextoReemplazo)
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarTextoOriginal(ByRef mensaje As String, ByVal textoOriginal As String) As Boolean
        If String.IsNullOrEmpty(textoOriginal) Then
            mensaje = "El texto origina no puede ser nulo"
            Return False
        End If

        Return True
    End Function

    Private Function ValidarTextoReemplazo(ByRef mensaje As String, ByVal textoReemplazo As CampoDestino) As Boolean
        If Not CampoDestino.IsDefined(GetType(CampoDestino), textoReemplazo) Then
            mensaje = "El campo de destino no es v�lido"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "M�todos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarTextoOriginal(pMensaje, mTextoOriginal) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarTextoReemplazo(pMensaje, mTextoReemplazo) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function


    ''' <summary>
    ''' Reemplaza las coincidencias que haya en el texto del Talon
    ''' a partir de los datos que haya en el Talon y en el Pago.
    ''' OJO: no reemplaza la fecha de impresi�n, porque esa s�lo se puede reemplazar
    ''' en el momento de imprimir
    ''' </summary>
    ''' <param name="pTalon">El TalonDoc sobre el que se va a producir
    ''' la operaci�n de reemplazo</param>
    Public Sub ReemplazarTexto(ByRef pTalon As TalonDN)
        If pTalon Is Nothing Then
            Throw New ApplicationException("El Tal�n Documento que se intenta reemplazar est� vac�o")
        ElseIf pTalon.HuellaRTF Is Nothing Then
            Throw New ApplicationException("La huella del Contenedor RTF del Tal�n que se intenta reemplazar est� vac�a")
        ElseIf pTalon.HuellaRTF.EntidadReferida Is Nothing Then
            Throw New ApplicationException("No se ha cargado la huella del Contenedor RTF del Tal�n que se intenta reemplazar")
        End If

        Dim contenedor As ContenedorRTFDN = pTalon.HuellaRTF.EntidadReferida


        If contenedor.RTF.Contains(Me.mTextoOriginal) Then
            Select Case Me.mTextoReemplazo
                Case CampoDestino.Destinatario
                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalon.Destinatario.DenominacionFiscal)

                Case CampoDestino.Importe
                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, AuxIU.FormateadorMonedaEurosConSimbolo.FormatearRapido(pTalon.ImportePago))

                Case CampoDestino.Origen
                    If pTalon.Pago.Origen Is Nothing Then
                        Throw New ApplicationException("El Origen que se intenta reemplazar est� vac�o")
                    End If

                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalon.Pago.Origen.ToString)

                Case CampoDestino.Direccion_Completa
                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalon.DireccionEnvio.ToString)

                Case CampoDestino.Direccion_Calle
                    With pTalon.DireccionEnvio
                        Try
                            contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, String.Join(" ", New String() {.TipoVia.Nombre, .Via}))
                        Catch ex As Exception
                            contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, String.Join(" ", New String() {String.Empty, .Via}))
                        End Try
                    End With

                Case CampoDestino.Direccion_CP
                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalon.DireccionEnvio.CodPostal)

                Case CampoDestino.Direccion_Localidad
                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalon.DireccionEnvio.Localidad.Nombre)

                Case CampoDestino.Direccion_Provincia
                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalon.DireccionEnvio.Localidad.Provincia.Nombre)

            End Select
        End If
    End Sub

    ''' <summary>
    ''' Reemplaza las coincidencias que haya en el texto del TalonDoc
    ''' a partir de los datos que haya en el TalonDoc y en el Pago.
    ''' �ste realiza un reemplazo completo de todos los campos que no hubiesen sido reemplazados
    ''' por el Talon
    ''' </summary>
    ''' <param name="pTalonDocumento">El TalonDoc sobre el que se va a producir
    ''' la operaci�n de reemplazo</param>
    Public Sub ReemplazarTexto(ByRef pTalonDocumento As TalonDocumentoDN)
        If pTalonDocumento Is Nothing Then
            Throw New ApplicationException("El Tal�n Documento que se intenta reemplazar est� vac�o")
        ElseIf pTalonDocumento.HuellaRTF Is Nothing Then
            Throw New ApplicationException("El Tal�n Documento contiene una Huella RTF vac�a")
        ElseIf pTalonDocumento.HuellaRTF.EntidadReferida Is Nothing Then
            Throw New ApplicationException("La huella del Contenedor RTF del Tal�n Dcumento est� vac�a")
        ElseIf pTalonDocumento.Talon Is Nothing Then
            Throw New ApplicationException("El Tal�n que contiene el Tal�n Documento que se intenta reemplazar est� vac�o")
        End If

        Dim contenedor As ContenedorRTFDN = pTalonDocumento.HuellaRTF.EntidadReferida

        If contenedor.RTF.Contains(Me.mTextoOriginal) Then
            Select Case Me.mTextoReemplazo
                Case CampoDestino.Destinatario
                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalonDocumento.Destinatario)

                Case CampoDestino.Fecha_Corta
                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalonDocumento.FechaTalon.ToShortDateString)

                Case CampoDestino.Fecha_Larga
                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalonDocumento.FechaTalon.ToLongDateString)

                Case CampoDestino.Importe
                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, AuxIU.FormateadorMonedaEurosConSimbolo.FormatearRapido(pTalonDocumento.Importe))

                Case CampoDestino.Origen
                    If pTalonDocumento.Talon.Pago.Origen Is Nothing Then
                        Throw New ApplicationException("ElOrigen del Pago del Tal�n de Negocio del Tal�n Documento que se intenta reemplazar est� vac�o")
                    End If

                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalonDocumento.Talon.Pago.Origen.ToString)

                Case CampoDestino.Numero_Serie_Talon
                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalonDocumento.NumeroSerie)

                Case CampoDestino.Direccion_Completa
                    Try
                        contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalonDocumento.Talon.DireccionEnvio.ToString)
                    Catch ex As Exception
                        Throw New ApplicationException("La direcci�n de env�o no es correcta")
                    End Try

                Case CampoDestino.Direccion_Calle
                    With pTalonDocumento.Talon.DireccionEnvio
                        contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, String.Join(" ", New String() {.TipoVia.Nombre, .Via}))
                    End With

                Case CampoDestino.Direccion_CP
                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalonDocumento.Talon.DireccionEnvio.CodPostal)

                Case CampoDestino.Direccion_Localidad
                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalonDocumento.Talon.DireccionEnvio.Localidad.Nombre)

                Case CampoDestino.Direccion_Provincia
                    contenedor.RTF = contenedor.RTF.Replace(Me.mTextoOriginal, pTalonDocumento.Talon.DireccionEnvio.Localidad.Provincia.Nombre)

            End Select
        End If
    End Sub

    Public Overrides Function ToString() As String
        Return [Enum].GetName(GetType(CampoDestino), Me.mTextoReemplazo) & " (" & Me.mTextoOriginal & ")"
    End Function
#End Region

End Class
