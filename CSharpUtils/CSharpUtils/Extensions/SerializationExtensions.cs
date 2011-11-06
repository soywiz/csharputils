﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace CSharpUtils.Extensions
{
	static public class SerializationExtensions
	{
		static public String ToJson<T>(this T Object)
		{
			return Json.JSON.Encode(Object);
		}

		static public String ToXmlString<T>(this T Struct)
		{
			var Serializer = new XmlSerializer(typeof(T));
			var StringWriter = new StringWriter();
			Serializer.Serialize(StringWriter, Struct);
			return StringWriter.ToString();
			//return Str
		}

		static public T FromXmlString<T>(this String XmlString)
		{
			var Serializer = new XmlSerializer(typeof(T));
			return (T)Serializer.Deserialize(new StringReader(XmlString));
		}
	}
}