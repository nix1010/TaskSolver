using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditorApplication
{
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
}
