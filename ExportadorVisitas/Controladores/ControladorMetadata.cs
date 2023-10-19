using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TATOR.metadata;

namespace TATOR.Controladores
{
    public class ControladorMetadata
    {

        private static ControladorMetadata _instancia;

        private MetadataService metadata;

        
        public static ControladorMetadata instancia
        {
            get
            {
                if (ControladorMetadata._instancia == null)
                    ControladorMetadata._instancia = new ControladorMetadata();
                return ControladorMetadata._instancia;
            }
        }


        private ControladorMetadata()
        {
            metadata = new metadata.MetadataService();
            metadata.Url = ControladorSalesforce.instancia.metadataURL;
            metadata.SessionHeaderValue = new metadata.SessionHeader();
            metadata.SessionHeaderValue.sessionId = ControladorSalesforce.instancia.binding.SessionHeaderValue.sessionId;

        }


        public void inicializar()
        {
            metadata = new metadata.MetadataService();
            metadata.Url = ControladorSalesforce.instancia.metadataURL;
            metadata.SessionHeaderValue = new metadata.SessionHeader();
            metadata.SessionHeaderValue.sessionId = ControladorSalesforce.instancia.binding.SessionHeaderValue.sessionId;

        }


        public List<String> obtenerListaObjetos()
        {
            var resultados = new List<string>();
            ListMetadataQuery query = new ListMetadataQuery();
            query.type = "CustomObject";
            double asOfVersion = 31.0;

            var results = metadata.listMetadata(new ListMetadataQuery[] { query }, asOfVersion);

            foreach (var result in results)
            {
                resultados.Add(result.fullName);
            }
            return resultados;

        }

        public List<string> obtenerListaCampos(string objeto)
        {
            var resultados = new List<string>();
            metadata.Timeout = 600000;
            var results = metadata.readMetadata("CustomObject", new String[] { objeto });

            foreach (var result in ((CustomObject)(results[0])).fields)
            {
                resultados.Add(result.fullName);
            }

            resultados.Add("Id");
            return resultados;

        }




    }
}
