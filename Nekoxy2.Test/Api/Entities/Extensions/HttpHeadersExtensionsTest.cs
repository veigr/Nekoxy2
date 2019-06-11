using Nekoxy2.Entities.Http;
using Nekoxy2.Entities.Http.Delegations;
using Nekoxy2.Entities.Http.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.Api.Entities.Extensions
{
    public class HttpHeadersExtensionsTest
    {
        [Fact]
        public void HasHeaderTest()
        {
            var headers = CreateTestHeader();
            headers.HasHeader("set-cookie").IsTrue();
            headers.HasHeader("hoge").IsFalse();
        }

        [Fact]
        public void FirstValueTest()
        {
            var headers = CreateTestHeader();
            headers.GetFirstValue("set-cookie").Is("Hoge");
        }

        [Fact]
        public void GetValuesTest()
        {
            var headers = CreateTestHeader();
            headers.GetValues("set-cookie").ToArray().Is(new[] { "Hoge", "Fuga" });
        }

        static IReadOnlyHttpHeaders CreateTestHeader()
        {
            var source =
@"Set-Cookie: Hoge
Set-Cookie: Fuga

";
            Nekoxy2.ApplicationLayer.Entities.Http.HttpHeaders.TryParse(source, out var headers);

            return ReadOnlyHttpHeaders.Convert(headers);
        }
    }
}
