using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sprache;
using Parser = MultipartParser.Parser;

namespace MultipartParser.Data
{
    public class Message
    {
        public Dictionary<string, string> Headers { get; private set; }
        public ContentType ContentType { get; private set; }
        public string Body { get; private set; }

        public Message(Dictionary<string, string> headers, string body)
        {
            this.Headers = headers;
            this.Body = body;

            if (Headers.ContainsKey("Content-Type"))
            {
                this.ContentType = ContentType.Make(Headers["Content-Type"]);
            }
        }
    }

    public class ContentType
    {
        public Dictionary<string, string> Attrs { get; private set; }
        public string MediaType { get; private set; }
        public string MediaSubType { get; private set; }

        public ContentType(string mediaType, string mediaSubType, Dictionary<string, string> attrs)
        {
            this.MediaType = mediaType;
            this.MediaSubType = mediaSubType;
            this.Attrs = attrs;
        }

        public string this[string key]
        {
            get
            {
                return Attrs[key];
            }
        }

        public static ContentType Make(string contentType)
        {
            return Parser.ContentType.Parse(contentType);
        }

        public override string ToString()
        {
            return MediaType + "/" + MediaSubType
                + Attrs.Aggregate("", (cur, next) => cur + "; " + next.Key + "=\"" + next.Value + "\"");
        }
    }
}
