Imports Framework.FachadaLogica
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Usuarios.DN
Imports FN.Empresas.DN

Public Class EmpresaFS
    Inherits BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region

#Region "M�todos"

    Public Function GuardarEmpleadoYPuestoR(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal empPR As EmpleadoYPuestosRDN) As EmpleadoYPuestosRDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1� guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 � comprobaci�n permisos  
                pActor.Autorizado()

                '3� creaci�n de la ln y ejecucion del m�todo
                Dim miln As New FN.Empresas.LN.EmpleadosLN

                GuardarEmpleadoYPuestoR = miln.GuardarEmpleadoYPuestoR(empPR)

                '4� guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function

    Public Function RecuperarSedePrincipalxCIFEmpresa(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal cifNifEmpresa As String) As SedeEmpresaDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1� guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 � comprobaci�n permisos  
                pActor.Autorizado()

                '3� creaci�n de la ln y ejecucion del m�todo
                Dim miln As New FN.Empresas.LN.EmpresaLN()

                RecuperarSedePrincipalxCIFEmpresa = miln.RecuperarSedePrincipalxCIFEmpresa(cifNifEmpresa)

                '4� guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function

#End Region

End Class
