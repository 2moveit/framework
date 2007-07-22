Public Class PaqueteImpresion
    Inherits MotorIU.PaqueteIU

    ''' <summary>
    ''' El tal�n documento que se quiere imprimir
    ''' </summary>
    Public TalonDocumento As FN.GestionPagos.DN.TalonDocumentoDN
    ''' <summary>
    ''' La configuraci�n de impresi�n que se quiere aplicar
    ''' </summary>
    Public ConfiguracionImpresion As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN
    ''' <summary>
    ''' Indica como valor de retorno si se ha impreso o se ha cancelado la impresi�n
    ''' </summary>
    Public Impreso As Boolean
    ''' <summary>
    ''' Indica si el formulario de impresi�n debe mostrarse o no al usuario
    ''' </summary>
    Public ImpresionSilenciosa As Boolean
    ''' <summary>
    ''' El mensaje de error del servidor al intentar realizar la operaci�n "impresi�n"
    ''' </summary>
    ''' <remarks></remarks>
    Public MensajeError As String
    ''' <summary>
    ''' La configuraci�n de impresora que usamos en el caso de que sea autom�tico
    ''' </summary>
    ''' <remarks></remarks>
    Public PrinterSettings As System.Drawing.Printing.PrinterSettings
    ''' <summary>
    ''' Si se trata de una impresi�n de prueba
    ''' </summary>
    Public Prueba As Boolean = False



End Class
