#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    public class SymbolMacro
    {
        #region Fields

        public object Form;

        #endregion Fields

        #region Constructors

        public SymbolMacro(object form)
        {
            Form = form;
        }

        #endregion Constructors

        #region Public Methods

        public override string ToString()
        {
            return string.Format("Kiezel.SymbolMacro Form={0}", Runtime.ToPrintString(Form));
        }

        #endregion Public Methods
    }
}