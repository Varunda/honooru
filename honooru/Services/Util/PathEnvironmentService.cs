﻿using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace honooru.Services.Util {

    public class PathEnvironmentService {

        private readonly ILogger<PathEnvironmentService> _Logger;

        public PathEnvironmentService(ILogger<PathEnvironmentService> logger) {
            _Logger = logger;
        }

        /// <summary>
        ///     search the PATH environmental variable for an executable. do not include .exe at the end
        /// </summary>
        /// <param name="name">name of the executable to search for</param>
        /// <returns>the full path to the executable, or <c>null</c> if it does not exist</returns>
        /// <exception cref="Exception"></exception>
        public string? FindExecutable(string name) {
            if (name.EndsWith(".exe")) {
                throw new Exception($"when finding an executable, do not include a trailing '.exe'");
            }

            string? path = Environment.GetEnvironmentVariable("PATH");
            if (path == null) {
                _Logger.LogError($"missing PATH env var");
                return null;
            }

            // add the .exe to windows stuff 
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                name += ".exe";
            }

            foreach (string dir in path.Split(Path.PathSeparator)) {
                string fullPath = Path.Combine(dir, name);
                if (File.Exists(fullPath)) {
                    _Logger.LogInformation($"found executable [name={name}] [fullPath={fullPath}]");
                    return fullPath;
                }
            }

            return null;
        }

    }
}
