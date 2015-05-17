using Nancy;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NancyApplication1
{
    public class Handler : NancyModule
    {
        private const string clientId = "be8f08cb9a0471772f88";
        private const string clientSecret = "41f67b9c2ec542449b83039aed56b64a56bf523b";
        private readonly GitHubClient client = new GitHubClient(new ProductHeaderValue("MyHello"));

        public Handler()
        {
            Get["/", true] = async (x, ct) =>
            {
                var user = await client.User.Get("ben");
                return String.Format("{0} people love {1}!", user.Followers, user.Name);
            };

            Get["/{user}/{repo}/{sha}", true] = async (parms, ctx) =>
            {
                var accessToken = Session["accessToken"] as string;
                if (string.IsNullOrEmpty(accessToken))
                    return RedirectToOAuth();
                client.Credentials = new Credentials(accessToken);

                try
                {
                    var status = await client.Repository.CommitStatus.GetCombined(
                        parms.user, parms.repo, parms.sha);
                    return status.State.ToString();
                }
                catch (NotFoundException)
                {
                    return HttpStatusCode.NotFound;
                }
            };

            Get["/{user}/{repo}/{sha}/{status}", true] = async (parms, ctx) =>
            {
                var accessToken = Session["accessToken"] as string;
                if (string.IsNullOrEmpty(accessToken))
                    return RedirectToOAuth();
                client.Credentials = new Credentials(accessToken);

                CommitState newState = Enum.Parse(typeof(CommitState), parms.status, true);
                try
                {
                    CommitStatus newStatus = await client.Repository.CommitStatus.Create(
                        parms.user, parms.repo, parms.sha, new NewCommitStatus
                        {
                            State = newState,
                            TargetUrl = new Uri(Request.Url.SiteBase),
                            Context = "arbitrary",
                        });
                    return newStatus;
                }
                catch (NotFoundException)
                {
                    return HttpStatusCode.NotFound;
                }
                return String.Format("https://api.github.com/repos/{0}/{1}/commits/{2}/status",
                    parms.user, parms.repo, parms.sha);
            };

            Get["/authorize", true] = async (parms, ctx) =>
            {
                var csrf = Session["CSRF:State"] as string;
                Session.Delete("CSRF:State");
                if (csrf != Request.Query["state"])
                {
                    return HttpStatusCode.Unauthorized;
                }

                var token = await client.Oauth.CreateAccessToken(
                    new OauthTokenRequest(clientId, clientSecret, Request.Query["code"].ToString())
                    {
                        RedirectUri = new Uri(Request.Url.SiteBase + "/authorize")
                    });
                Session["accessToken"] = token.AccessToken;

                var origUrl = Session["OrigUrl"].ToString();
                Session.Delete("OrigUrl");
                return Response.AsRedirect(origUrl);
            };
        }

        private Response RedirectToOAuth()
        {
            var csrf = Guid.NewGuid().ToString();
            Session["CSRF:State"] = csrf;
            Session["OrigUrl"] = this.Request.Path;

            var request = new OauthLoginRequest(clientId)
            {
                Scopes = { "repo:status" },
                State = csrf
            };
            var oauthLoginUrl = client.Oauth.GetGitHubLoginUrl(request);
            return Response.AsRedirect(oauthLoginUrl.ToString());
        }
    }
}