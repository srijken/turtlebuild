using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace QQn.TurtleUtils.Streams
{
	/// <summary>
	/// The encoding of the webrequest data
	/// </summary>
	public enum WebRequestPostDataEncoding
	{
		/// <summary>
		/// Normal web post
		/// </summary>
		WwwFormUrlEncoded = 0,

		/// <summary>
		/// Multipart post; optimized for binary data
		/// </summary>
		MultipartFormData
	}
	/// <summary>
	/// Writer of form data for a <see cref="HttpWebRequest"/>
	/// </summary>
	public class WebFormWriter : IDisposable
	{
		readonly WebRequest _request;
		readonly WebRequestPostDataEncoding _encoding;
		Stream _webRequestStream;
		StreamWriter _streamWriter;

		static readonly string _topBoundary = Guid.NewGuid().ToString("N");
		static readonly string _subBoundary = Guid.NewGuid().ToString("N");
		const string NetworkEol = "\r\n";

		bool _nextField;

		/// <summary>
		/// Initializes a new instance of the <see cref="WebFormWriter"/> class with method POST
		/// </summary>
		/// <param name="request">The request.</param>
		/// <param name="encoding">The encoding.</param>
		public WebFormWriter(WebRequest request, WebRequestPostDataEncoding encoding)
			: this(request, encoding, "POST")
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WebFormWriter"/> class.
		/// </summary>
		/// <param name="request">The request.</param>
		/// <param name="encoding">The encoding.</param>
		/// <param name="method">The method.</param>
		public WebFormWriter(WebRequest request, WebRequestPostDataEncoding encoding, string method)
		{
			if (request == null)
				throw new ArgumentNullException("request");

			_request = request;
			_request.Method = "POST";
			_encoding = encoding;

			switch(encoding)
			{
				case WebRequestPostDataEncoding.WwwFormUrlEncoded:
					request.ContentType = "application/x-www-form-urlencoded";
					break;
				case WebRequestPostDataEncoding.MultipartFormData:
					request.ContentType = string.Format("multipart/form-data; boundary={0}", _topBoundary);
					break;
				default:
					throw new ArgumentOutOfRangeException("encoding", encoding, "Unknown encoding");
			}

			_webRequestStream = request.GetRequestStream();
			_streamWriter = new StreamWriter(_webRequestStream, Encoding.ASCII);
		}

		/// <summary>
		/// Writes the boundary.
		/// </summary>
		void WriteBoundary()
		{
			switch (_encoding)
			{
				case WebRequestPostDataEncoding.MultipartFormData:
					_streamWriter.Write("--");
					_streamWriter.Write(_topBoundary);
					_streamWriter.Write(NetworkEol);
					break;
				default:
					throw new InvalidOperationException(string.Format("WriteBoundary() is not supported for encoding {0}", _encoding));
			}
		}


		/// <summary>
		/// Adds the value.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		public void AddValue(string key, string value)
		{
			if (string.IsNullOrEmpty(key))
				throw new ArgumentNullException("key");
			else if (string.IsNullOrEmpty(value))
				throw new ArgumentNullException("value");

			switch (_encoding)
			{
				case WebRequestPostDataEncoding.WwwFormUrlEncoded:
					if (_nextField)
						_streamWriter.Write('&');
					else
						_nextField = true;

					WriteUrlEncoded(key);
					_streamWriter.Write('=');
					WriteUrlEncoded(value);
					break;
				case WebRequestPostDataEncoding.MultipartFormData:
					AddValue(key, Encoding.UTF8.GetBytes(value));
					break;

				default:
					throw new InvalidOperationException(string.Format("AddFile(String, String) is not supported for encoding {0}", _encoding));
			}
		}

		/// <summary>
		/// Adds the specified key-value-pair
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		public void AddValue(string key, byte[] value)
		{
			if (string.IsNullOrEmpty(key))
				throw new ArgumentNullException("key");
			else if (value == null)
				throw new ArgumentNullException("value");

			switch (_encoding)
			{				
				case WebRequestPostDataEncoding.WwwFormUrlEncoded:
					if (!IsSafeKeyName(key))
						throw new FormatException("Invalid character in key");

					WriteBoundary();
					_streamWriter.Write("Content-Disposition: form-data; name=\"{0}\"", key);
					_streamWriter.Write(NetworkEol);
					_streamWriter.Write(NetworkEol);

					_webRequestStream.Write(value, 0, value.Length);
					_streamWriter.Write(NetworkEol);
					break;
				default:
					throw new InvalidOperationException(string.Format("AddValue(String, Byte[]) is not supported for encoding {0}", _encoding));
			}

			
		}


		bool IsSafeKeyName(string key)
		{
			foreach (char c in key)
			{
				if (!char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
					return false;
			}
			return true;
		}

		void WriteUrlEncoded(string value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			for (int i = 0; i < value.Length; i++)
			{
				if (char.IsLetterOrDigit(value, i))
					_streamWriter.Write(value[i]);
				else switch (value[i])
					{
						case ' ':
							_streamWriter.Write('+');
							break;
						default:
							foreach (byte b in _streamWriter.Encoding.GetBytes(new char[] { value[i] }))
							{
								_streamWriter.Write("%{0:X2}", b);
							}
							break;
					}
			}
		}

		/// <summary>
		/// Adds the specified file with the application/octet-stream content-type
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="filename">The filename.</param>
		public void AddFile(string key, string filename)
		{
			AddFile(key, filename, "application/octet-stream");
		}

		/// <summary>
		/// Adds the specified file with the specified content-type
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="filename">The filename.</param>
		/// <param name="contentType">Type of the content.</param>
		public void AddFile(string key, string filename, string contentType)
		{
			if (string.IsNullOrEmpty(key))
				throw new ArgumentNullException("key");
			else if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException("filename");
			else if (string.IsNullOrEmpty(contentType))
				throw new ArgumentNullException("contentType");

			using (FileStream fs = File.OpenRead(filename))
			{
				AddFile(key, filename, fs, contentType);
			}
		}

		/// <summary>
		/// Adds the stream with the specified filename and content-type
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="filename">The filename.</param>
		/// <param name="stream">The stream.</param>
		/// <param name="contentType">Type of the content.</param>
		public void AddFile(string key, string filename, Stream stream, string contentType)
		{
			if (string.IsNullOrEmpty(key))
				throw new ArgumentNullException("key");
			else if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException("filename");
			else if (string.IsNullOrEmpty(contentType))
				throw new ArgumentNullException("contentType");

			filename = Path.GetFileName(filename);

			switch (_encoding)
			{
				case WebRequestPostDataEncoding.MultipartFormData:
					if (!IsSafeKeyName(key))
						throw new FormatException("Invalid character in key");

					WriteBoundary();
					_streamWriter.Write("Content-Disposition: form-data; name=\"{0}\" filename=\"{1}\"", key, filename);
					_streamWriter.Write(NetworkEol);
					_streamWriter.Write("Content-Type: {0}", contentType);
					_streamWriter.Write(NetworkEol);
					_streamWriter.Write(NetworkEol);

					byte[] buffer = new byte[8192];
					int read;

					while (0 < (read = stream.Read(buffer, 0, buffer.Length)))
					{
						_webRequestStream.Write(buffer, 0, read);
					}

					_streamWriter.Write(NetworkEol);
					break;
				default:
					throw new InvalidOperationException(string.Format("AddFile is not supported for encoding {0}", _encoding));
			}
		}

		#region IDisposable Members

		void IDisposable.Dispose()
		{
			Close();
		}

		/// <summary>
		/// Closes the writer and the RequestStream of the <see cref="WebRequest"/>
		/// </summary>
		public virtual void Close()
		{
			_streamWriter = null;
			if (_webRequestStream != null)
			{
				_webRequestStream.Close();
				_webRequestStream = null;
			}
		}

		#endregion
	}
}