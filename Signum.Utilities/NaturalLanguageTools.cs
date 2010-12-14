﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Globalization;
using System.Resources;

namespace Signum.Utilities
{
    public static class NaturalLanguageTools
    {
        public static Dictionary<string, IPluralizer> Pluralizers = new Dictionary<string, IPluralizer>
        {
            {"es", new SpanishPluralizer()},
            {"en", new EnglishPluralizer()},
        };

        public static Dictionary<string, IGenderDetector> GenderDetectors = new Dictionary<string, IGenderDetector>
        {
            {"es", new SpanishGenderDetector()},
        };

        public static Gender GetGender(string name)
        {
            IGenderDetector detector = GenderDetectors.TryGetC(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            if (detector != null)
                return detector.GetGender(name);

            return Gender.Neuter;
        }

        public static string Pluralize(string singularName)
        {
            IPluralizer pluralizer = Pluralizers.TryGetC(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            if (pluralizer != null)
                return pluralizer.MakePlural(singularName);

            return singularName;
        }
    }

    public interface IPluralizer
    {
        string MakePlural(string singularName);
    }

    public class EnglishPluralizer : IPluralizer
    {
        //http://www.csse.monash.edu.au/~damian/papers/HTML/Plurals.html
        Dictionary<string, string> exceptions = new Dictionary<string, string>
        {
            {"an", "en"}, // woman -> women 
            {"ch", "ches"}, // church -> churches 
            {"eau", "eaus"},  //chateau -> chateaus
            {"en", "ens"}, //foramen -> foramens
            {"ex", "exes"}, //index -> indexes
            {"f", "ves"}, //wolf -> wolves 
            {"fe", "ves"}, //wolf -> wolves 
            {"ieu", "ieus milieu"}, //milieu-> mileus
            {"is", "is"}, //basis -> basis 
            {"ix", "ixes"}, //matrix -> matrixes
            {"nx", "nxes"}, //phalanx -> phalanxes 
            {"s", "s"}, //series -> series 
            {"sh", "shes"}, //wish -> wishes 
            {"us",  "us"},// genus -> us 
            {"x",  "xes"},// box -> boxes 
            {"y", "ies"}, //ferry -> ferries 
        };

        public string MakePlural(string singularName)
        {
            if (string.IsNullOrEmpty(singularName))
                return singularName;

            int index = singularName.IndexOf(' ');

            if (index != -1)
                return MakePlural(singularName.Substring(0, index)) + singularName.Substring(index);

            var result = exceptions.FirstOrDefault(r => singularName.EndsWith(r.Key));
            if (result.Value != null)
                return singularName.Substring(0, singularName.Length - result.Key.Length) + result.Value;

            return singularName + "s";
        }
    }

    public class SpanishPluralizer : IPluralizer
    {
        //http://es.wikipedia.org/wiki/Formaci%C3%B3n_del_plural_en_espa%C3%B1ol
        Dictionary<string, string> exceptions = new Dictionary<string, string>
        {
            {"x", "x"}, // tórax -> tórax
            {"s", "s"}, // suponemos que el nombre ya está pluralizado
            {"z", "ces"},  //vez -> veces
            {"g", "gues"}, //zigzag -> zigzagues
            {"c", "ques"}, //frac -> fraques
            {"án", "anes"},
            {"én", "enes"},
            {"ín", "ines"},
            {"ón", "ones"},
            {"ún", "unes"},
        };

        char[] vowels = new[] { 'a', 'e', 'i', 'o', 'u', 'á', 'é', 'í', 'ó', 'ú' };

        public string MakePlural(string singularName)
        {
            if (string.IsNullOrEmpty(singularName))
                return singularName;

            int index = singularName.IndexOf(' ');

            if (index != -1)
                return MakePlural(singularName.Substring(0, index)) + singularName.Substring(index);

            var result = exceptions.FirstOrDefault(r => singularName.EndsWith(r.Key));
            if (result.Value != null)
                return singularName.Substring(0, singularName.Length - result.Key.Length) + result.Value;

            char lassChar = singularName[singularName.Length - 1];
            if (vowels.Contains(lassChar))
                return singularName + "s";
            else
                return singularName + "es";
        }
    }

    public interface IGenderDetector
    {
        Gender GetGender(string name);
    }

    public class SpanishGenderDetector : IGenderDetector
    {
        //http://roble.pntic.mec.es/acid0002/index_archivos/Gramatica/genero_sustantivos.htm
        Dictionary<string, Gender> terminationIsFemenine = new Dictionary<string, Gender>()
        {
            {"umbre", Gender.Femenine },
           
            {"ión", Gender.Femenine },
            {"dad", Gender.Femenine },
            {"tad", Gender.Femenine },
            
            {"ie", Gender.Femenine },
            {"is", Gender.Femenine }, 

            {"pa", Gender.Masculine},
            //{"ta", Gender.Masculine}, Cuenta, Nota, Alerta... son femeninos
            {"ma", Gender.Masculine},

            {"a", Gender.Femenine},
            {"n", Gender.Masculine},
            {"o", Gender.Masculine},
            {"r", Gender.Masculine},
            {"s", Gender.Masculine},
            {"e", Gender.Masculine},
            {"l", Gender.Masculine},

            {"", Gender.Masculine},
        };


        public Gender GetGender(string name)
        {
            if (string.IsNullOrEmpty(name))
                return Gender.Neuter;

            int index = name.IndexOf(' ');

            if (index != -1)
                return GetGender(name.Substring(0, index));

            foreach (var kvp in terminationIsFemenine)
            {
                if (name.EndsWith(kvp.Key))
                    return kvp.Value;
            }

            return Gender.Masculine;
        }
    }

    public enum Gender
    {
        Neuter,    //_n 
        Masculine, //_m
        Femenine,  //_f
    }
}
