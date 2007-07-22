Public Class ctrlCuestionarioTarificacion
    Inherits MotorIU.ControlesP.ControladorControlBase

    Public Sub New(ByVal Navegador As MotorIU.Motor.INavegador, ByVal sender As MotorIU.ControlesP.IControlP)
        MyBase.New(Navegador, sender)
    End Sub

    Public Function ObtenerMarcas() As IList
        Dim mias As New Framework.AS.DatosBasicosAS()
        Return mias.RecuperarListaTipos(GetType(FN.RiesgosVehiculos.DN.MarcaDN))
    End Function

    Public Function ObtenerSexos() As IList
        Dim mias As New Framework.AS.DatosBasicosAS()
        Return mias.RecuperarListaTipos(GetType(FN.Personas.DN.TipoSexo))
    End Function

    Public Function ObtenerTomador(ByVal pID As String) As FN.Seguros.Polizas.DN.TomadorDN
        Dim mias As New Framework.AS.DatosBasicosAS()
        Return mias.RecuperarGenerico(pID, GetType(FN.Seguros.Polizas.DN.TomadorDN))
    End Function

    Public Function ObtenerCaracteristicas() As IList
        Dim mias As New Framework.AS.DatosBasicosAS()
        Return mias.RecuperarListaTipos(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN))
    End Function

    Public Function ObtenerModelosPorMarca(ByVal pMarca As FN.RiesgosVehiculos.DN.MarcaDN) As List(Of FN.RiesgosVehiculos.DN.ModeloDN)
        Dim mias As New FN.RiesgosVehiculos.AS.RiesgosVeh�culosAS
        Return mias.RecuperarModelosPorMarca(pMarca)
    End Function

    Public Function ObtenerLocalidades() As FN.Localizaciones.DN.ColLocalidadDN
        Dim mias As New Framework.AS.DatosBasicosAS
        Dim micol As New FN.Localizaciones.DN.ColLocalidadDN()
        Dim lista As IList = mias.RecuperarListaTipos(GetType(FN.Localizaciones.DN.LocalidadDN))
        For Each loc As FN.Localizaciones.DN.LocalidadDN In lista
            micol.Add(loc)
        Next
        Return micol
    End Function

    Public Function ObtenerLocalidadPorCodigoPostal(ByVal pCodigoPostal As String) As FN.Localizaciones.DN.ColLocalidadDN
        Dim mias As New FN.Localizaciones.AS.LocalizacionesAS()
        Return mias.RecuperarLocalidadPorCodigoPostal(pCodigoPostal)
    End Function

    ''' <summary>
    '''Determina si un modelo es asegurable sin estar matriculado
    ''' </summary>
    ''' <param name="pModelo"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function AsegurableNoMatriculado(ByVal pModelo As FN.RiesgosVehiculos.DN.ModeloDN, ByVal fecha As Date) As Boolean
        Dim mias As New FN.RiesgosVehiculos.AS.RiesgosVeh�culosAS()
        'si no existe un modelo de datos para ese modelo-marca-sin matr�cula, es que
        'no se puede asegurar sin matricular
        Return mias.ExisteModeloDatos(pModelo.Nombre, pModelo.Marca.Nombre, False, fecha)
    End Function

    ''' <summary>
    ''' Obtiene los requisitos permitidos para tarificar, pasando como par�metros los datos del riesgo modelo, la cilindrada y si est� matriculado.
    ''' El resto de par�metros son ByRef para obtener los requisitos de tarificaci�n
    ''' </summary>
    ''' <param name="modelo"></param>
    ''' <param name="cilindrada"></param>
    ''' <param name="fecha"></param>
    ''' <param name="AnosMinEdad"></param>
    ''' <param name="AnosMinCarnet"></param>
    ''' <param name="matriculado"></param>
    ''' <param name="tiposCarnetNecesarios"></param>
    ''' <remarks></remarks>
    Public Sub RecuperarRequisitosTarificar(ByVal modelo As FN.RiesgosVehiculos.DN.ModeloDN, ByVal cilindrada As Integer, ByVal fecha As Date, ByVal matriculado As Boolean, ByRef AnosMinEdad As Integer, ByRef AnosMinCarnet As Integer, ByRef admiteNoMatric As Boolean, ByRef tiposCarnetNecesarios As IList(Of FN.RiesgosVehiculos.DN.TipoCarnet))
        Dim modeloDatos As FN.RiesgosVehiculos.DN.ModeloDatosDN
        Dim miAS As New FN.RiesgosVehiculos.AS.RiesgosVeh�culosAS()

        'Se comprueba si el modelo admite no matriculaci�n
        admiteNoMatric = True
        modeloDatos = miAS.RecuperarModeloDatos(modelo.Nombre, modelo.Marca.Nombre, False, fecha)

        If modeloDatos Is Nothing Then
            admiteNoMatric = False
            modeloDatos = miAS.RecuperarModeloDatos(modelo.Nombre, modelo.Marca.Nombre, True, fecha)
        End If

        GSAMV.DN.RestriccionesAMV.RecuperarRequisitosTarificar(modeloDatos, cilindrada, AnosMinEdad, AnosMinCarnet, matriculado, tiposCarnetNecesarios)
    End Sub

    Public Function AdmiteMulticonductor(ByVal modelo As FN.RiesgosVehiculos.DN.ModeloDN, ByVal cilindrada As Integer, ByVal fecha As Date, ByVal edad As Integer, ByVal anyosCarnet As Integer, ByVal matriculada As Boolean, ByVal tipoCarnet As FN.RiesgosVehiculos.DN.TipoCarnet) As Boolean
        Dim modeloDatos As FN.RiesgosVehiculos.DN.ModeloDatosDN
        Dim miAS As New FN.RiesgosVehiculos.AS.RiesgosVeh�culosAS()
        modeloDatos = miAS.RecuperarModeloDatos(modelo.Nombre, modelo.Marca.Nombre, matriculada, fecha)

        Return GSAMV.DN.RestriccionesAMV.AdmiteMCND(modeloDatos, cilindrada, edad, anyosCarnet, matriculada, tipoCarnet)
    End Function

    Public Function A�osCarnetMinimoXCCCyCarnet(ByVal tipocarnet As FN.RiesgosVehiculos.DN.TipoCarnet) As Integer
        Return GSAMV.DN.RestriccionesAMV.A�osCarnetMinimoXCCCyCarnet(tipocarnet)
    End Function

End Class
