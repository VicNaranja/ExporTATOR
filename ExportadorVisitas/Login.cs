using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using TATOR.Controladores;

namespace TATOR
{
    public partial class Login : Form
    {

        public string user { get; set; }
        public string pass { get; set; }

        public Login()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {            
            //Leemos los 2 valores y hacemos login en salesforce
            user = this.textBoxUserName.Text;
            pass = this.textBoxPassword.Text;

            //user = "arodriguez@hotelbeds.com";
            //pass = "20Odisea01qO4SuB7Qm0bOhZQsflNhsHLt";

            //user = "Sistemas.vol@iberostar.com";
            //pass = "Wacawaca316";

            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;


            if (ControladorSalesforce.instancia.conectarSF(user, pass, "", 6000))
            {
                 this.DialogResult = DialogResult.OK;
                 this.Close();
            }
            else
            {
                this.labelError.Text = "No se ha podido conectar con salesforce";
                //this.DialogResult = DialogResult.No;
            }
        }



    }
}
