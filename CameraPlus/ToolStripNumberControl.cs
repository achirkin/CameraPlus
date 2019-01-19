using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace CameraPlus
{
    [ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.ToolStrip)]
    public class ToolStripNumberControl : ToolStripControlHost
    {
        public ToolStripNumberControl()
            : base(new NumericUpDown())
        {

        }

        protected override void OnSubscribeControlEvents(Control control)
        {
            base.OnSubscribeControlEvents(control);
            ((NumericUpDown)control).ValueChanged += new EventHandler(OnValueChanged);
        }

        protected override void OnUnsubscribeControlEvents(Control control)
        {
            base.OnUnsubscribeControlEvents(control);
            ((NumericUpDown)control).ValueChanged -= new EventHandler(OnValueChanged);
        }

        public decimal Maximum
        {
            get
            {
                return (Control as NumericUpDown).Maximum;
            }
            set
            {
                (Control as NumericUpDown).Maximum = value;
            }
        }
        public decimal Minimum
        {
            get
            {
                return (Control as NumericUpDown).Minimum;
            }
            set
            {
                (Control as NumericUpDown).Minimum = value;
            }
        }

        public decimal Increment
        {
            get
            {
                return (Control as NumericUpDown).Increment;
            }
            set
            {
                (Control as NumericUpDown).Increment = value;
            }
        }

        public int DecimalPlaces
        {
            get
            {
                return (Control as NumericUpDown).DecimalPlaces;
            }
            set
            {
                (Control as NumericUpDown).DecimalPlaces = value;
            }
        }

        public decimal Value
        {
            get
            {
                return (Control as NumericUpDown).Value;
            }
            set
            {
                (Control as NumericUpDown).Value = value;
            }
        }

        public event EventHandler ValueChanged;

        public NumericUpDown NumericUpDownControl
        {
            get { return Control as NumericUpDown; }
        }

        public void OnValueChanged(object sender, EventArgs e)
        {
            if (ValueChanged != null)
            {
                ValueChanged(this, e);
            }
        }
    }
}
