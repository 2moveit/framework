﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Entities.Properties;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System.Globalization;

namespace Signum.Entities
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public abstract class ValidatorAttribute : Attribute
    {
        public Func<ModifiableEntity, bool> IsApplicable; 
        public string ErrorMessage { get; set; }

        public int Order { get; set; }

        //Descriptive information that continues the sentence: The property should {HelpMessage}
        //Used for documentation purposes only
        public abstract string HelpMessage { get; }

        public string Error(ModifiableEntity entity, PropertyInfo property, object value)
        {
            if (IsApplicable != null && !IsApplicable(entity))
                return null;

            string defaultError = OverrideError(value);

            if (defaultError == null)
                return null;

            string error = GetLocalizedErrorMessage(property, this) ?? ErrorMessage ?? defaultError;
            if (error != null)
                error = error.Formato(property.NiceName());

            return error; 
        }

        static string GetLocalizedErrorMessage(PropertyInfo property, ValidatorAttribute validator)
        {
            Assembly assembly = property.DeclaringType.Assembly;
            if (assembly.HasAttribute<LocalizeDescriptionsAttribute>())
            {
                string key = property.DeclaringType.Name.Add(property.Name, "_").Add(validator.GetType().Name, "_");
                string result = assembly.GetDefaultResourceManager().GetString(key);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// When overriden, validates the value against this validator rule
        /// </summary>
        /// <param name="value"></param>
        /// <returns>returns an string with the error message, using {0} if you want the property name to be inserted</returns>
        protected abstract string OverrideError(object value);
    }

    public class NotNullValidatorAttribute : ValidatorAttribute
    {
        protected override string OverrideError(object obj)
        {
            if (obj == null)
                return Resources._0IsNotSet;

            return null;
        }

        public override string HelpMessage
        {
            get { return Resources.BeNotNull; }
        }
    }

    public class StringLengthValidatorAttribute : ValidatorAttribute
    {
        int min = -1;
        int max = -1;
        bool allowNulls = false;

        public bool AllowNulls
        {
            get { return allowNulls; }
            set { allowNulls = value; }
        }

        public int Min
        {
            get { return min; }
            set { min = value; }
        }

        public int Max
        {
            get { return max; }
            set { max = value; }
        }

        protected override string OverrideError(object value)
        {
            string val = (string)value;

            if (string.IsNullOrEmpty(val))
                return allowNulls ? null : Resources._0IsNotSet;

            if (min == max && min != -1 && val.Length != min)
                return Resources.TheLenghtOf0HasToBeEqualTo0.Formato(min);

            if (min != -1 && val.Length < min)
                return Resources.TheLengthOf0HasToBeGreaterOrEqualTo0.Formato(min);

            if (max != -1 && val.Length > max)
                return Resources.TheLengthOf0HasToBeLesserOrEqualTo0.Formato(max);

            return null;
        }

        public override string HelpMessage
        {
            get
            {
                string result =
                    min != -1 && max != -1 ? Resources.HaveBetween0And1Characters.Formato(min, max) :
                    min != -1 ? Resources.HaveMinimum0Characters.Formato(min) :
                    max != -1 ? Resources.HaveMaximun0Characters.Formato(max) : null;

                if (allowNulls)
                    result = result.Add(Resources.OrBeNull, " ");

                return result;
            }
        }
    }


    public class RegexValidatorAttribute : ValidatorAttribute
    {
        Regex regex;
        public RegexValidatorAttribute(Regex regex)
        {
            this.regex = regex;
        }

        public RegexValidatorAttribute(string regexExpresion)
        {
            this.regex = new Regex(regexExpresion);
        }

        string formatName;
        public string FormatName
        {
            get { return formatName; }
            set { formatName = value; }
        }

        protected override string OverrideError(object value)
        {
            string str = (string)value;
            if (string.IsNullOrEmpty(str))
                return null;

            if (regex.IsMatch(str))
                return null;

            if (formatName == null)
                return Resources._0HasNoCorrectFormat;
            else
                return Resources._0DoesNotHaveAValid0Format.Formato(formatName);
        }

        public override string HelpMessage
        {
            get
            {
                return Resources.HaveValid0Format.Formato(formatName);
            }
        }
    }

    public class EMailValidatorAttribute : RegexValidatorAttribute
    {
        public static readonly Regex EmailRegex = new Regex(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                          @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                          @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", RegexOptions.IgnoreCase);

        public EMailValidatorAttribute()
            : base(EmailRegex)
        {
            this.FormatName = "e-Mail";
        }
    }

    public class TelephoneValidatorAttribute : RegexValidatorAttribute
    {
        public static readonly Regex TelephoneRegex = new Regex(@"^((\+|00)\d\d)? *(\([ 0-9]+\))? *[0-9][ \-0-9]+$");

        public TelephoneValidatorAttribute()
            : base(TelephoneRegex)
        {
            this.FormatName = Resources.Telephone;
        }
    }

    public class URLValidatorAttribute : RegexValidatorAttribute
    {
        public static readonly Regex URLRegex = new Regex(
              "^(https?://)"
            + "?(([0-9a-z_!~*'().&=+$%-]+: )?[0-9a-z_!~*'().&=+$%-]+@)?" //user@ 
            + @"(([0-9]{1,3}\.){3}[0-9]{1,3}" // IP- 199.194.52.184 
            + "|" // allows either IP or domain 
            + @"([0-9a-z_!~*'()-]+\.)*" // tertiary domain(s)- www. 
            + @"([0-9a-z][0-9a-z-]{0,61})?[0-9a-z]" // second level domain 
            + @"(\.[a-z]{2,6})?)" // first level domain- .com or .museum 
            + "(:[0-9]{1,4})?" // port number- :80 
            + "((/?)|" // a slash isn't required if there is no file name 
            + "(/[0-9a-z_!~*'().;?:@&=+$,%#-]+)+/?)$", RegexOptions.IgnoreCase);

        public URLValidatorAttribute()
            : base(URLRegex)
        {
            this.FormatName = "URL";
        }
    }

    public class FileNameValidatorAttribute : RegexValidatorAttribute
    {
        public static readonly Regex FileNameRegex = new Regex(@"^(?!^(PRN|AUX|CLOCK\$|NUL|CON|COM\d|LPT\d|\..*)(\..+)?$)[^\x00-\x1f\\?*:\"";|/]+$");
        public FileNameValidatorAttribute()
            : base(FileNameRegex)
        {
            this.FormatName = Resources.FileName;
        }
    }

    public class DecimalsValidatorAttribute : ValidatorAttribute
    {
        public int DecimalPlaces { get; set; }

        public DecimalsValidatorAttribute()
        {
            DecimalPlaces = 2;
        }

        public DecimalsValidatorAttribute(int decimalPlaces)
        {
            this.DecimalPlaces = decimalPlaces;
        }

        protected override string OverrideError(object value)
        {
            if (value == null)
                return null;

            if (value is decimal && Math.Round((decimal)value, DecimalPlaces) != (decimal)value)
            {
                return Resources._0HasMoreThan0DecimalPlaces.Formato(DecimalPlaces);
            }

            return null;
        }

        public override string HelpMessage
        {
            get { return Resources.Have0Decimals.Formato(DecimalPlaces); }
        }
    }


    public class NumberIsValidatorAttribute : ValidatorAttribute
    {
        public ComparisonType ComparisonType;
        public IComparable number;

        public NumberIsValidatorAttribute(ComparisonType comparison, float number)
        {
            this.ComparisonType = comparison;
            this.number = number;
        }

        public NumberIsValidatorAttribute(ComparisonType comparison, double number)
        {
            this.ComparisonType = comparison;
            this.number = number;
        }

        public NumberIsValidatorAttribute(ComparisonType comparison, byte number)
        {
            this.ComparisonType = comparison;
            this.number = number;
        }

        public NumberIsValidatorAttribute(ComparisonType comparison, short number)
        {
            this.ComparisonType = comparison;
            this.number = number;
        }

        public NumberIsValidatorAttribute(ComparisonType comparison, int number)
        {
            this.ComparisonType = comparison;
            this.number = number;
        }

        public NumberIsValidatorAttribute(ComparisonType comparison, long number)
        {
            this.ComparisonType = comparison;
            this.number = number;
        }

        protected override string OverrideError(object value)
        {
            if (value == null)
                return null;

            IComparable val = (IComparable)value;

            if (number.GetType() != value.GetType())
                number = (IComparable)Convert.ChangeType(number, value.GetType()); // asi se hace solo una vez 

            bool ok = (ComparisonType == ComparisonType.EqualTo && val.CompareTo(number) == 0) ||
                      (ComparisonType == ComparisonType.DistinctTo && val.CompareTo(number) != 0) ||
                      (ComparisonType == ComparisonType.GreaterThan && val.CompareTo(number) > 0) ||
                      (ComparisonType == ComparisonType.GreaterThanOrEqual && val.CompareTo(number) >= 0) ||
                      (ComparisonType == ComparisonType.LessThan && val.CompareTo(number) < 0) ||
                      (ComparisonType == ComparisonType.LessThanOrEqual && val.CompareTo(number) <= 0);

            if (ok)
                return null;

            return Resources._0HasToBe0Than1.Formato(ComparisonType.NiceToString(), number.ToString());
        }

        public override string HelpMessage
        {
            get { return Resources.Be + ComparisonType.NiceToString() + " " + number.ToString(); }
        }
    }

    //Not using C intervals to please user!
    public class NumberBetweenValidatorAttribute : ValidatorAttribute
    {
        IComparable min;
        IComparable max;

        public NumberBetweenValidatorAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public NumberBetweenValidatorAttribute(double min, double max)
        {
            this.min = min;
            this.max = max;
        }

        public NumberBetweenValidatorAttribute(byte min, byte max)
        {
            this.min = min;
            this.max = max;
        }

        public NumberBetweenValidatorAttribute(short min, short max)
        {
            this.min = min;
            this.max = max;
        }

        public NumberBetweenValidatorAttribute(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        public NumberBetweenValidatorAttribute(long min, long max)
        {
            this.min = min;
            this.max = max;
        }

        protected override string OverrideError(object value)
        {
            if (value == null)
                return null;

            IComparable val = (IComparable)value;

            if (min.GetType() != value.GetType())
            {
                min = (IComparable)Convert.ChangeType(min, val.GetType()); // asi se hace solo una vez 
                max = (IComparable)Convert.ChangeType(max, val.GetType());
            }

            if (min.CompareTo(val) <= 0 &&
                val.CompareTo(max) <= 0)
                return null;

            return Resources._0HasToBeBetween0And1.Formato(min, max);
        }

        public override string HelpMessage
        {
            get { return Resources.BeBetween0And1.Formato(min, max); }
        }
    }

    public class NoRepeatValidatorAttribute : ValidatorAttribute
    {
        protected override string OverrideError(object value)
        {
            IList list = (IList)value;
            if (list == null || list.Count <= 1)
                return null;
            string ex = list.Cast<object>().GroupCount().Where(kvp => kvp.Value > 1).ToString(e => "{0} x {1}".Formato(e.Key, e.Value), ", ");
            if (ex.HasText())
                return Properties.Resources._0HasSomeRepeatedElements0.Formato(ex);
            return null;
        }

        public override string HelpMessage
        {
            get { return Resources.HaveNoRepeatedElements; }
        }
    }

    public class CountIsValidatorAttribute : ValidatorAttribute
    {
        public ComparisonType ComparisonType;
        public int number;

        public CountIsValidatorAttribute(ComparisonType comparison, int number)
        {
            this.ComparisonType = comparison;
            this.number = number;
        }

        protected override string OverrideError(object value)
        {
            IList list = (IList)value;

            int val = list == null? 0: list.Count;

            if ((ComparisonType == ComparisonType.EqualTo && val.CompareTo(number) == 0) ||
                (ComparisonType == ComparisonType.DistinctTo && val.CompareTo(number) != 0) ||
                (ComparisonType == ComparisonType.GreaterThan && val.CompareTo(number) > 0) ||
                (ComparisonType == ComparisonType.GreaterThanOrEqual && val.CompareTo(number) >= 0) ||
                (ComparisonType == ComparisonType.LessThan && val.CompareTo(number) < 0) ||
                (ComparisonType == ComparisonType.LessThanOrEqual && val.CompareTo(number) <= 0))
                return null;

            return Resources.TheNumberOfElementsOf0HasToBe01.Formato(ComparisonType.NiceToString(), number.ToString());
        }

        public override string HelpMessage
        {
            get { return Resources.HaveANumberOfElements01.Formato(ComparisonType.NiceToString(), number.ToString()); }
        }
    }

    public class DaysPrecissionValidatorAttribute : DateTimePrecissionValidatorAttribute
    {
        public DaysPrecissionValidatorAttribute()
            : base(DateTimePrecision.Days)
        { }
    }

    public class SecondsPrecissionValidatorAttribute : DateTimePrecissionValidatorAttribute
    {
        public SecondsPrecissionValidatorAttribute()
            : base(DateTimePrecision.Seconds)
        { }
    }

    public class MinutesPrecissionValidatorAttribute : DateTimePrecissionValidatorAttribute
    {
        public MinutesPrecissionValidatorAttribute()
            : base(DateTimePrecision.Minutes)
        { }

    }
    public class DateTimePrecissionValidatorAttribute : ValidatorAttribute
    {

        public DateTimePrecision Precision { get; private set; }

        public DateTimePrecissionValidatorAttribute(DateTimePrecision precision)
        {
            this.Precision = precision;
        }

        protected override string OverrideError(object value)
        {
            if (value == null)
                return null;

            var prec = ((DateTime)value).GetPrecision();
            if (prec > Precision)
                return "{{0}} has a precission of {0} instead of {1}".Formato(prec, Precision);

            return null;
        }

        public string FormatString
        {
            get
            {
                var dtfi = CultureInfo.CurrentCulture.DateTimeFormat;
                switch (Precision)
                {
                    case DateTimePrecision.Days: return "d";
                    case DateTimePrecision.Hours: return dtfi.ShortDatePattern + " " + "HH";
                    case DateTimePrecision.Minutes: return "g";
                    case DateTimePrecision.Seconds: return "G";
                    case DateTimePrecision.Milliseconds: return dtfi.ShortDatePattern + " " + dtfi.LongTimePattern + ".fff";
                    default: return "";
                }
            }
        }

        public override string HelpMessage
        {
            get
            {
                return Resources.HaveAPrecisionOf + " " + Precision.NiceToString().ToLower();
            }
        }
    }

    public class StringCaseValidatorAttribute : ValidatorAttribute
    {
        private Case textCase;
        public Case TextCase
        {
            get { return this.textCase; }
            set { this.textCase = value; }
        }

        public StringCaseValidatorAttribute(Case textCase)
        {
            this.textCase = textCase;
        }

        protected override string OverrideError(object value)
        {
            if (string.IsNullOrEmpty((string)value)) return null;

            string str = (string)value;

            if ((this.textCase == Case.Uppercase) && (str != str.ToUpper()))
                return Resources._0HasToBeUppercase;

            if ((this.textCase == Case.Lowercase) && (str != str.ToLower()))
                return Resources._0HasToBeLowercase;

            return null;
        }

        public override string HelpMessage
        {
            get { return Resources.Be + (textCase == Case.Uppercase ? Resources.Uppercase : Resources.Lowercase); }
        }
    }

    public enum Case
    {
        Uppercase,
        Lowercase
    }
    
    [ForceLocalization]
    public enum ComparisonType
    {
        EqualTo,
        DistinctTo,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
    }

    public class StateValidator<E, S> : IEnumerable
        where E : ModifiableEntity
        where S : struct
    {
        Func<E, S> getState;
        string[] propertyNames;
        string[] propertyNiceNames;
        Func<E, object>[] getters;

        Dictionary<S, bool?[]> dictionary = new Dictionary<S, bool?[]>();

        public StateValidator(Func<E, S> getState, params Expression<Func<E, object>>[] properties)
        {
            this.getState = getState;
            PropertyInfo[] pis = properties.Select(p => ReflectionTools.GetPropertyInfo(p)).ToArray();
            propertyNames = pis.Select(pi => pi.Name).ToArray();
            propertyNiceNames = pis.Select(pi => pi.NiceName()).ToArray();
            getters = properties.Select(p => p.Compile()).ToArray();
        }

        public void Add(S state, params bool?[] necessary)
        {
            if (necessary != null && necessary.Length != propertyNames.Length)
                throw new ArgumentException("The StateValidator {0} for state {1} has {2} values instead of {3}"
                    .Formato(GetType().TypeName(), state, necessary.Length, propertyNames.Length));

            dictionary.Add(state, necessary);
        }

        public string Validate(E entity, PropertyInfo pi)
        {
            return Validate(entity, pi, true);
        }

        public bool? IsAllowed(S state, PropertyInfo pi)
        {
            int index = propertyNames.IndexOf(pi.Name);
            if (index == -1)
                return null;

            return dictionary.GetOrThrow(state, Resources.State0NotRegisteredInStateValidator)[index];
        }

        public string Validate(E entity, PropertyInfo pi, bool showState)
        {
            int index = propertyNames.IndexOf(pi.Name);
            if (index == -1)
                return null;

            S state = getState(entity);

            bool? necessary = dictionary.GetOrThrow(state, Resources.State0NotRegisteredInStateValidator)[index];

            if (necessary == null)
                return null;

            object val = getters[index](entity);
            if (val is IList && ((IList)val).Count == 0 || val is string && ((string)val).Length == 0) //both are indistinguible after retrieving
                val = null;

            if (val != null && !necessary.Value)
                return showState ? Resources._0IsNotAllowedOnState1.Formato(propertyNiceNames[index], state) :
                                   Resources._0IsNotAllowed.Formato(propertyNiceNames[index]);

            if (val == null && necessary.Value)
                return showState ? Resources._0IsNecessaryOnState1.Formato(propertyNiceNames[index], state) :
                                   Resources._0IsNecessary.Formato(propertyNiceNames[index]);

            return null;
        }

        public bool? Necessary(S state, PropertyInfo pi)
        {
            int index = propertyNames.IndexOf(pi.Name);
            if (index == -1)
                throw new ArgumentException("The property is not registered");

            return dictionary[state][index];
        }

        public IEnumerator GetEnumerator() //just to use object initializer
        {
            throw new NotImplementedException();
        }
    }

}