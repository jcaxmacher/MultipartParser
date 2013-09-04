using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sprache;
using Parser = MultipartParser.Parser;

namespace MultipartParser
{
    [TestFixture]
    class Tests
    {
        [Test]
        public void QuotedStringTest()
        {
            string input = "\"This is a test\"";
            string output = Parser.InnerString.Parse(input);
            Assert.AreEqual("This is a test", output);
        }

        [Test]
        public void TokenTest()
        {
            string input = "asdf-asdf";
            string output = Parser.TokenString.End().Parse(input);
            Assert.AreEqual("asdf-asdf", output);
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void NotTokenTest()
        {
            string input = "asdf asdf";
            string output = Parser.TokenString.End().Parse(input);
        }

        [Test]
        public void MediaTypeTest()
        {
            string input = "application/xml";
            var output = Parser.MediaType.End().Parse(input);
            Assert.AreEqual("application", output.Item1);
            Assert.AreEqual("xml", output.Item2);
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void NotMediaTypeTest()
        {
            string input = "appl ication/xml";
            var output = Parser.MediaType.End().Parse(input);
            Assert.AreEqual("application", output.Item1);
            Assert.AreEqual("xml", output.Item2);
        }

        [Test]
        public void MediaAttributeTest()
        {
            string input = "; asdf=\"The bomb\"";
            var output = Parser.MediaAttribute.End().Parse(input);
            Assert.AreEqual("asdf", output.Item1);
            Assert.AreEqual("The bomb", output.Item2);
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void NotMediaAttributeTest()
        {
            string input = "; appl ication=asdf";
            var output = Parser.MediaAttribute.End().Parse(input);
            Assert.AreEqual("application", output.Item1);
            Assert.AreEqual("asdf", output.Item2);
        }

        [Test]
        public void ContentTypeTest()
        {
            string input = "Multipart/Related; boundary=\"uuid:c73c9ce8-6e02-40ce-9f68-064e18843428\"; testkey=asdf:/testvalue";
            Data.ContentType output = Parser.ContentType.End().Parse(input);
            Assert.AreEqual("Multipart", output.MediaType);
            Assert.AreEqual("Related", output.MediaSubType);
            Assert.Contains("boundary", output.Attrs.Keys);
            Assert.True(output.Attrs.Count == 2);
            Assert.AreEqual("uuid:c73c9ce8-6e02-40ce-9f68-064e18843428", output.Attrs["boundary"]);
        }

        [Test]
        public void FullContentTest()
        {
            string contentTypeData = "Multipart/Related; boundary=\"uuid:c73c9ce8-6e02-40ce-9f68-064e18843428\"; testkey=asdf:/testvalue";
            Data.ContentType contentType = Parser.ContentType.End().Parse(contentTypeData);
            string input = @"--uuid:c73c9ce8-6e02-40ce-9f68-064e18843428
Content-Id: <rootpart*c73c9ce8-6e02-40ce-9f68-064e18843428@example.jaxws.sun.com>
Content-Type: application/xop+xml;charset=utf-8;type=text/xml
Content-Transfer-Encoding: binary
 
<?xml version=1.0 ?>
  <S:Envelope xmlns:S=http://schemas.xmlsoap.org/soap/envelope/>
     <S:Body>
	<ns2:downloadImageResponse xmlns:ns2=http://ws.mkyong.com/>
	  <return>
	    <xop:Include xmlns:xop=http://www.w3.org/2004/08/xop/include
		href=cid:012eb00e-9460-407c-b622-1be987fdb2cf@example.jaxws.sun.com>
	    </xop:Include>
	  </return>
	</ns2:downloadImageResponse>
     </S:Body>
   </S:Envelope>
--uuid:c73c9ce8-6e02-40ce-9f68-064e18843428
Content-Id: <012eb00e-9460-407c-b622-1be987fdb2cf@example.jaxws.sun.com>
Content-Type: image/png
Content-Transfer-Encoding: binary

data
--uuid:c73c9ce8-6e02-40ce-9f68-064e18843428--";
            List<Data.Message> messages = Parser.Messages(contentType["boundary"]).Parse(input);
            Assert.True(messages.Count == 2);
            Assert.AreEqual("png", messages[1].ContentType.MediaSubType);
            Assert.True(messages[0].ContentType.Attrs.Count == 2);
            Assert.AreEqual(415, messages[0].Body.Length);
        }
    }
}
