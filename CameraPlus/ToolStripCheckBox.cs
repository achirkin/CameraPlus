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
    public class ToolStripCheckBox : ToolStripControlHost
    {
        public ToolStripCheckBox(string text)
            : base(new CheckBox())
        {
            this.Text = text;
        }

        protected override void OnSubscribeControlEvents(Control control)
        {
            base.OnSubscribeControlEvents(control);
            ((CheckBox)control).CheckedChanged += new EventHandler(OnCheckedChanged);
        }

        protected override void OnUnsubscribeControlEvents(Control control)
        {
            base.OnUnsubscribeControlEvents(control);
            ((CheckBox)control).CheckedChanged -= new EventHandler(OnCheckedChanged);
        }

        public bool Checked
        {
            get
            {
                return (Control as CheckBox).Checked;
            }
            set
            {
                (Control as CheckBox).Checked = value;
            }
        }

        public event EventHandler CheckedChanged;

        public Control NumericUpDownControl
        {
            get { return Control as CheckBox; }
        }

        public void OnCheckedChanged(object sender, EventArgs e)
        {
            if (CheckedChanged != null)
            {
                CheckedChanged(this, e);
            }
        }
    }
}
