using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NancyApplication1
{
    public class Class1 : NancyModule
    {
        public Class1()
        {
            Get["/"] = _ => "Hello, world!";
        }
    }
}