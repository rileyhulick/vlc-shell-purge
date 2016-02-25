using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VLC_Shell_Purge
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ThingDoer.ThingToDo myThingToDo = ThingDoer.ThingToDo.Null;

            if (radioButton1.Checked)
                myThingToDo = ThingDoer.ThingToDo.Disable;
            else if (radioButton2.Checked)
                myThingToDo = ThingDoer.ThingToDo.Hide;
            else if (radioButton3.Checked)
                myThingToDo = ThingDoer.ThingToDo.Remove;
            else if (radioButton4.Checked)
                myThingToDo = ThingDoer.ThingToDo.Undo;

            //try
            {
                if (!checkBox3.Checked)
                    MessageBox.Show("A backup of your classes registry will be at \"C:\\temp\\undo_vlc_shell_purge.reg\".\n" +
                    "You can open that file to undo changes made by this program, or delete it if you're satisfied with how this worked.", 
                    "For to Undo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //some decorations to show that it's working.
                //This'll be undone when the program closes.
                Cursor.Current = Cursors.WaitCursor;

                ThingDoer.DoTheThing(myThingToDo, checkBox1.Checked, checkBox2.Checked, checkBox3.Checked);

                MessageBox.Show("Thing done successfully.", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information); //otherwise, an exception would have been thrown
                this.Close();
            }
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message, "Something went wrong.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    this.Close();
            //}
        }
    }
}
