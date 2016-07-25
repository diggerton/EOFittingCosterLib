using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.IO;
using System.Net;
using eZet.EveLib.EveCentralModule;

namespace EOFittingCoster.Tests
{
    [TestClass]
    public class I_BAD_CODER
    {
        [TestMethod]
        public void MyTestMethod()
        {
            EveCentral c = new EveCentral();
            var results = c.GetMarketStat(new EveCentralOptions { Items = new int[] { 23757 } });

        }
    }
}
