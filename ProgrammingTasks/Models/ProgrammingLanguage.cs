using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web;

namespace ProgrammingTasks.Models
{
    [TypeConverter(typeof(ProgrammingLanguageConverter))]
    public class ProgrammingLanguage
    {
        public static readonly ProgrammingLanguage C_PLUS_PLUS = new ProgrammingLanguage("C++", ".cpp");
        public static readonly ProgrammingLanguage C_SHARP = new ProgrammingLanguage("C#", ".cs");
        public static readonly ProgrammingLanguage JAVA = new ProgrammingLanguage("Java", ".java");

        private ProgrammingLanguage(string name, string extension)
        {
            Name = name;
            Extension = extension;
        }

        public string Name { get; private set; }
        public string Extension { get; private set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class ProgrammingLanguageConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                string valueString = (string)value;

                foreach (FieldInfo fieldInfo in typeof(ProgrammingLanguage).GetFields())
                {
                    ProgrammingLanguage programmingLangauge = (ProgrammingLanguage)fieldInfo.GetValue(null);

                    if (programmingLangauge.Name.ToLower().Equals(valueString.ToLower()))
                    {
                        return programmingLangauge;
                    }
                }
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}