Imports Framework.DatosNegocio
Imports FN.RiesgosVehiculos.DN

Public Class RestriccionesAMV


#Region "M�todos"

    ''' <summary>
    ''' Se le pasa el modelo y la cilindrada y devuelve byref todas las caracter�sticas
    ''' que dependen de �stos
    ''' </summary>
    ''' <param name="modeloDatos">Fija los valores de modelo, marca y categor�a del riesgo</param>
    ''' <param name="cilindrada">Establece la cilindrada</param>
    ''' <param name="AnosMinEdad">Recibe el m�nimo de edad para las condiciones</param>
    ''' <param name="AnosMinCarnet">Recibe los a�os m�nimos de carnet. Si no requiere carnet el valor ser� -1</param>
    ''' <param name="matriculado">Par�metro que indica si la moto est� matriculada o no</param>
    ''' <param name="tiposCarnet">Recibe los tipos de carnet v�lidos</param>
    ''' <remarks></remarks>
    Public Shared Sub RecuperarRequisitosTarificar(ByVal modeloDatos As ModeloDatosDN, ByVal cilindrada As Integer, ByRef AnosMinEdad As Integer, ByRef AnosMinCarnet As Integer, ByVal matriculado As Boolean, ByRef tiposCarnet As IList(Of TipoCarnet))
        Dim categoria As CategoriaDN

        If modeloDatos Is Nothing Then
            Throw New ApplicationException("El ModeloDatos no puede ser nulo")
        End If

        categoria = modeloDatos.Categoria

        If (categoria.Nombre.ToUpper() <> "QUAD" AndAlso categoria.Nombre.ToUpper() <> "CROSS" AndAlso Not matriculado) _
                OrElse ((categoria.Nombre.ToUpper() = "QUAD" OrElse categoria.Nombre.ToUpper() = "CROSS") AndAlso matriculado) Then
            If Not matriculado Then
                Throw New ApplicationException("El veh�culo debe estar matriculado para poder ser asegurado")
            Else
                Throw New ApplicationException("La categor�a no es consistente con el estado de matriculaci�n")
            End If
        End If

        'SPORT -> edad m�nima 23; minCarnet 1
        'Resto -> 18 a�os
        AnosMinEdad = 18
        AnosMinCarnet = 0
        If categoria.Nombre.ToUpper() = "SPORT" Then
            AnosMinEdad = 23
            AnosMinCarnet = 1
        End If

        'Admite no matriculacion -> Quad y Cross
        If Not matriculado Then
            AnosMinCarnet = -1
        End If

        'Tipos Carnet -> cilindrada <= 125 A y B; Resto: A
        tiposCarnet = New List(Of TipoCarnet)
        If categoria.Nombre.ToUpper() <> "QUAD" AndAlso categoria.Nombre.ToUpper() <> "QUAD MATRICULADO" Then
            tiposCarnet.Add(TipoCarnet.A)
        End If

        If categoria.Nombre.ToUpper() <> "SPORT" AndAlso cilindrada <= 125 Then
            tiposCarnet.Add(TipoCarnet.B)
        End If

    End Sub

    Public Shared Function AdmiteMCND(ByVal modeloDatos As ModeloDatosDN, ByVal cilindrada As String, ByVal edad As Integer, ByVal anyosCarnet As Integer, ByVal matriculada As Boolean, ByVal tipoCarnet As TipoCarnet) As Boolean
        Dim categoria As CategoriaDN

        If modeloDatos Is Nothing Then
            Throw New ApplicationException("El ModeloDatos no puede ser nulo")
        End If

        categoria = modeloDatos.Categoria

        'Cilindrada < 125
        If cilindrada > 125 Then
            Return False
        End If

        'Cualquier categoria excepto Cross, Quad, Trial y Enduro
        If categoria.Nombre.ToUpper() = "QUAD" OrElse categoria.Nombre.ToUpper() = "QUAD MATRICULADO" _
                OrElse categoria.Nombre.ToUpper() = "ENDURO" OrElse categoria.Nombre.ToUpper() = "CROSS" Then
            Return False
        End If

        Return True

    End Function

    ''' <summary>
    ''' Establece la condici�n de a�os de carnet m�nimo a partir de la cilindrada y
    ''' un tipo de carnet de conducir espec�fico. Si no hay nada especificado, devuelve -1
    ''' </summary>
    Public Shared Function A�osCarnetMinimoXCCCyCarnet(ByVal tipocarnet As TipoCarnet) As Integer
        If tipocarnet = FN.RiesgosVehiculos.DN.TipoCarnet.B Then
            Return 3
        End If
        Return -1
    End Function

#End Region


End Class