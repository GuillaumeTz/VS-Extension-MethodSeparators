using System.ComponentModel;
using System.Windows.Media;

namespace MethodSeparators.Options
{
    internal class GeneralOptions : BaseOptionModel<GeneralOptions>
    {
        private double _lineSeparatorThickness = 1.5;

        [Category("Method Separators")]
        [DisplayName("Line Separator Thickness")]
        [Description("Thickness of the method separator line.")]
        public double LineSeparatorThickness
        {
            get { return _lineSeparatorThickness; }
            set { _lineSeparatorThickness = value; }
        }
    }
}
