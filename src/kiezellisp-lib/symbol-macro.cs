using System;

namespace Kiezel
{
    public class SymbolMacro
    {
        public object Form;

        public SymbolMacro(object form)
        {
            Form = form;    
        }

        public override string ToString()
        {
            return String.Format("Kiezel.SymbolMacro Form={0}", Runtime.ToPrintString(Form));
        }
    }
}

