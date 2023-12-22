using _1.FlappyBirdClone;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

var logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger<Program>();
logger.LogInformation("Game has started.");

var app = new FlappyBirdCloneApp();
app.Run();