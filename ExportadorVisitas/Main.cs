using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TATOR.Controladores;
using System.IO;
using TATOR.partner;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TATOR
{
    public partial class Main : Form
    {
        public string user { get; set; }
        public string pass { get; set; }
        public string fileName { get; set; }

        private SynchronizationContext m_SynchronizationContext;

        private DateTime m_PreviousTime = DateTime.Now;       

        public Main()
        {
            InitializeComponent();

            m_SynchronizationContext = SynchronizationContext.Current;
        }

        private async void Main_Load(object sender, EventArgs e)
        {
            //pagina cargada
            //Si no hay usuario y contraseña enviamos al login

            
            var login = new Login();
            if (login.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                //Obtenemos los objetos de la organizacion
                user =  login.user;
                pass = login.pass;

                this.labelStatus.Text = "Descargando objetos de su organización...";
                List<string> listaObjetos = await Task<List<string>>.Run(() =>
                {
                   return ControladorMetadata.instancia.obtenerListaObjetos();
                });

                this.labelStatus.Text = "";
                listaObjetos.Sort();
                this.comboObjetos.Items.AddRange(listaObjetos.ToArray());

            }
            else
            {
                this.Close();
            }
            
        }

        private async void comboObjetos_SelectedValueChanged(object sender, EventArgs e)
        {
            //Obtenemos los campos del objeto seleccionado

            var objeto = this.comboObjetos.SelectedItem.ToString();

            this.labelStatus.Text = "Descargando campos para el objeto seleccionado...";
            List<string> listaCampos = await Task<List<string>>.Run(() =>
            {
                return ControladorMetadata.instancia.obtenerListaCampos(objeto);
            });

            this.labelStatus.Text = "";
            //var listaCampos = ControladorMetadata.instancia.obtenerListaCampos(this.comboObjetos.SelectedItem.ToString());
            listaCampos.Sort();

            //rellenamos combo
            this.comboCampo.SelectedItem = null;
            this.comboCampo.Items.Clear();
            this.comboCampo.Items.AddRange(listaCampos.ToArray());

            //rellenamos lista campos
            this.listBoxCampos.Items.Clear();
            this.listBoxCampos.Items.AddRange(listaCampos.ToArray());

        }

        //Seleccionar fichero con ids
        private void button2_Click(object sender, EventArgs e)
        {
            var FD = new System.Windows.Forms.OpenFileDialog();
            if (FD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.textBoxFicheroInput.Text = FD.FileName;
                this.textBoxFicheroSalida.Text = Path.GetDirectoryName(FD.FileName);
            }
        }

        //Seleccionar ruta destino
        private void button1_Click(object sender, EventArgs e)
        {
            var FD = new System.Windows.Forms.FolderBrowserDialog();
            if (FD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.textBoxFicheroSalida.Text = FD.SelectedPath;                
            }
        }


        public void Report(string value)
        {
            DateTime now = DateTime.Now;

            if ((now - m_PreviousTime).Milliseconds > 20)
            {
                m_SynchronizationContext.Post((@object) =>
                {                    
                    this.labelStatus.Text = (string)@object;
                }, value);

                m_PreviousTime = now;
            }
        }


        //Exportar
        private async void button3_Click(object sender, EventArgs e)
        {
            this.labelStatus.Text = "";
            var numParalelos = 0;
            try
            {
                //VALIDACIONES
                if (this.comboObjetos.SelectedItem == null)
                {
                    this.labelStatus.Text = "Seleccione un objeto.";
                    return;
                }

                if (this.comboCampo.SelectedItem == null)
                {
                    this.labelStatus.Text = "Seleccione un campo a filtrar.";
                    return;
                }

                if (this.textBoxFicheroInput.Text == "")
                {
                    this.labelStatus.Text = "Seleccione el fichero de entrada.";
                    return;
                }

                if (this.textBoxFicheroSalida.Text == "")
                {
                    this.labelStatus.Text = "Seleccione la ruta de salida.";
                    return;
                }

                if (this.listBoxCampos.CheckedItems.Count == 0 && this.textBoxCamposManual.Text == "")
                {
                    this.labelStatus.Text = "Seleccione los campos a exportar.";
                    return;
                }

                if (!String.IsNullOrEmpty(this.textBoxNumParalelos.Text) && !int.TryParse(this.textBoxNumParalelos.Text, out numParalelos))
                {
                    this.labelStatus.Text = "Numero de operaciones paralelas incorrecta";
                    return;
                }
                
                //FIN VALIDACIONES
                var fecha = DateTime.Now.ToString("yyyyMMdd");
                fileName = "Exportator-" + this.comboObjetos.SelectedItem.ToString() + "-" + fecha + ".csv";

                var objetoFrom =  this.comboObjetos.SelectedItem.ToString();

                
                //objetoFrom = "Attachment";

                var campoWhere = this.comboCampo.SelectedItem.ToString();

                var listaCampoSelect = new List<string>();

                if (this.listBoxCampos.CheckedItems.Count > 0)
                {
                    foreach (var campo in this.listBoxCampos.CheckedItems)
                    {
                        listaCampoSelect.Add(campo.ToString());
                    }                   
                }
                else 
                {
                    foreach (var campo in this.textBoxCamposManual.Text.Split(','))
                    {
                        listaCampoSelect.Add(campo.ToString().Trim());
                    }    
                }
                
                
                this.button3.Enabled = false;
                var listaQuerys = new List<Query>();

                await Task.Run(() =>
                {
                    //Obtenemos el numero de lineas del fichero
                    var numLines = 0;
                    using (var sr = new System.IO.StreamReader(this.textBoxFicheroInput.Text))
                    {
                        while (!sr.EndOfStream)
                        {
                            string linea = sr.ReadLine();
                            numLines++;
                        }
                    }
                    Report("Empezando exportación...");

                    var numbloquestotal = numLines / 200 + 1;

                    if (numParalelos > 0)
                    {
                        //creamos la carpeta "exportator" dentro de la ruta elegida
                        if (!Directory.Exists(this.textBoxFicheroSalida + @"\exportator"))
                            Directory.CreateDirectory(this.textBoxFicheroSalida.Text + @"\exportator");
                        else
                        {
                            DirectoryInfo directory = new DirectoryInfo(this.textBoxFicheroSalida.Text + @"\exportator");
                            //Borramos contenido
                            foreach (FileInfo file in directory.GetFiles())
                                file.Delete();
                        }
                    }


                    if (ControladorSalesforce.instancia.conectarSF(user, pass, "", 60000))
                    {
                        using (var sr = new System.IO.StreamReader(this.textBoxFicheroInput.Text))
                        {
                            var firstTime = true;
                            var listaIdsExportar = new List<string>();
                            var numBloques = 1;
                            while (!sr.EndOfStream || listaIdsExportar.Count > 0)
                            {
                                listaIdsExportar.Add(sr.ReadLine());

                                if (listaIdsExportar.Count >= 200 || sr.EndOfStream)
                                {
                                    if (numParalelos > 0)
                                    {
                                        var q = new Query();
                                        q.objetoFrom = objetoFrom;
                                        q.campoWhere = campoWhere;
                                        q.listaCampoSelect = listaCampoSelect;
                                        q.listaIdsExportar = new List<string>();
                                        q.listaIdsExportar.AddRange(listaIdsExportar);
                                        q.firstTime = firstTime;
                                        q.file = String.Format(this.textBoxFicheroSalida.Text + @"\exportator\exportator{0}.txt", numBloques);
                                        q.UI = this;
                                        listaQuerys.Add(q);
                                    }
                                    else
                                    {
                                        Report("Exportando " + numBloques + " de " + numbloquestotal);
                                        exportar(objetoFrom, campoWhere, listaCampoSelect, listaIdsExportar, firstTime);
                                    }

                                    numBloques++;
                                    firstTime = false;
                                    listaIdsExportar.Clear();
                                }
                            }

                        }

                        if (numParalelos == 0)
                               Report("Exportación Terminada. Gracias por usar exportator.");
                    }

                });

                if (numParalelos > 0)
                {
                    //Si hay tareas creadas las vamos paralelizando
                    var bloqueActual = 0;
                    var numBloques = listaQuerys.Count / numParalelos +1;
                    while (numParalelos * bloqueActual <= listaQuerys.Count)
                    {
                        var querys = listaQuerys.Skip(bloqueActual * numParalelos).Take(numParalelos);
                        await Task.Run(() =>
                        {
                            Parallel.ForEach(querys, x => x.runQuery().Wait());
                        });
                        bloqueActual++;
                        //Informamos a la interfaz
                        Report("Bloque " + bloqueActual + " de " + numBloques + " terminado");
                    }

                    //Juntamos fichero y terminamos
                    using (var output = File.Create(this.textBoxFicheroSalida.Text + @"\" + fileName))
                    {
                        foreach (var file in Directory.GetFiles(this.textBoxFicheroSalida.Text + @"\exportator"))
                        {
                            using (var input = File.OpenRead(file))
                            {
                                input.CopyTo(output);
                            }
                            File.Delete(file);
                        }
                        Directory.Delete(this.textBoxFicheroSalida.Text + @"\exportator");
                        Report("Fichero Generado! exportator al rescate una vez mas!!");                        
                    }
                }                
            }
            catch (Exception ex)
            {
                //this.labelStatus.Text = "Error no controlado.. Revise su fichero.";
                this.labelStatus.Text = "Error " + ex.Message;
            }
            finally
            {
                this.button3.Enabled = true;
            }

        }


        private void exportar(string objetoFrom, string campoWhere, List<string> listaCampoSelect, List<string> listaIdsExportar, bool firstTime)
        {
            var file = Path.Combine(this.textBoxFicheroSalida.Text, fileName);

            var sql = "select ";

            foreach (var campo in listaCampoSelect)
            {
                sql = sql + campo + ",";
            }

            sql = sql.Substring(0, sql.Length - 1);

            sql = sql + " FROM " + objetoFrom;

            //CampaignId = '701570000017hvo' and
            //Grupo_de_Control__c = false and
            var where = " WHERE  " + campoWhere + " IN (";
            //var where = " WHERE  ParentId "  + " IN (";
            foreach (var id in listaIdsExportar)
            {
                if (id.StartsWith("\""))
                    where = where + "'" + id.Substring(1,id.Length-2) + "'" + ",";
                else
                    where = where + "'" + id + "'" + ","; //OJO poner comillas para textos
            }
            where = where.Substring(0, where.Length - 1) + ")";

            sql = sql + where;

            QueryResult qr = null;
            List<sObject> resultado = ControladorSalesforce.instancia.query(sql, ref qr).ToList();


            while (!qr.done)
            {

                using (var sr = new System.IO.StreamWriter(file, true))
                {
                    if (firstTime)
                        sr.Write(ToCsv(listaCampoSelect, ",", resultado, true));
                    else
                        sr.Write(ToCsv(listaCampoSelect, ",", resultado, false));
                    sr.Flush();
                }

                resultado.Clear();
                resultado.AddRange(ControladorSalesforce.instancia.query(sql, ref qr).ToList());

            }

            if (resultado.Count > 0)
            {


                using (var sr = new System.IO.StreamWriter(file, true))
                {
                    if (firstTime)
                        sr.Write(ToCsv(listaCampoSelect, ",", resultado, true));
                    else
                        sr.Write(ToCsv(listaCampoSelect, ",", resultado, false));
                    sr.Flush();
                }
            }

        }


        public string ToCsv<T>(List<string> listaCamposSelect, string separator, IEnumerable<T> objectlist, bool writeHeader)
        {
            Type t = typeof(T);
            PropertyInfo[] fields = t.GetProperties();

            var header = "";

            foreach (var campo in listaCamposSelect)
            {
                header = header + campo + ",";
            }

            header = header.Substring(0, header.Length - 1);
                        
            StringBuilder csvdata = new StringBuilder();
            if (writeHeader)
                csvdata.AppendLine(header);

            foreach (var o in objectlist)
                csvdata.AppendLine(ToCsvFields(listaCamposSelect,separator, fields, o));

            return csvdata.ToString();
        }

        public string ToCsvFields(List<string> listaCamposSelect, string separator, PropertyInfo[] fields, object o)
        {
            StringBuilder linie = new StringBuilder();

            var header = "";

            foreach (var campo in listaCamposSelect)
            {
                header = header + campo + ",";
            }

            header = header.Substring(0, header.Length - 1);

            var count = 0;
            foreach (var nombre in header.Split(','))
            {

                try
                {

                    if (linie.Length > 0)
                    {
                        linie.Append(separator);
                        count++;
                    }

                    var x2 = GetPropValue(o, nombre);

                    if (x2 != null)
                    {

                        //if (!String.IsNullOrEmpty(x2.ToString()))
                        //    linie.Append("1");
                        //else
                        //    linie.Append("0");


                        if (x2.GetType().Name == "String" && x2.ToString() != "")
                            linie.Append("\"" + x2.ToString().Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ") + "\"");
                        else if (x2.GetType().Name == "String" && x2.ToString() == "")
                            linie.Append("\"\"");
                        else if (x2.GetType().Name == "Double")
                            linie.Append("\"" + x2.ToString().Replace(",", ".") + "\"");
                        else if (x2.GetType().Name == "DateTime")
                            linie.Append(((DateTime)x2).ToString("yyyy-MM-dd"));
                        else
                            linie.Append("\"" + x2.ToString().Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ") + "\"");

                    }
                    else
                    {
                        linie.Append("\"NULL\"");
                       // linie.Append("0");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error copiando registro al fichero " + ex.ToString());
                }
            }

            return linie.ToString();
        }

        public static object GetPropValue(object src, string propName)
        {
            try
            {
                return ((sObject)src).Any.Where(x => x.LocalName == propName).FirstOrDefault().InnerText;
            }
            catch(Exception ex)
            {
                throw new Exception("No se ha encontrado valor para el campo introducido.");
            }
        }


        #region EVENTOS
        private void buttonCamposManuales_Click(object sender, EventArgs e)
        {
             //reemplazamos el combo por un input            
            this.buttonSelLista.Enabled = true;
            this.textBoxCamposManual.Visible = true;
            this.buttonCamposManuales.Enabled = false;

        }

        private void buttonSelLista_Click(object sender, EventArgs e)
        {
            this.buttonCamposManuales.Enabled = true;
            this.textBoxCamposManual.Visible = false;
            this.buttonSelLista.Enabled = false;

        }

        private void panel6_MouseHover(object sender, EventArgs e)
        {
            
        }

        private void panel6_MouseLeave(object sender, EventArgs e)
        {           
        }

        private void panel6_MouseClick(object sender, MouseEventArgs e)
        {
            if (this.panel6.Height == 100)
            {
                this.panel6.Height = 22;                
                this.panel6.Location = new Point(-20, 784);
            }
            else
            {
                this.panel6.Height = 100;
                this.panel6.Location = new Point(-20, 705);                
            }
        }

        private void textBoxFicheroInput_TextChanged(object sender, EventArgs e)
        {

        }
        #endregion



        public class Query
        {
            private object threadLock = new object();
            public Main UI { get; set; }
            public string file { get; set; }
            public int numQuery { get; set; }
            public int timeout { get; set; }
            public bool isCustom { get; set; }
            public string objetoFrom { get; set; }
            public string campoWhere { get; set; }
            public List<string> listaCampoSelect { get; set; }
            public List<string> listaIdsExportar { get; set; }
            public bool firstTime { get; set; }

            public async Task runQuery()
            {
                await Task.Run(async () =>
                {
                    var threadId = Thread.CurrentThread.ManagedThreadId;

                    
                    Console.WriteLine("Query {0} started on thread {1} at {2}.",
                         numQuery, Thread.CurrentThread.ManagedThreadId, DateTime.Now.ToString("hh:mm:ss.fff"));

                    
                    var terminado = false;
                    
                    while (!terminado)
                    {
                        try
                        {
                            await Task<List<sObject>>.Run(() =>
                            {                               
                                var sql = "select ";
                                foreach (var campo in listaCampoSelect)
                                {
                                    sql = sql + campo + ",";
                                }
                                sql = sql.Substring(0, sql.Length - 1);
                                sql = sql + " FROM  " + objetoFrom;
                                //CampaignId = '701570000017hvo' and
                                //Grupo_de_Control__c = false and
                                var where = " WHERE  " + campoWhere + " IN (";
                                //var where = " WHERE  ParentId " + " IN (";
                                foreach (var id in listaIdsExportar)
                                {
                                    if (id.StartsWith("\""))
                                        where = where + "'" + id.Substring(1, id.Length - 2) + "'" + ",";
                                    else
                                        where = where + "'" + id + "'" + ","; 
                                }
                                where = where.Substring(0, where.Length - 1) + ")";
                                sql = sql + where;

                                QueryResult qr = null;
                                List<sObject> resultado = ControladorSalesforce.instancia.query(sql, ref qr).ToList();
                                
                                while (!qr.done)
                                {
                                    using (var sr = new System.IO.StreamWriter(file, true))
                                    {
                                        if (firstTime)
                                            sr.Write(UI.ToCsv(listaCampoSelect, ",", resultado, true));
                                        else
                                            sr.Write(UI.ToCsv(listaCampoSelect, ",", resultado, false));
                                        sr.Flush();
                                    }

                                    resultado.Clear();
                                    resultado.AddRange(ControladorSalesforce.instancia.query(sql, ref qr).ToList());
                                }

                                if (resultado.Count > 0)
                                {
                                    using (var sr = new System.IO.StreamWriter(file, true))
                                    {
                                        if (firstTime)
                                            sr.Write(UI.ToCsv(listaCampoSelect, ",", resultado, true));
                                        else
                                            sr.Write(UI.ToCsv(listaCampoSelect, ",", resultado, false));
                                        sr.Flush();
                                    }
                                }

                                terminado = true;
                            });
                            
                        }
                        catch (Exception ex)
                        {                           
                            /*
                            await UI.Dispatcher.BeginInvoke(
                                DispatcherPriority.Background,
                                new Action(() => UI.listBoxResultados.Items.Insert(0, "REINTENTAMOS EXCEPCION " + numQuery + " thread: " + threadId + " at " + DateTime.Now.ToString("hh:mm:ss.fff") + ex.Message)));
                           **/
                        }
                    }

                    Console.WriteLine("Query {0} stopped at {1}.",
                        numQuery, DateTime.Now.ToString("hh:mm:ss.fff"));

                    

                });

            }

           

        }

    }
}
