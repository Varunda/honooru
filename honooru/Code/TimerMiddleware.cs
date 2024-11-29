﻿using honooru.Code.ExtensionMethods;
using honooru.Models;
using honooru.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace honooru.Code {

    public class TimerMiddleware {

        private readonly RequestDelegate _Next;
        private readonly ILogger<TimerMiddleware> _Logger;
        private readonly AppCurrentAccount _CurrentUser;

        public TimerMiddleware(RequestDelegate next, ILogger<TimerMiddleware> logger,
            AppCurrentAccount currentUser) {

            _Next = next;

            _Logger = logger;
            _CurrentUser = currentUser;
        }

        public async Task InvokeAsync(HttpContext context) {
            Stopwatch timer = Stopwatch.StartNew();
            await _Next(context);
            long timerMs = timer.ElapsedMilliseconds;

            string? controllerName = context.Request.RouteValues["controller"]?.ToString();
            string? actionName = context.Request.RouteValues["action"]?.ToString();
            string url = context.Request.Method + " " + context.Request.GetDisplayUrl();

            List<string> parameters = [];
            foreach (KeyValuePair<string, object?> iter in context.Request.RouteValues) {
                // skip these
                if (iter.Key == "controller" || iter.Key == "action" || iter.Key == ".") {
                    continue;
                }

                string param = $"[{iter.Key}=";

                object? value = iter.Value;
                if (value == null) {
                    param += "null";
                } else if (value is string str) {
                    param += $"'{str.Truncate(100)}'";
                } else if (value is int i) {
                    param += i;
                } else {
                    _Logger.LogWarning($"unchecked type of parameter passed [name={iter.Key}] [type name={value.GetType().FullName}]");
                }

                param += "]";
                parameters.Add(param);
            }

            AppAccount? currentUser = await _CurrentUser.Get();

            string paramStr = parameters.Count == 0 ? "" : $"{string.Join(" ", parameters)}"; 
            _Logger.LogInformation($"http request complete [url={url}] [status={context.Response.StatusCode}] [timer={timerMs}ms] [controller={controllerName}] [action={actionName}]"
                + $" [user={currentUser?.Name}/{currentUser?.ID}] {paramStr}");
        }


    }
}
