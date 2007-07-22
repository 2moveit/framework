Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica

Imports Framework.Usuarios.DN
Imports Framework.Ficheros.FicherosDN


Public Class RutaAlmacenamientoFicheroFS
    Inherits BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region

#Region "M�todos"

    Public Function RecuperarListadoRutas(ByVal actor As PrincipalDN, ByVal idSesion As String) As IList(Of RutaAlmacenamientoFicherosDN)
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1� guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2� verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3� creacion de la ln y ejecucion del metodo

            Dim miLN As Framework.Ficheros.FicherosLN.RutaAlmacenamientoFicherosLN
            miLN = New Framework.Ficheros.FicherosLN.RutaAlmacenamientoFicherosLN(mTL, mRec)
            RecuperarListadoRutas = miLN.RecuperarListadoRutas()
            '-----------------------------------------------------------------------------

            '4� guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try

    End Function

    Public Function GuardarRutaAlmacenamientoF(ByVal rutaAlmacenamiento As RutaAlmacenamientoFicherosDN, ByVal actor As PrincipalDN, ByVal idSesion As String) As RutaAlmacenamientoFicherosDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1� guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2� verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3� creacion de la ln y ejecucion del metodo

            Dim miLN As Framework.Ficheros.FicherosLN.RutaAlmacenamientoFicherosLN
            miLN = New Framework.Ficheros.FicherosLN.RutaAlmacenamientoFicherosLN(mTL, mRec)
            GuardarRutaAlmacenamientoF = miLN.Guardar(rutaAlmacenamiento)
            '-----------------------------------------------------------------------------

            '4� guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try

    End Function

    Public Function CerrarRaf(ByVal pRaf As RutaAlmacenamientoFicherosDN, ByVal actor As PrincipalDN, ByVal idSesion As String) As RutaAlmacenamientoFicherosDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1� guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2� verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3� creacion de la ln y ejecucion del metodo

            Dim miLN As Framework.Ficheros.FicherosLN.RutaAlmacenamientoFicherosLN
            miLN = New Framework.Ficheros.FicherosLN.RutaAlmacenamientoFicherosLN(mTL, mRec)
            Return miLN.CerrarRaf(pRaf)
            '-----------------------------------------------------------------------------

            '4� guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try
    End Function

#End Region

End Class
