﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using honooru.Code.Converters;
using System.Text.Json.Serialization.Metadata;

namespace honooru.Models {

    /// <summary>
    ///     Represents the base object all responses will follow
    /// </summary>
    public class ApiResponse : IActionResult {

        /// <summary>
        ///     Status code of the result
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        ///     Data of the response
        /// </summary>
        public object? Data { get; set; }

        public ApiResponse(int status, object? data) {
            Status = status;
            Data = data;
        }

        public Task ExecuteResultAsync(ActionContext context) {
            IActionResultExecutor<ApiResponse> executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<ApiResponse>>();
            return executor.ExecuteAsync(context, this);
        }

    }

    /// <summary>
    ///     Parameterized base response to an API request
    /// </summary>
    /// <typeparam name="T">Type to expect <see cref="Data"/> to be. However, this is not enforced</typeparam>
    public class ApiResponse<T> : IActionResult {

        /// <summary>
        ///     Status code of the result
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        ///     Data of the response
        /// </summary>
        public object? Data { get; set; }

        public ApiResponse(int status, object? data) {
            Status = status;
            Data = data;
        }

        public Task ExecuteResultAsync(ActionContext context) {
            IActionResultExecutor<ApiResponse> executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<ApiResponse>>();
            ApiResponse response = new ApiResponse(Status, Data);
            return executor.ExecuteAsync(context, response);
        }
    }


    /// <summary>
    ///     <see cref="IActionResultExecutor{TResult}"/> that is executed when an <see cref="ApiResponse"/> is
    ///     returned from a controller
    /// </summary>
    public class ApiResponseExecutor : IActionResultExecutor<ApiResponse> {

        private readonly ILogger _Logger;

        private readonly Func<Stream, Encoding, TextWriter> _Writer;

        private static readonly JsonSerializerOptions _JsonOptions = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        static ApiResponseExecutor() {
            _JsonOptions.Converters.Add(new DateTimeJsonConverter());
        }

        public ApiResponseExecutor(ILoggerFactory logger, IHttpResponseStreamWriterFactory writerFactory) { 
            _Logger = logger.CreateLogger<ApiResponseExecutor>();

            _Writer = writerFactory.CreateWriter;
        }

        public Task ExecuteAsync(ActionContext context, ApiResponse result) {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(result);

            OutputFormatterWriteContext formatterContext = new(context.HttpContext, _Writer, typeof(object), result.Data!); // Force is safe, cause null is a valid object

            SystemTextJsonOutputFormatter formatter = new(_JsonOptions);//, ArrayPool<char>.Shared);

            if (context.HttpContext.Response.HasStarted == true) {
                _Logger.LogError("Response to {ConnectionID} at {URL} has already started. From {IPv4}",
                    context.HttpContext.Connection.Id,
                    $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path.Value}",
                    context.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "<no remote ip>");
            }

            context.HttpContext.Response.StatusCode = result.Status;

            // 201 Created
            if (result.Status == 201 && result.Data == null) {
                throw new InvalidOperationException(
                    $"Cannot have null {nameof(result.Data)} when the {nameof(result.Status)} is 201 Created");
            }

            // 302 Found
            if (result.Status == 302) {
                if (result.Data is not string ss) {
                    throw new InvalidOperationException($"Cannot have a non-string {nameof(result.Data)} when {nameof(result.Status)} is 302 Found");
                } else {
                    context.HttpContext.Response.Headers.Location = ss;
                }
            }

            if (result.Status != 204) { // It is an error to write to a 204
                return formatter.WriteAsync(formatterContext);
            }

            return Task.CompletedTask;
        }

    }

}
