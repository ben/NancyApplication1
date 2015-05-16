﻿using Nancy;
using Octokit;
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
            Get["/", true] = async (x,ct) => {
                var client = new GitHubClient(new ProductHeaderValue("MyHello"));
                var user = await client.User.Get("ben");
                return String.Format("{0} people love {1}!", user.Followers, user.Name);
            };
        }
    }
}