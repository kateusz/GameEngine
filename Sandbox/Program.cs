using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

var logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger<Program>();
logger.LogInformation("Program has started.");

var sandbox = new global::Sandbox.Sandbox();
sandbox.Run();