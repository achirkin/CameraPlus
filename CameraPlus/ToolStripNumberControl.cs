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
            ((NumericUpDown)control).MouseClick += ToolStripNumberControl_MouseClick;
            ((NumericUpDown)control).Click += ToolStripNumberControl_Click;
        }

        protected override void OnUnsubscribeControlEvents(Control control)
        {
            base.OnUnsubscribeControlEvents(control);
            ((NumericUpDown)control).ValueChanged -= new EventHandler(OnValueChanged);
            ((NumericUpDown)control).MouseClick -= ToolStripNumberControl_MouseClick;
            ((NumericUpDown)control).Click -= ToolStripNumberControl_Click;
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

        public Control NumericUpDownControl
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

        private void ToolStripNumberControl_Click(object sender, EventArgs e)
        {
            Plugin.Log("Click!");
        }

        private void ToolStripNumberControl_MouseClick(object sender, MouseEventArgs e)
        {
            Plugin.Log("MouseClick!");
        }
    }
}
