using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Collections;
using System.Xml.XPath;

namespace Framework.Configuracion
{
    /// <summary>
    /// Clase para leer archivos de configuraci�n XML asociados a un ensamblado y que no se encuentran
    /// en el config de la aplicaci�n principal
    /// </summary>
    public static class LectorConfiguracionXML
    {
        /// <summary>
        /// Devuelve un hashtable de string/string con los valores 
        /// que se encuentran dentro de configuraci�n.
        /// </summary>
        /// <param name="NombreArchivo">El nombre del archivo XML de configuraci�n.</param>
        /// <returns>Un dccionario((string,string) con los valores de configuraci�n en formato clave/valor.</returns>
        public static Dictionary<string, string> LeerConfiguracion(string NombreArchivo)
        {
            //comprobamos que existe el archivo de configuraci�n en la ruta base
            if (!System.IO.File.Exists(NombreArchivo))
            {
                //no se encuentra el archivo en el directorio por defecto, quiz� oprque se est�
                //ejecutando en un test o alg�n otro tipo de ejecutable que distorsiona el path
                NombreArchivo = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\" + NombreArchivo;
                if (!System.IO.File.Exists(NombreArchivo))
                {
                    throw new System.IO.FileNotFoundException("No se ha encontrado el archivo de configuraci�n " + NombreArchivo);
                }
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(NombreArchivo);

            //creamos el namespace para ejecutar consultas XPath sobre el documento Datasource
            NameTable ntDS = new NameTable();
            XmlNamespaceManager nsManagerDS = new XmlNamespaceManager(ntDS);
            string NameSpaceDS = doc.DocumentElement.NamespaceURI;
            nsManagerDS.AddNamespace("s", NameSpaceDS);

            XmlNodeList propiedades = doc.SelectNodes(@"//s:propiedad", nsManagerDS);
            Dictionary<string, string> ht = new Dictionary<string, string>();
            foreach (XmlNode nodo in propiedades)
            {
                string key = nodo.Attributes.GetNamedItem("nombre").Value;
                string valor = nodo.Attributes.GetNamedItem("valor").Value;
                ht.Add(key, valor);
            }

            return ht;
        }
    }
}
