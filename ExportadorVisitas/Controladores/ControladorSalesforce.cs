///<Autor>Francisco Javier Cabezas</Autor>
///<Modificador><Modificador>
///<Fecha Creacion>24-03-2009</Fecha Creacion>
///<Fecha Modificacion></Fecha Modificacion>

using System;
using System.Net;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using TATOR.partner;
using TATOR.Controladores;


namespace TATOR.Controladores


{
    public class ControladorSalesforce
    {
        #region Atributos

        //Constante con el texto de un error por timeout
        //private static String SESION_EXPIRADA = "timed out";

        //Momento del ultimo login realizado
        private DateTime _ultimoLogin;

        private bool errorActivo = false;

        //Nombre de usuario de SalesForce
        private String _usuarioSF;

        //Password de SalesForce
        private String _passwordSF;

        //Proxy a utilizar
        private String _proxyNombre;

        //Variable que realiza las operaciones contra SalesForce
        private SforceService _binding;

        //Variable que recoge el timeOut de la sesión
        private int _timeOut;

        //Variables para la gestión de reintentos de las querys
        //private int numReintentoIni = Convert.ToInt32((NTS.Configuration.IniManager.devolverValor(ConstantesLN.SF_QUERY_REPETIR)));
        //private int numIntento = 0;
		private int _nUpdate = 0;
        private int _nCreate = 0;
        private int _nDelete = 0;
        private int _nMerge = 0;

        private static ControladorSalesforce _instancia;

       
       
        //private QueryResult qr = null;

        //private bool done = false;

        #endregion

        #region Propiedades

        public SforceService binding
        {
            get { return _binding; }

        }

        public string metadataURL { get; set; }

        public static ControladorSalesforce instancia
        {
            get
            {
                if (ControladorSalesforce._instancia == null)
                    ControladorSalesforce._instancia = new ControladorSalesforce();
                return ControladorSalesforce._instancia;
            }
        }
        #endregion

        #region Métodos Privados

        /// <summary>
        /// Funcion privada para crear objetos en SalesForce, se usa para 
        /// poder hacer un intento extra en caso de error (para solventar timeouts etc)
        /// </summary>
        /// <param name="objetos">Objetos a crear</param>
        /// <param name="repetir">Indica si se ha de hacer otro intento en caso de error</param>
        /// <param name="longitudBloque"> Indica la longitud del bloque, recordar que el máximo serán 200</param>
        /// <returns>Devuelve una lista con tantos elementos, como se hayan intentado crear, indicando
        /// en cada posición si el elemento se ha insertado de forma correcta o no</returns>
        
        private SaveResult[] crearObjetosRepetir(sObject[] objetos, bool repetir, int longitudBloque_)
        {
            //Maximo de 200 objetos por vez
            SaveResult[] resultado = new SaveResult[0];
            //ArrayList arrayListTemp = null;
            //ArrayList arrayListFinal = new ArrayList(0);
            SaveResult[] resultadoTemporal = null;
            SaveResult[] resultadoTemporal2 = null;

            sObject[] objetosTemporal = null;
            sObject[] objetosRestantes = objetos;
            int vueltas = 0;
            bool hecho = false;

            longitudBloque_ = 200;
            _nCreate = 0;

            while (!hecho)
            {
                if (objetos != null && objetos.Length > 0)
                {
                    if (objetosRestantes.Length <= longitudBloque_)
                    {
                        objetosTemporal = objetosRestantes;
                        hecho = true;
                    }
                    else
                    {
                        objetosTemporal = new sObject[longitudBloque_];
                        Array.Copy(objetosRestantes, 0, objetosTemporal, 0, longitudBloque_);
                        vueltas++;
                        objetosRestantes = new sObject[objetos.Length - (vueltas * longitudBloque_)];
                        Array.Copy(objetos, vueltas * longitudBloque_, objetosRestantes, 0, objetosRestantes.Length);

                        //Array.Copy(objetos, (vueltas - 1) * longitudBloque_, objetosRestantes, 0, objetosRestantes.Length);
                    }
                    try
                    {
                        this.reconectar();
                        resultadoTemporal = resultado;
                        _nCreate++;
                        //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.opNormal, "ControladorSalesforce.crearObjetosRepetir ", " || **** INICIO INSERT # " + _nCreate + " **** ");
                        resultadoTemporal2 = binding.create(objetosTemporal);
                        //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.opNormal, "ControladorSalesforce.crearObjetosRepetir ", " || **** FIN INSERT **** ");
                        resultado = new SaveResult[resultadoTemporal.Length + resultadoTemporal2.Length];
                        Array.Copy(resultadoTemporal, 0, resultado, 0, resultadoTemporal.Length);
                        Array.Copy(resultadoTemporal2, 0, resultado, resultadoTemporal.Length, resultadoTemporal2.Length);
                    }
                    catch (System.Web.Services.Protocols.SoapException ex)
                    {
                        
                        this.errorActivo = true;
                        if (repetir)
                        {
                            
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.crearObjetosRepetir(objetos, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.modificarObjetosRepetir", "Error haciendo Insert en SFDC (SoapException) --> " + ex.Message);
                        }
                    }
                    catch ( System.Net.WebException ex)
                    {
                        this.errorActivo = true;
                        if (repetir)
                        {

                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.crearObjetosRepetir(objetos, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.modificarObjetosRepetir", "Error haciendo Insert en SFDC (WebException) --> " + ex.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.errorActivo = true;
                        if (repetir)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.crearObjetosRepetir(objetos, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.modificarObjetosRepetir", "Error haciendo Insert en SFDC (Exception) --> " + ex.Message);
                        }
                    }
                }
                else
                {
                    //if ((resultado != null) && (arrayListFinal.Count > 0))
                    //{
                    //    resultado = (SaveResult[])arrayListFinal.ToArray(typeof(SaveResult));
                    //    arrayListTemp = null;
                    //}

                    hecho = true;
                }
            }

            return resultado;
        }

        
        private void reconectar()
        {
            bool resultadoTest = false;

            try
            {
                DescribeGlobalResult describe = binding.describeGlobal();
                resultadoTest = true;
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.opNormal, "ControladorSalesForce.reconectar", " ERROR TEST Reconectando con Salesforce.com. SoapException:" + ex.Message);
            }
            catch (System.Net.WebException ex)
            {
                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.opNormal, "ControladorSalesForce.reconectar", " ERROR TEST Reconectando con Salesforce.com. WebException:" + ex.Message);
            }
            catch (Exception ex2)
            {
                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.opNormal, "ControladorSalesForce.reconectar", " ERROR TEST Reconectando con Salesforce.com. Exception:" + ex2.Message);
            }

            
            if (!resultadoTest)
            {
                // caso de fallar el Test Forzamos la reconexion
                this.desconectarSF();
                this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
            }
            else
            {
                // caso de no fallar el Test Forzamos la reconexion
                reconectarNoError();
            }
        }
        
        
        
        private void reconectarNoError()
        {
            
            DateTime ahora = DateTime.Now;
            ahora = ahora.AddHours(-this._ultimoLogin.TimeOfDay.Hours);
            if (ahora.TimeOfDay.Hours == 2)
            {
                this.desconectarSF();
                this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);                
                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.opNormal, "ControladorSalesForce.reconectar", "Reconectando con Salesforce.com.");
            }
            else
            {
                ahora = DateTime.Now;
                ahora = ahora.AddMinutes(-this._ultimoLogin.TimeOfDay.Minutes);
                if (ahora.TimeOfDay.Minutes > 100)
                {//ha pasado media hora, reconectamos
                    this.desconectarSF();
                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.opNormal, "ControladorSalesForce.reconectar", "Reconectando con Salesforce.com.");
                }
            }
        }

        /// <summary>
        /// Funcion privada para borrar objetos en SalesForce, se usa para 
        /// poder hacer un intento extra en caso de error (para solventar timeouts etc)
        /// </summary>
        /// <param name="objetos">Objetos a borrar</param>
        /// <param name="repetir">Indica si se ha de hacer otro intento en caso de error</param>
        /// <returns>Devuelve una lista con tantos elementos, como se hayan intentado borrar, indicando
        /// en cada posición si el elemento se ha borrado de forma correcta o no</returns>
        private DeleteResult[] borrarObjetosRepetir(String[] objetos, bool repetir, int longitudBloque_)
        {
            //Maximo de 200 objetos por vez
            DeleteResult[] resultado = new DeleteResult[0];
            DeleteResult[] resultadoTemporal = null;
            DeleteResult[] resultadoTemporal2 = null;
            String[] objetosTemporal = null;
            String[] objetosRestantes = objetos;
            int vueltas = 0;
            bool hecho = false;

            longitudBloque_ = 200;
            _nDelete = 0;

            while (!hecho)
            {                                
                if (objetos != null && objetos.Length > 0)
                {
                    if (objetosRestantes.Length <= longitudBloque_)
                    {
                        objetosTemporal = objetosRestantes;
                        hecho = true;
                    }
                    else
                    {
                        objetosTemporal = new String[longitudBloque_];
                        Array.Copy(objetosRestantes, 0, objetosTemporal, 0, longitudBloque_);
                        vueltas++;
                        objetosRestantes = new String[objetos.Length - (vueltas * longitudBloque_)];
                        Array.Copy(objetos, vueltas * longitudBloque_, objetosRestantes, 0, objetosRestantes.Length);
                    }
                    try
                    {
                        this.reconectar();
                        resultadoTemporal = resultado;
                        _nDelete++;
                        //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.opNormal, "ControladorSalesforce.borrarObjetosRepetir ", " || **** INICIO DELETE # " + _nDelete + " **** ");
                        resultadoTemporal2 = binding.delete(objetosTemporal);
                        //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.opNormal, "ControladorSalesforce.borrarObjetosRepetir ", " || **** FIN DELETE **** ");
                        resultado = new DeleteResult[resultadoTemporal.Length + resultadoTemporal2.Length];
                        Array.Copy(resultadoTemporal, 0, resultado, 0, resultadoTemporal.Length);
                        Array.Copy(resultadoTemporal2, 0, resultado, resultadoTemporal.Length, resultadoTemporal2.Length);
                    }
                    catch (System.Web.Services.Protocols.SoapException ex)
                    {
                        this.errorActivo = true;
                        if (repetir)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);

                            resultado = this.borrarObjetosRepetir(objetos, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.borrarObjetosRepetir Error borrando objetos: " + ex.Message);
                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.modificarObjetosRepetir", "Error haciendo Delete en SFDC (SoapException) --> " + ex.Message);
                        }
                    }
                    catch (System.Net.WebException ex)
                    {                       
                        this.errorActivo = true;
                        if (repetir)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);

                            resultado = this.borrarObjetosRepetir(objetos, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.borrarObjetosRepetir Error borrando objetos: " + ex.Message);
                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.modificarObjetosRepetir", "Error haciendo Delete en SFDC (WebException) --> " + ex.Message);

                        }
                  
                    }
                    catch (Exception ex)
                    {
                        this.errorActivo = true;
                        if (repetir)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.borrarObjetosRepetir(objetos, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.borrarObjetosRepetir Error borrando objetos: " + ex.Message);
                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.modificarObjetosRepetir", "Error haciendo Delete en SFDC (Exception) --> " + ex.Message);
                        }
                    }
                }
                else
                {
                    hecho = true;
                }
            }

            return resultado;
        }

        /// <summary>
        /// Funcion privada para modificar objetos en SalesForce, se usa para 
        /// poder hacer un intento extra en caso de error (para solventar timeouts etc)
        /// </summary>
        /// <param name="objetos">Objetos a modificar</param>
        /// <param name="repetir">Indica si se ha de hacer otro intento en caso de error</param>
        /// <returns>Devuelve una lista con tantos elementos, como se hayan intentado modificar, indicando
        /// en cada posición si el elemento se ha modificado de forma correcta o no</returns>
        private SaveResult[] modificarObjetosrepetir(sObject[] objetos, bool repetir, int longitudBloque_)
        {
            //Maximo de 200 objetos por vez
            SaveResult[] resultado = new SaveResult[0];
            SaveResult[] resultadoTemporal = null;
            SaveResult[] resultadoTemporal2 = null;
            sObject[] objetosTemporal = null;
            sObject[] objetosRestantes = objetos;
            int vueltas = 0;
            bool hecho = false;

            //longitudBloque_ = 200;
            _nUpdate = 0;
            
            while (!hecho)
            {                
                if (objetos != null && objetos.Length > 0)
                {
                    if (objetosRestantes.Length <= longitudBloque_)
                    {
                        objetosTemporal = objetosRestantes;
                        hecho = true;
                    }
                    else
                    {
                        objetosTemporal = new sObject[longitudBloque_];
                        Array.Copy(objetosRestantes, 0, objetosTemporal, 0, longitudBloque_);
                        vueltas++;
                        objetosRestantes = new sObject[objetos.Length - (vueltas * longitudBloque_)];
                        //Array.Copy(objetos, (vueltas - 1) * longitudBloque_, objetosRestantes, 0, objetosRestantes.Length);
                        Array.Copy(objetos, vueltas * longitudBloque_, objetosRestantes, 0, objetosRestantes.Length);
                    }
                    try
                    {
                        this.reconectar();
                        resultadoTemporal = resultado;
                        _nUpdate++;
                        //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.opNormal, "ControladorSalesforce.modificarObjetosrepetir ", " || **** INICIO UPDATE # " + _nUpdate + " **** ");
                        resultadoTemporal2 = binding.update(objetosTemporal);
                        //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.opNormal, "ControladorSalesforce.modificarObjetosrepetir ", " || **** FIN UPDATE **** ");
                        resultado = new SaveResult[resultadoTemporal.Length + resultadoTemporal2.Length];
                        Array.Copy(resultadoTemporal, 0, resultado, 0, resultadoTemporal.Length);
                        Array.Copy(resultadoTemporal2, 0, resultado, resultadoTemporal.Length, resultadoTemporal2.Length);
                    }
                    catch (System.Web.Services.Protocols.SoapException ex)
                    {
                        this.errorActivo = true;
                        if (repetir)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.modificarObjetosrepetir(objetos, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.modificarObjetosRepetir Error modificando objetos1: " + ex.Message);
                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.modificarObjetosRepetir", "Error haciendo Update en SFDC (SoapException) --> " + ex.Message);

                        }
                    }
                    catch (System.Net.WebException ex)
                    {
                        this.errorActivo = true;
                        if (repetir)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.modificarObjetosrepetir(objetos, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.modificarObjetosRepetir Error modificando objetos1: " + ex.Message);
                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.modificarObjetosRepetir", "Error haciendo Update en SFDC (WebException) --> " + ex.Message);

                        }
                    }
                    catch (Exception ex)
                    {
                        this.errorActivo = true;
                        if (repetir)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.modificarObjetosrepetir(objetos, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.modificarObjetosRepetir Error modificando objetos2: " + ex.Message);

                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.modificarObjetosRepetir", "Error haciendo Update en SFDC (Exception) --> " + ex.Message);

                        }
                    }
                }
                else
                {
                    hecho = true;
                }
            }

            return resultado;
        }

        private DateTime GetServerTimeStamp()
        {
            GetServerTimestampResult srvTime = null;

            DateTime myTime;

            try
            {
                srvTime = new GetServerTimestampResult();

                myTime = srvTime.timestamp;


            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.GetServerTimeStamp", "SoapException: " + ex.Message);
                //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.GetServerTimeStamp SoapException: " + ex.Message);                       
                          
                throw ex;
            }
            catch (System.Net.WebException ex)
            {
                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.GetServerTimeStamp", "WebException: " + ex.Message);
                //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.GetServerTimeStamp WebException: " + ex.Message);                       
                
                throw ex;
            }
            catch (Exception ex2)
            {
                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.GetServerTimeStamp", "Exception: " + ex2.Message);
                //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.GetServerTimeStamp Exception: " + ex2.Message);                       
                
                throw ex2;

            }
            return myTime; 
        }

        /// <summary>
        /// Funcion privada para unificar objetos en SalesForce, se usa para 
        /// poder hacer un intento extra en caso de error (para solventar timeouts etc)
        /// </summary>
        /// <param name="objetos">Objetos a unificar</param>
        /// <param name="repetir">Indica si se ha de hacer otro intento en caso de error</param>
        /// <returns>Devuelve una lista con tantos elementos, como se hayan intentado unificar, indicando
        /// en cada posición si el elemento se ha unificado de forma correcta o no</returns>
        private MergeResult[] mergeObjetosrepetir(MergeRequest[] objetos, bool repetir, int longitudBloque_)
        {
            //Maximo de 200 objetos por vez
            MergeResult[] resultado = new MergeResult[0];
            MergeResult[] resultadoTemporal = null;
            MergeResult[] resultadoTemporal2 = null;
            MergeRequest[] objetosTemporal = null;
            MergeRequest[] objetosRestantes = objetos;
            int vueltas = 0;
            bool hecho = false;

            longitudBloque_ = 200;
            _nMerge = 0;

            while (!hecho)
            {
                if (objetos != null && objetos.Length > 0)
                {
                    if (objetosRestantes.Length <= longitudBloque_)
                    {
                        objetosTemporal = objetosRestantes;
                        hecho = true;
                    }
                    else
                    {
                        objetosTemporal = new MergeRequest[longitudBloque_];
                        Array.Copy(objetosRestantes, 0, objetosTemporal, 0, longitudBloque_);
                        vueltas++;
                        objetosRestantes = new MergeRequest[objetos.Length - (vueltas * longitudBloque_)];
                        //Array.Copy(objetos, (vueltas - 1) * longitudBloque_, objetosRestantes, 0, objetosRestantes.Length);
                        Array.Copy(objetos, vueltas * longitudBloque_, objetosRestantes, 0, objetosRestantes.Length);
                    }
                    try
                    {
                        this.reconectar();
                        resultadoTemporal = resultado;
                        resultadoTemporal2 = binding.merge(objetosTemporal);
                        _nMerge++;
                        //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.opNormal, "ControladorSalesforce.modificarObjetosrepetir ", " || **** INICIO MERGE # " + _nUpdate + " **** ");
                        resultado = new MergeResult[resultadoTemporal.Length + resultadoTemporal2.Length];
                        //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.opNormal, "ControladorSalesforce.modificarObjetosrepetir ", " || **** FIN MERGE **** ");
                        Array.Copy(resultadoTemporal, 0, resultado, 0, resultadoTemporal.Length);
                        Array.Copy(resultadoTemporal2, 0, resultado, resultadoTemporal.Length, resultadoTemporal2.Length);
                    }
                    catch (System.Web.Services.Protocols.SoapException ex)
                    {
                        this.errorActivo = true;
                        if (repetir)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.mergeObjetosrepetir(objetos, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.modificarObjetosRepetir Error mergeando objetos3: " + ex.Message);
                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.modificarObjetosRepetir", "Error haciendo Merge en SFDC (SoapException) --> " + ex.Message);

                        }
                    }
                    catch (System.Net.WebException ex)
                    {
                        this.errorActivo = true;
                        if (repetir)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.mergeObjetosrepetir(objetos, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.modificarObjetosRepetir Error mergeando objetos3: " + ex.Message);
                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.modificarObjetosRepetir", "Error haciendo Merge en SFDC (WebException) --> " + ex.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.errorActivo = true;
                        if (repetir)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.mergeObjetosrepetir(objetos, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.modificarObjetosRepetir Error mergeando objetos4: " + ex.Message);
                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.modificarObjetosRepetir", "Error haciendo Merge en SFDC (Exception) --> " + ex.Message);

                        }
                    }
                }
                else
                {
                    hecho = true;
                }
            }

            return resultado;
        }

        private UpsertResult[] upsertObjetosRepetir(sObject[] objetos, String externalField, bool repetir, int longitudBloque_)
        {
            //Maximo de 200 objetos por vez
            UpsertResult[] resultado = new UpsertResult[0];
            UpsertResult[] resultadoTemporal = null;
            UpsertResult[] resultadoTemporal2 = null;
            sObject[] objetosTemporal = null;
            //sObject[] objetosTemporalAux = null;
            sObject[] objetosRestantes = objetos;
            int vueltas = 0;
            bool hecho = false;
            while (!hecho)
            {
                if (objetos != null && objetos.Length > 0)
                {
                    if (objetosRestantes.Length <= longitudBloque_)
                    {
                        objetosTemporal = objetosRestantes;
                        hecho = true;
                    }
                    else
                    {
                        objetosTemporal = new sObject[longitudBloque_];
                        Array.Copy(objetosRestantes, 0, objetosTemporal, 0, longitudBloque_);
                        vueltas++;
                        objetosRestantes = new sObject[objetos.Length - (vueltas * longitudBloque_)];
                        Array.Copy(objetos, (vueltas - 1) * longitudBloque_, objetosRestantes, 0, objetosRestantes.Length);
                    }
                    try
                    {
                        this.reconectar();
                        resultadoTemporal = resultado;

                        //objetosTemporalAux = new sObject[1];
                        //for (int i = 0; i < objetosTemporal.Length; i++)
                        //{
                        //    objetosTemporalAux[0] = objetosTemporal[i];
                        //    resultadoTemporal2 = binding.upsert(externalField, objetosTemporalAux);
                        //}

                        ////LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.aviso, "ControladorSalesForce.upsertObjetosRepetir", "INICIO upsert de objetos: " + DateTime.Now.ToString());
                        resultadoTemporal2 = binding.upsert(externalField, objetosTemporal);
                        ////LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.aviso, "ControladorSalesForce.upsertObjetosRepetir", "FIN upsert de objetos: " + DateTime.Now.ToString());
                        resultado = new UpsertResult[resultadoTemporal.Length + resultadoTemporal2.Length];
                        Array.Copy(resultadoTemporal, 0, resultado, 0, resultadoTemporal.Length);
                        Array.Copy(resultadoTemporal2, 0, resultado, resultadoTemporal.Length, resultadoTemporal2.Length);
                    }
                    catch (System.Web.Services.Protocols.SoapException ex)
                    {
                        this.errorActivo = true;
                        if (repetir)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.upsertObjetosRepetir(objetos, externalField, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.upsertObjetosRepetir Error haciendo upsert de objetos: " + ex.Message);                       

                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.upsertObjetosRepetir", "Error haciendo upsert de objetos: " + ex.Message);

                        }
                    }
                    catch (System.Net.WebException ex)
                    {
                        this.errorActivo = true;
                        if (repetir)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.upsertObjetosRepetir(objetos, externalField, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.upsertObjetosRepetir Error haciendo upsert de objetos: " + ex.Message);                       

                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.upsertObjetosRepetir", "Error haciendo upsert de objetos: " + ex.Message);

                        }
                    }
                    catch (Exception ex)
                    {
                        this.errorActivo = true;
                        if (repetir)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.upsertObjetosRepetir(objetos, externalField, false, longitudBloque_);
                        }
                        else
                        {
                            resultado = null;
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.upsertObjetosRepetir Error haciendo upsert de objetos: " + ex.Message);

                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.upsertObjetosRepetir", "Error haciendo upsert de objetos:: " + ex.Message);

                        }
                    }
                }
                else
                {
                    hecho = true;
                }
            }

            return resultado;
        }

        /// <summary>
        /// Funcion privada para realizar una consulta en SalesForce, se usa para 
        /// poder hacer un intento extra en caso de error (para solventar timeouts etc)
        /// </summary>
        /// <param name="sql">Consulta a realizar</param>
        /// <param name="repetir">Indica si se ha de hacer otro intento en caso de error</param>
        /// <returns>Devuelve una lista con los objetos que cumplen la consulta</returns>
        private sObject[] queryRepetir(String sql, ref QueryResult qr_, bool repetir)
        {
            sObject[] resultado = new sObject[0];
            try
            {               
                this.reconectar();
            
                if (qr_ == null) //Si se ha finalizado o no se ha ejecutado nunca la consulta
                {
                    //LogErrores.instancia.escribirLogAFichero(LogErrores.eTipoMensaje.aviso, "ControladorSalesforce.queryRepetir()", " **** REALIZO QUERY **** ");

                    qr_ = binding.query(sql);
                    if (qr_.size > 0)
                        resultado = qr_.records;
                }
                else //Si no se ha finalizado la consulta que esta en proceso, queryMore() 
                {
                    if (!qr_.done)
                    {
                        //LogErrores.instancia.escribirLogAFichero(LogErrores.eTipoMensaje.aviso, "ControladorSalesforce.queryRepetir()", " **** REALIZO QUERY_MORE **** ");
                        
                        qr_ = binding.queryMore(qr_.queryLocator);
                        if (qr_.size > 0)
                            resultado = qr_.records;
                    }
                    else
                        qr_ = null;
                }

            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                this.errorActivo = true;
                String error = "ControladorSalesForce.Query System.Web.Services.Protocols.SoapException ex: ";
                error = error + ex.Message + "\nSQL: " + sql;
                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.queryRepetir", " Error --> " + error);
                if (repetir)
                {
                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.queryRepetir", "Reintento ..... ");
                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.queryRepetir(sql, ref qr_, false);
                }
                else
                {
                    resultado = null;
                    qr_ = null;
                    throw ex;
                }

            }
            catch (System.Net.WebException ex)
            {
                this.errorActivo = true;
                String error = "ControladorSalesForce.Query System.Net.WebException ex: ";
                error = error + ex.Message + "\nSQL: " + sql;
                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.queryRepetir", "Error: " + error);
                if (repetir)
                {
                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.queryRepetir", "Reintento ..... ");
                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.queryRepetir(sql, ref qr_, false);
                }
                else
                {
                    resultado = null;
                    qr_ = null;
                    //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.queryRepetir Error: " + error);

                    throw ex;
                }
            }
            catch (Exception ex)
            {
                String error = "ControladorSalesForce.Query Exception: ";
                error = error + ex.Message + "\nSQL: " + sql;
                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.queryRepetir", "Error: " + error);
                this.errorActivo = true;

                if (repetir)
                {
                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.queryRepetir", "Reintento ..... ");
                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.queryRepetir(sql, ref qr_, false);
                }
                else
                {
                    resultado = null;
                    qr_ = null;
                    //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.queryRepetir Error: " + error);

                    throw ex;
                }

            }
            return resultado;
        }

        /// <summary>
        /// Funcion privada para hacer una consulta de tipo count() sobre Salesforce.com, se usa para 
        /// poder hacer un intento extra en caso de error (para solventar timeouts etc)
        /// </summary>
        /// <param name="objetos">Consulta a realizar</param>
        /// <param name="repetir">Indica si se ha de hacer otro intento en caso de error</param>
        /// <returns>Devuelve el número de objetos que cumplen la consulta</returns>
        private int contarQueryRepetir(String sql, bool repetir)
        {
            int resultado = -1;
            QueryResult qr = null;

            try
            {
                this.reconectar();
                qr = binding.query(sql);
                resultado = qr.size;
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                this.errorActivo = true;
                if (repetir)
                {
                    ////LogErrores.escribeError("Repitiendo...");
                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.contarQueryRepetir(sql, false);
                }
                else
                {
                    resultado = -1;
                    String error = "ControladorSalesForce.contarQuery System.Web.Services.Protocols.SoapException ex: ";
                    error = error + ex.Message + "\nSQL: " + sql;
                    //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF,  error);

                    //LogErrores.instancia.escribirLog(//LogErrores.eTipoMensaje.error, "ControladorSalesForce.contarQueryRepetir", "Error: " + error);
                }
            }
            catch (System.Net.WebException ex)
            {
                this.errorActivo = true;
                if (repetir)
                {
                    ////LogErrores.escribeError("Repitiendo...");
                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.contarQueryRepetir(sql, false);
                }
                else
                {
                    resultado = -1;
                    String error = "ControladorSalesForce.contarQuery System.Net.WebException ex: ";
                    error = error + ex.Message + "\nSQL: " + sql;
                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.contarQueryRepetir", "Error: " + error);
                    //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.contarQueryRepetir Error: " + error);
                }
            }
            catch (Exception ex)
            {
                this.errorActivo = true;
                if (repetir)
                {
                    //LogErrores.escribeError("Repitiendo...");
                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.contarQueryRepetir(sql, false);
                }
                else
                {
                    resultado = -1;
                    String error = "ControladorSalesForce.contarQuery Exception: ";
                    error = error + ex.Message + "\nSQL: " + sql;
                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.contarQueryRepetir", "Error: " + error);
                }
            }

            return resultado;
        }

        /// <summary>
        /// Función privada para conectar con SalesForce, se usa para poder hacer un intento extra en caso de error (para solventar timeouts etc)
        /// </summary>
        /// <param name="usuario">Nombre de usuario de SalesForce</param>
        /// <param name="password">Contraseña de usuario de SalesForce</param>
        /// <param name="repetir">Indica si se ha de hacer otro intento en caso de error</param>
        /// <returns>Devuelve true o false indicando si se ha conectado o no</returns>
        private bool conectarSFRepetir(String usuario, String password, bool repetir, String nombreProxy, int timeOut)
        {
            bool resultado = false;
            int intentos = 3; // maximo de intentos antes de lanzar exception

            if (!repetir)
            {
                intentos = 1;
            }

            while (!resultado && intentos > 0)
            {
                intentos--;
                try
                {
                    resultado =conectarSFRepetirBase(usuario,password,repetir, nombreProxy,timeOut);        
                }
                catch (Exception ex)
                {
                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesforce.conectarSFRepetir", "Exception:" + ex.Message);
                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesforce.conectarSFRepetir", "Intentos Restantes:" + intentos);
                    if (intentos == 0)
                    {
                        //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesforce.conectarSFRepetir", "Exceso de Intentos al conectarme con SF");
                        //LogErrores.instancia.enviarLogPorMail("ERROR CONEXION SF");
                        //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.conectarSFRepetir Exceso de Intentos al conectarme Error: " + ex.Message);
                
                        throw new Exception("Exceso de Intentos al conectarme "+ ex.Message);
                    }
                }
                if (!resultado)
                {
                    this.errorActivo = true;
                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesforce.conectarSFRepetir", " Espero 100 Seg Antes del siguiente intento de  Conexion ");
                    //System.Threading.Thread.Sleep(5000);
                }
            }

            return resultado;

        }
        
        
        private bool conectarSFRepetirBase(String usuario, String password, bool repetir, String nombreProxy, int timeOut)        
        {
            AssignmentRuleHeader arh = null;
            bool resultado = false;
            LoginResult loginResult;
            this._usuarioSF = usuario;
            this._passwordSF = password;
            this._proxyNombre = nombreProxy;
            this._timeOut = timeOut;
            this._binding = new SforceGzip();

            //this._binding = new SforceService();

            try
            {
                if ((this._proxyNombre != null) && (!this._proxyNombre.Equals("")))
                {
                    IWebProxy ObjetoProxy = new WebProxy(nombreProxy, true);
                    //IWebProxy ObjetoProxy = new WebProxy(nombreProxy, 8080);
                    _binding.Proxy = ObjetoProxy;
                    _binding.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;

                }

                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.aviso, "ControladorCargarDatos.tratarCreaciones", " *** INICIO Haciendo login a SFDC (" + DateTime.Now.ToString() + ")" );
                loginResult = binding.login(this._usuarioSF, this._passwordSF);
                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.aviso, "ControladorCargarDatos.tratarCreaciones", " *** FIN Login a SFDC realizado (" + DateTime.Now.ToString() + ") || SessionId --> " + loginResult.sessionId);

                //Change the binding to the new endpoint
                binding.Url = loginResult.serverUrl;
                // Time out en milisegundos, forzamos que al menos sean 100 segundos que es lo que establece Salesforce.com por defecto
                if (timeOut > Constantes.TIME_OUT_DEFECTO)
                    binding.Timeout = timeOut;
                else
                    binding.Timeout = Constantes.TIME_OUT_DEFECTO;
                //Create a new session header object and set the session id to that returned by the login
                binding.SessionHeaderValue = new SessionHeader();
                binding.SessionHeaderValue.sessionId = loginResult.sessionId;
                binding.QueryOptionsValue = new QueryOptions();
                metadataURL = loginResult.metadataServerUrl;

                binding.QueryOptionsValue.batchSize = Constantes.NUMERO_MAXIMO_REGISTROS_SALESFORCE_QUERY;



                binding.QueryOptionsValue.batchSizeSpecified = true;
                arh = new AssignmentRuleHeader();
                arh.useDefaultRule = true;
                binding.AssignmentRuleHeaderValue = arh;
                DescribeGlobalResult describe = binding.describeGlobal();
                this.errorActivo = false;
                this._ultimoLogin = DateTime.Now;
                resultado = true;
            }
            catch (System.Web.Services.Protocols.SoapException e)
            {
                this.errorActivo = true;
                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesforce.conectarSFRepetirBase", "SoapException Error haciendo login." + e.Message);
                //throw new Exception("SoapException Error haciendo login." + e.Message);
            }
            catch (System.Net.WebException ex)
            {
                this.errorActivo = true;
                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesforce.conectarSFRepetirBase", "SoapException Error haciendo login." + ex.Message);
                //throw new Exception("SoapException Error haciendo login." + ex.Message);
            }
            catch (Exception e2)
            {
                this.errorActivo = true;
                // This is something else, probably comminication

                //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesforce.conectarSFRepetirBase", "Exception Error haciendo login." + e2.Message);
                //throw new Exception("Exception Error haciendo login." + e2.Message);
            }

            return resultado;

        }

        /// <summary>
        /// Funcion privada que devuelve todos los objetos eliminados desde una fecha hasta cinco minutos antes de la operación
        /// se usa para poder hacer un intento extra en caso de error (para solventar timeouts etc)
        /// </summary>
        /// <param name="objeto">Tipo de objeto a recuperar</param>
        /// <param name="fechaInicio">Fecha desde la que se quiere recuperar los objetos</param>
        /// <param name="repetir">Indica si se ha de hacer otro intento en caso de error</param>
        /// <returns>Devuelve una lista con los elementos del tipo que se indica, eliminados desde la fecha que se pasa
        /// como parámetro</returns>
        private String[] obtenerBorradosRepetir(String objeto, DateTime fechaInicio, bool repetir)
        {
            GetDeletedResult gdr = null;
            String[] resultado = null;
            DeletedRecord[] borrados = null;


            try
            {
                this.reconectar();
                DateTime fechaFin = binding.getServerTimestamp().timestamp;
                //Para asegurarnos de que el servidor de SalesForce acepta la query
                fechaFin.AddMinutes(-5);
                gdr = binding.getDeleted(objeto, fechaInicio, fechaFin);
                borrados = gdr.deletedRecords;
                resultado = new String[borrados.Length];

                for (int i = 0; i < resultado.Length; i++)
                    resultado[i] = borrados[i].id;
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                this.errorActivo = true;
                if (repetir)
                {

                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.obtenerBorradosRepetir(objeto, fechaInicio, false);
                }
                else
                {
                    resultado = null;
                    //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.ObtenerBorradosRepetir Error: " + ex.Message);
                
                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.ObtenerBorradosRepetir", "Error: " + ex.Message);
                }
            }
            catch (System.Net.WebException ex)
            {
                this.errorActivo = true;
                if (repetir)
                {

                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.obtenerBorradosRepetir(objeto, fechaInicio, false);
                }
                else
                {
                    resultado = null;
                    //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.ObtenerBorradosRepetir Error: " + ex.Message);
               
                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.ObtenerBorradosRepetir", "Error: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                this.errorActivo = true;
                if (repetir)
                {
                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.obtenerBorradosRepetir(objeto, fechaInicio, false);
                }
                else
                {
                    resultado = null;
                    //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.ObtenerBorradosRepetir Error: " + ex.Message);
               
                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.ObtenerBorradosRepetir", "Error: " + ex.Message);
                }
            }

            return resultado;
        }

        /// <summary>
        /// Funcion privada para obtener la descripción de un objeto Salesforce (para solventar timeouts etc)
        /// </summary>
        /// <param name="objeto">Tipo de objeto a recuperar</param>
        /// <param name="repetir">Indica si se ha de hacer otro intento en caso de error</param>
        /// <returns>Descripción del objeto cuyo tipo se pasa como parámetro</returns>
        private DescribeSObjectResult describirObjetoRepetir(String objeto, bool repetir)
        {
            DescribeSObjectResult resultado = null;

            try
            {
                this.reconectar();
                resultado = binding.describeSObject(objeto);
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                this.errorActivo = true;
                if (repetir)
                {
                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.describirObjetoRepetir(objeto, false);
                }
                else
                {
                    resultado = null;
                    //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.describirObjetosRepetir Error: " + ex.Message);               
                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.describirObjetosRepetir", "Error: " + ex.Message);
                }
            }
            catch (System.Net.WebException ex)
            {
                this.errorActivo = true;
                if (repetir)
                {
                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.describirObjetoRepetir(objeto, false);
                }
                else
                {
                    resultado = null;
                    //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.describirObjetosRepetir Error: " + ex.Message);               

                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.describirObjetosRepetir", "Error: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                this.errorActivo = true;
                if (repetir)
                {
                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.describirObjetoRepetir(objeto, false);
                }
                else
                {
                    resultado = null;
                    //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.describirObjetosRepetir Error: " + ex.Message);               

                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.describirObjetosRepetir", "Error: " + ex.Message);
                }
            }

            return resultado;
        }

        /// <summary>
        /// Funcion privada que devuelve todos los objetos creados o modificados desde una fecha hasta cinco minutos antes de la operación
        /// se usa para poder hacer un intento extra en caso de error (para solventar timeouts etc)
        /// </summary>
        /// <param name="objeto">Tipo de objeto a recuperar</param>
        /// <param name="fechaInicio">Fecha desde la que se quiere recuperar los objetos</param>
        /// <param name="repetir">Indica si se ha de hacer otro intento en caso de error</param>
        /// <returns>Devuelve una lista con los ids de los objetos creados o modificados del tipo y desde la fecha que se pasan como parámetro.</returns>
        private String[] obtenerModificadosRepetir(String objeto, DateTime fechaInicio, bool repetir)
        {
            GetUpdatedResult gur = null;
            String[] resultado = null;

            try
            {
                this.reconectar();
                DateTime fechaFin = binding.getServerTimestamp().timestamp;
                //Para asegurarnos de que el servidor de SalesForce acepta la query
                fechaFin.AddMinutes(-5);
                gur = binding.getUpdated(objeto, fechaInicio, fechaFin);
                resultado = gur.ids;
                this.errorActivo = false;
            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                this.errorActivo = true;
                if (repetir)
                {
                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.obtenerModificadosRepetir(objeto, fechaInicio, false);
                }
                else
                {
                    resultado = null;
                    //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.obtenerModificadosRepetir Error: " + ex.Message);               
                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.obtenerModificadosRepetir", "Error: " + ex.Message);
                }
            }
            catch (System.Net.WebException ex)
            {
                this.errorActivo = true;
                if (repetir)
                {
                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.obtenerModificadosRepetir(objeto, fechaInicio, false);
                }
                else
                {
                    resultado = null;
                    //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.obtenerModificadosRepetir Error: " + ex.Message);               

                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.obtenerModificadosRepetir", "Error: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                this.errorActivo = true;
                if (repetir)
                {
                    this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                    resultado = this.obtenerModificadosRepetir(objeto, fechaInicio, false);
                }
                else
                {
                    resultado = null;
                    //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.obtenerModificadosRepetir Error: " + ex.Message);               

                    //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.obtenerModificadosRepetir", "Error: " + ex.Message);
                }
            }

            return resultado;
        }
        /// <summary>
        /// Método que realiza el retrieve
        /// </summary>
        /// <param name="fields_">Campos incluídos en le retrieve</param>
        /// <param name="type_">Tipo de objeto</param>
        /// <param name="repetir_">Repetir o no en caso de error</param>
        /// <param name="ids_">Identificador de los objetos a recuperar</param>
        /// <param name="longitudBloque_">Número de objetos a recuperar en el retrieve, como máximo 2000</param>
        /// <returns></returns>
        private sObject[] retrieveObjetosRepetir(string fields_, string type_, bool repetir_, string[] ids_, int longitudBloque_)
        {
            //Maximo de 200 objetos por vez
            sObject[] resultado = new sObject[0];
            ArrayList arrayListIdTemp = null;
            ArrayList arrayListIdObjetosRestantes = null;
            ArrayList arrayListTemp = new ArrayList(0);
            ArrayList arrayListFinal = new ArrayList(0);
            bool hecho = false;
            int longitudBloque = 0;

            this.reconectar();

            if (longitudBloque_ > Constantes.NUMERO_MAXIMO_REGISTROS_SALESFORCE_RETRIEVE)
                longitudBloque = Constantes.NUMERO_MAXIMO_REGISTROS_SALESFORCE_RETRIEVE;
            else
                longitudBloque = longitudBloque_;
            if (ids_ != null && ids_.Length > 0)
            {
                arrayListIdObjetosRestantes = new ArrayList(ids_);
            }

            while (!hecho)
            {
                if (arrayListIdObjetosRestantes != null && arrayListIdObjetosRestantes.Count > 0)
                {
                    if (arrayListIdObjetosRestantes.Count <= longitudBloque)
                    {
                        arrayListIdTemp = arrayListIdObjetosRestantes;
                        arrayListIdObjetosRestantes = null;
                    }
                    else
                    {
                        arrayListIdTemp = new ArrayList(arrayListIdObjetosRestantes.GetRange(0, longitudBloque));
                        arrayListIdObjetosRestantes.RemoveRange(0, longitudBloque);
                    }
                    try
                    {
                        this.reconectar();
                        arrayListTemp = new ArrayList(binding.retrieve(fields_, type_, (string[])arrayListIdTemp.ToArray(typeof(string))));
                        arrayListTemp.TrimToSize();
                        arrayListFinal.AddRange(arrayListTemp);
                        arrayListTemp = null;
                    }
                    catch (System.Web.Services.Protocols.SoapException ex)
                    {
                        this.errorActivo = true;
                        if (repetir_)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.retrieveObjetosRepetir(fields_, type_, false, ids_, longitudBloque);
                        }
                        else
                        {
                            resultado = null;
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.retrieveObjetosRepetir Error:" + ex.Message);

                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.retrieveObjetosRepetir", "Error:" + ex.Message);
                        }
                    }
                    catch (System.Net.WebException ex)
                    {
                        this.errorActivo = true;
                        if (repetir_)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.retrieveObjetosRepetir(fields_, type_, false, ids_, longitudBloque);
                        }
                        else
                        {
                            resultado = null;
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.retrieveObjetosRepetir Error:" + ex.Message);
                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.retrieveObjetosRepetir", "Error :" + ex.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.errorActivo = true;
                        if (repetir_)
                        {
                            this.conectarSF(this._usuarioSF, this._passwordSF, this._proxyNombre, this._timeOut);
                            resultado = this.retrieveObjetosRepetir(fields_, type_, false, ids_, longitudBloque);
                        }
                        else
                        {
                            resultado = null;
                            //this.eventRegister.RegistrarErrorEntidad(ControladorEventosNagios.ENTIDAD_IMPORTIB, ControladorEventosNagios.ERROR_SF, "ControladorSalesForce.retrieveObjetosRepetir Error:" + ex.Message);

                            //LogErrores.instancia.escribirLog(LogErrores.eTipoMensaje.error, "ControladorSalesForce.retrieveObjetosRepetir", "Error:" + ex.Message);
                        }
                    }
                }
                else
                {
                    if ((resultado != null) && (arrayListFinal.Count > 0))
                    {
                        resultado = (sObject[])arrayListFinal.ToArray(typeof(sObject));
                        arrayListTemp = null;
                    }
                    this.errorActivo = false;
                    hecho = true;
                }
            }

            return resultado;
        }


        

        private void Sleep()
        {
            //System.Threading.Thread.Sleep(1400);
            System.Threading.Thread.Sleep(100);
        }

        #endregion Métodos Privados

        #region Metodos Públicos

        /// <summary>
        /// Método público para conectar con SalesForce, 
        /// </summary>
        /// <param name="usuario">Nombre de usuario de SalesForce</param>
        /// <param name="password">Contraseña de usuario de SalesForce</param>
        /// <param name="nombreProxy">Proxy</param>
        /// <param name="timeOut">Milisegundos para el timeOut</param>

        /// <returns>Devuelve true o false indicando si se ha conectado o no</returns>
        public bool conectarSF(String usuario, String password, String nombreProxy, int timeOut)
        {
            this.Sleep();
            return this.conectarSFRepetir(usuario, password, false, nombreProxy, timeOut);
        }
        /// <summary>
        /// Método público para desconectar de SalesForce
        /// </summary>
        public void desconectarSF()
        {
            this.Sleep();
            this._binding = null;
        }

        /// <summary>
        /// Método público que crea los objetos que se pasan como parámetro en el array
        /// </summary>
        /// <param name="objetos">Objetos a crear</param>
        /// <returns>Devuelve una lista con tantos elementos, como se hayan intentado crear, indicando
        /// en cada posición si el elemento se ha insertado de forma correcta o no</returns>
        public SaveResult[] crearObjetos(sObject[] objetos, int longitudBloque_)
        {
            this.Sleep();
            return this.crearObjetosRepetir(objetos, true, longitudBloque_);
        }

        /// <summary>
        /// Método público  que borra los objetos en SalesForce que se pasan como parámetros en el array
        /// </summary>
        /// <param name="objetos">Objetos a borrar</param>
        /// <returns>Devuelve una lista con tantos elementos, como se hayan intentado borrar, indicando
        /// en cada posición si el elemento se ha borrado de forma correcta o no</returns>
        public DeleteResult[] borrarObjetos(String[] objetos, int longitudBloque_)
        {
            this.Sleep();
            return this.borrarObjetosRepetir(objetos, true, longitudBloque_);
        }

        /// <summary>
        /// Método público que modifica los objetos en SalesForce que se pasan como parámetro        
        /// </summary>
        /// <param name="objetos">Objetos a borrar</param>
        /// <returns>Devuelve una lista con tantos elementos, como se hayan intentado modificar, indicando
        /// en cada posición si el elemento se ha modificado de forma correcta o no</returns>
        public SaveResult[] modificarObjetos(sObject[] objetos, int longitudBloque_)
        {
            this.Sleep();
            return this.modificarObjetosrepetir(objetos, true, longitudBloque_);
        }

        /// <summary>
        /// Método público que unifica varios objetos en SalesForce que se pasan como parámetro        
        /// </summary>
        /// <param name="objetos">Objetos a unifucar</param>
        /// <returns>Devuelve una lista con tantos elementos, como se hayan intentado unificar, indicando
        /// en cada posición si el elemento se ha unificado de forma correcta o no</returns>
        public MergeResult[] mergeObjetos(MergeRequest[] objetos, int longitudBloque_)
        {
            this.Sleep();
            return this.mergeObjetosrepetir(objetos, true, longitudBloque_);
        }

        /// <summary>
        /// Método público que hace un upsert de los objetos en SalesForce que se pasan como parámetro        
        /// </summary>
        /// <param name="objetos">Objetos a borrar</param>
        /// <returns>Devuelve una lista con tantos elementos, como se hayan enviado para hacer upsert, indicando
        /// en cada posición si el elemento se ha modificado de forma correcta o no</returns>
        public UpsertResult[] upsertObjetos(sObject[] objetos, String externalField, int longitudBloque_)
        {
            this.Sleep();
            return this.upsertObjetosRepetir(objetos, externalField, true, longitudBloque_);
        }


        public sObject[] retrieveObjetos(string fields_, string type_, String[] ids_, int longitudBloque_)
        {
            this.Sleep();
            return this.retrieveObjetosRepetir(fields_, type_, true, ids_, longitudBloque_);
        }


        /// <summary>
        /// Método público que realiza la query en SalesForce que se pasan como parámetro        
        /// </summary>
        /// <param name="sql">Consulta a realizar</param>
        /// <returns>Devuelve una lista con tantos elementos, como cumplan la consulta que se ha lanzado</returns>
        public sObject[] query(String sql, ref QueryResult qr_)
        {
            this.Sleep();
            return this.queryRepetir(sql, ref qr_, true);
        }


        /// <summary>
        /// Método público que realiza el queryMore de la última consulta realizada en SalesForce
        /// </summary>
        /// <returns>Devuelve una lista con tantos elementos, como cumplan la consulta que se ha lanzado</returns>
        //public sObject[] queryMore()
        //{
        //    return this.queryMore();
        //}



        /// <summary>
        /// Método público que realiza una query de tipo count en SalesForce que se pasan como parámetro        
        /// </summary>
        /// <param name="sql">Consulta a realizar</param>
        /// <returns>Devuelve el número de registros que cumplen la consulta</returns>
        public int contarQuery(String sql)
        {
            this.Sleep();
            return this.contarQueryRepetir(sql, true);
        }

        /// <summary>
        /// Método público que recupera los objetos borrados en SalesForce 
        /// desde la fecha y del tipo que se pasan como parámetros
        /// </summary>
        /// <param name="objeto">Tipo de objeto</param>
        /// <param name="fechaInicio">Fecha desde la que se quiere recuperar los registros</param>
        /// <returns>Devuelve una lista con os identificadores de los registros eliminados</returns>
        public String[] obtenerBorrados(String objeto, DateTime fechaInicio)
        {
            this.Sleep();
            return this.obtenerBorradosRepetir(objeto, fechaInicio, true);
        }

        /// <summary>
        /// Método público que obtiene la descripcion de un objeto de Salesforce
        /// desde la fecha y del tipo que se pasan como parámetros
        /// </summary>
        /// <param name="objeto">Tipo de objeto</param>
        /// <returns>Devuelve la la descripción del objeto</returns>
        public DescribeSObjectResult describirObjeto(String objeto)
        {
            this.Sleep();
            return this.describirObjetoRepetir(objeto, true);
        }

        /// <summary>
        /// Método público que obtiene todos los objetos creados o modificados desde una fecha determinada hasta cinco minutos antes del momento de la operación
        /// desde la fecha y del tipo que se pasan como parámetros
        /// </summary>
        /// <param name="objeto">Tipo de objeto</param>
        /// <param name="fechaInicio">Fecha de inicio desde la que se quiere recuperar los registros</param>
        /// <returns>Array de String con los IDs de los objetos creados o modificados</returns>
        public String[] obtenerModificados(String objeto, DateTime fechaInicio)
        {
            this.Sleep();
            return this.obtenerModificadosRepetir(objeto, fechaInicio, true);
        }

        

        #endregion
    }
}


