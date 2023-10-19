///<Autor>Francisco Javier Cabezas</Autor>
///<Modificador><Modificador>
///<Fecha Creacion>24-03-2009</Fecha Creacion>
///<Fecha Modificacion></Fecha Modificacion>
using System;
using TATOR.partner;
//using IntegrationLib.sForce;

namespace TATOR.Controladores
{
    // we extend the generated proxy class, and override the GetWebRequest to get the request compressed
    // its that easy!, we also turn on the base class supplied support to handle response compression
    class SforceGzip : SforceService
    {
        public SforceGzip()
        {
            this.EnableDecompression = true;
        }

        protected override System.Net.WebRequest GetWebRequest(Uri uri)
        {
            return new GzipWebRequest(base.GetWebRequest(uri));
        }
    }
}
