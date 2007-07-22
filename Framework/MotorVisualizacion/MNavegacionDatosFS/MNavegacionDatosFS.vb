Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica
Imports Framework.Usuarios.DN
Imports MNavegacionDatosDN
Public Class MNavegacionDatosFS


#Region "Constructores"

    'Public Sub New(ByVal tl As ITransaccionLogicaLN, ByVal rec As IRecursoLN)
    '    MyBase.New(tl, rec)
    'End Sub

#End Region





    Public Function RecuperarEntidadNavDN(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pTipo As System.Type) As EntidadNavDN
        Using tr As New Transaccion

            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
            Try

                '1� guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, Recurso.Actual)

                '2� verificacion de permisos por rol de usuario
                ' pActor.Autorizado()

                '-----------------------------------------------------------------------------
                '3� creacion de la ln y ejecucion del metodo
                Dim mln As MNavegacionDatosLN.MNavDatosLN

                mln = New MNavegacionDatosLN.MNavDatosLN(Transaccion.Actual, Recurso.Actual)
                RecuperarEntidadNavDN = mln.RecuperarEntidadNavDN(pTipo)
                '-----------------------------------------------------------------------------

                tr.Confirmar()

                '4� guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, Recurso.Actual)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", Recurso.Actual)
                Throw

            End Try



        End Using

    End Function



    Public Function RecuperarRelaciones(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pTipo As System.Type) As ColRelacionEntidadesNavDN







        Using tr As New Transaccion

            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
            Try

                '1� guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, Recurso.Actual)

                '2� verificacion de permisos por rol de usuario
                ' pActor.Autorizado()

                '-----------------------------------------------------------------------------
                '3� creacion de la ln y ejecucion del metodo
                Dim mln As MNavegacionDatosLN.MNavDatosLN

                mln = New MNavegacionDatosLN.MNavDatosLN(Transaccion.Actual, Recurso.Actual)
                RecuperarRelaciones = mln.RecuperarRelaciones(pTipo)
                '-----------------------------------------------------------------------------

                tr.Confirmar()

                '4� guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, Recurso.Actual)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", Recurso.Actual)
                Throw

            End Try



        End Using






    End Function
End Class
