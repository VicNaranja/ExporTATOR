///<Autor>Francisco Javier Cabezas</Autor>
///<Modificador><Modificador>
///<Fecha Creacion>24-03-2009</Fecha Creacion>
///<Fecha Modificacion></Fecha Modificacion>
using System;
using System.Net;
using System.IO;
using System.IO.Compression;


namespace TATOR.Controladores
{
	/// <summary>
	/// This class wraps an existing WebRequest, and will handle compressing the request. The framework provided
    /// code will handle uncompressing the response.
	/// 
	/// To use this with a Soap Client, create a new class that derives from the WSDL generated class and override GetWebRequest, 
	/// its implementation should simply be 
	///		return new GzipWebRequest(base.GetWebRequest(uri));
	/// 
	/// Then when using the web service, remember to create instances of the derived class, rather than the generated class
    /// This approach allows you to add compression support without having to change any generated code.
	/// </summary>
	public class GzipWebRequest : WebRequest
	{
		/// <summary>
		/// Construct an WebRequest wrapper that gzip compresses the request stream.
		/// </summary>
		/// <param name="wrappedRequest">The WebRequest we're wrapping.</param>
		public GzipWebRequest(WebRequest wrappedRequest)
		{
			this.wr = wrappedRequest;
			wr.Headers["Content-Encoding"] = "gzip";
		}

		private WebRequest wr;
        private Stream request_stream = null;

        // most of these just delegate to the contained WebRequest
		public override string Method
		{
			get { return wr.Method; }
			set { wr.Method = value; }
		}
	
		public override Uri RequestUri
		{
			get { return wr.RequestUri; }
		}
	
		public override WebHeaderCollection Headers
		{
			get { return wr.Headers; }
			set { wr.Headers = value; }
		}
	
		public override long ContentLength
		{
			get { return wr.ContentLength; }
			set { wr.ContentLength = value; }
		}
	
		public override string ContentType
		{
			get { return wr.ContentType; }
			set { wr.ContentType = value; }
		}
	
		public override ICredentials Credentials
		{
			get { return wr.Credentials; }
			set { wr.Credentials = value; }
		}
	
		public override bool PreAuthenticate
		{
			get { return wr.PreAuthenticate; }
			set { wr.PreAuthenticate = value; }
		}
	
		public override System.IO.Stream GetRequestStream()
		{
			return WrappedRequestStream(wr.GetRequestStream());
		}
	
		public override IAsyncResult BeginGetRequestStream(AsyncCallback callback, object state)
		{
			return wr.BeginGetRequestStream (callback, state);
		}
	
		public override System.IO.Stream EndGetRequestStream(IAsyncResult asyncResult)
		{
			return WrappedRequestStream(wr.EndGetRequestStream (asyncResult));
		}
	
		/// <summary>
		/// helper function that wraps the request stream in a GZipStream
		/// </summary>
		/// <param name="requestStream">The Stream we're compressing</param>
		/// <returns></returns>
		private Stream WrappedRequestStream(Stream requestStream)
		{
			if ( request_stream == null )
				request_stream = new GZipStream(requestStream, CompressionMode.Compress);
			return request_stream;
		}

		public override WebResponse GetResponse()
		{
			return wr.GetResponse();
		}
	
		public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
		{
			return wr.BeginGetResponse (callback, state);
		}
	
		public override WebResponse EndGetResponse(IAsyncResult asyncResult)
		{
			return wr.EndGetResponse (asyncResult);
		}
	}
}
