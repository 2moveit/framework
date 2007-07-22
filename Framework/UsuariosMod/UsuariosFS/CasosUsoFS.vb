Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica

Imports Framework.Usuarios.DN
Imports Framework.Usuarios.LN

Public Class CasosUsoFS
    Inherits Framework.FachadaLogica.BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region

#Region "M�todos"

    Public Function RecuperarListaCasosUso(ByVal actor As PrincipalDN, ByVal idSesion As String) As IList(Of CasosUsoDN)
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try
            '1� guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2� verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3� creacion de la ln y ejecucion del metodo
            Dim miLN As CasosUsoLN
            miLN = New CasosUsoLN(mTL, mRec)
            RecuperarListaCasosUso = miLN.RecuperarListaCasosUso()
            '-----------------------------------------------------------------------------

            '4� guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try

    End Function

    Public Function GuardarCasoUso(ByVal casoUso As CasosUsoDN, ByVal actor As PrincipalDN, ByVal idSesion As String) As CasosUsoDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try
            '1� guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2� verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3� creacion de la ln y ejecucion del metodo

            Dim miLN As CasosUsoLN
            miLN = New CasosUsoLN(mTL, mRec)
            GuardarCasoUso = miLN.GuardarCasoUso(casoUso)
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
