using DiscordBotHealthUpdate;

if (args.Length != 1)
{
    throw new ArgumentException("Wrong number of arguments");
}

HealthReporter healthReporter = new HealthReporter(args[0]);

await healthReporter.Start();