Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica

Imports Framework.Usuarios.DN
Imports Framework.Ficheros.FicherosDN


Public Class FicherosFS
    Inherits BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region

    Public Function ObtenerCajonDocumentosRelacionados(ByVal pGUID As String, ByVal actor As PrincipalDN, ByVal idSesion As String) As ColCajonDocumentoDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New Framework.LogicaNegocios.Transacciones.CajonHiloLN(mRec)

            Try

                '1� guardar log de inicio
                mfh.EntradaMetodo(idSesion, actor, mRec)

                '2� verificacion de permisos por rol de usuario
                actor.Autorizado()

                '-----------------------------------------------------------------------------
                '3� creacion de la ln y ejecucion del metodo

                Dim miLN As New Framework.Ficheros.FicherosLN.CajonDocumentoLN()
                ObtenerCajonDocumentosRelacionados = miLN.RecuperarCajonesParaEntidadReferida(pGUID)
                '-----------------------------------------------------------------------------

                '4� guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, actor, mRec)

            Catch ex As Exception

                mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
                Throw

            End Try
        End Using

    End Function


    Public Function RecuperarColTipoFichero(ByVal actor As PrincipalDN, ByVal idSesion As String) As ColTipoFicheroDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New Framework.LogicaNegocios.Transacciones.CajonHiloLN(mRec)

            Try

                '1� guardar log de inicio
                mfh.EntradaMetodo(idSesion, actor, mRec)

                '2� verificacion de permisos por rol de usuario
                actor.Autorizado()

                '-----------------------------------------------------------------------------
                '3� creacion de la ln y ejecucion del metodo

                Dim miLN As New Framework.Ficheros.FicherosLN.CajonDocumentoLN()
                RecuperarColTipoFichero = miLN.RecuperarColTipoFichero
                '-----------------------------------------------------------------------------

                '4� guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, actor, mRec)

            Catch ex As Exception

                mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
                Throw

            End Try
        End Using

    End Function

    Public Function VincularCajonDocumento(ByVal actor As PrincipalDN, ByVal idSesion As String) As DataSet
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New Framework.LogicaNegocios.Transacciones.CajonHiloLN(mRec)

            Try

                '1� guardar log de inicio
                mfh.EntradaMetodo(idSesion, actor, mRec)

                '2� verificacion de permisos por rol de usuario
                actor.Autorizado()

                '-----------------------------------------------------------------------------
                '3� creacion de la ln y ejecucion del metodo

                Dim colc, coli As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN
                Dim miLN As New Framework.Ficheros.FicherosLN.CajonDocumentoLN
                VincularCajonDocumento = miLN.VincularCajonDocumento(colc, coli)
                '-----------------------------------------------------------------------------



                '4� guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, actor, mRec)

            Catch ex As Exception

                mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
                Throw

            End Try
        End Using

    End Function



End Class
