///<Autor>Francisco Javier Cabezas</Autor>
///<Modificador><Modificador>
///<Fecha Creacion>24-03-2009</Fecha Creacion>
///<Fecha Modificacion></Fecha Modificacion>
using System;
using System.Collections.Generic;
using System.Text;

namespace TATOR.Controladores
{
    public class Constantes
    {
        public static int NUMERO_MAXIMO_REGISTROS_SALESFORCE = 200;
        public static int NUMERO_MAXIMO_REGISTROS_SALESFORCE_PEDIDOS = 100;
        public static int NUMERO_MAXIMO_REGISTROS_SALESFORCE_QUERY = 1000;
        public static int NUMERO_MAXIMO_REGISTROS_SALESFORCE_RETRIEVE = 2000;
        
        public static int TIME_OUT_DEFECTO = 100000;

        public static int LONGITUD_CAMPO_MENSAJE_ERROR = 255;
    
      

        public static string ACCESS_READWRITE ="Edit";

        public static string TITTLE = "Proceso de integración de Iberostar";

        public static string SESION_RECORDTYPEID = "01220000000DZIf";

        //public static string VISITA_RECORDTYPEID = "01220000000DoSw";
        //public static string RESERVA_RECORDTYPEID = "01220000000DoSr";
        public static string VISITA_RECORDTYPE = "Visita";
        public static string RESERVA_RECORDTYPE = "Reserva";


         //     * Division_produccion:    
         //America: 02dD0000000CaosIAC
         //Europa:  02dD0000000CaoxIAC
        //Global:  02dD0000000Kyn4IAC
 
        //Division_Sandbox:
        //America: 02dM00000004CDKIA2
        //Europa:  02dM00000004CDPIA2
        //Global:  02dM0000000CaVYIA0

        public static string GESTORA_AM = "AM";
        public static string GESTORA_EU = "EU";
        



    }
}



