﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Security;
using Application;
using Core.Common;
using Newtonsoft.Json;
using Core.Entity;

namespace WebApi.Authorize
{
    /// <summary>
    /// 自定义此特性用于接口的身份验证
    /// </summary>
    public class RequestAuthorizeAttribute : AuthorizeAttribute
    {
        //重写基类的验证方式，加入我们自定义的Ticket验证
        public override void OnAuthorization(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            //是否允许匿名访问，则返回未验证401
            var attributes = actionContext.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().OfType<AllowAnonymousAttribute>();
            bool isAnonymous = attributes.Any(a => a is AllowAnonymousAttribute);
            Log.Info("验证开始！");
            if (isAnonymous)
            {
                base.OnAuthorization(actionContext);
            }
            else
            {
                //从http请求的头里面获取身份验证信息，验证是否是请求发起方的ticket
                var authorization = actionContext.Request.Headers.Authorization;
                if ((authorization != null) && (authorization.Parameter != null))
                {
                    //解密用户ticket,并校验用户名密码是否匹配
                    var encryptTicket = authorization.Parameter;
                    LoginService _loginService = new LoginService();
                    Log.Info("请求头验证开始！" + encryptTicket);
                    if (_loginService.WeChatLogin(encryptTicket))
                    {
                        Log.Info("请求头验证通过！"+ encryptTicket);
                        base.IsAuthorized(actionContext);
                    }
                    else
                    {
                        HandleUnauthorizedRequest(actionContext);
                    }
                }
                else
                {
                    HandleUnauthorizedRequest(actionContext);
                }
            }
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext filterContext)
        {
            base.HandleUnauthorizedRequest(filterContext);

            var response = filterContext.Response = filterContext.Response ?? new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.Forbidden;
            response.Content = new StringContent(JsonConvert.SerializeObject(new Result { success = false, message = "服务端拒绝访问：你没有权限，或者掉线了" }),
                Encoding.UTF8, "application/json");
        }
    }
}