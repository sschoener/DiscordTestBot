# Installation
 1. Clone the repository.
 2. Make sure that you have .NET Core 2.1.201 installed. You can install it from [here](https://github.com/dotnet/core/blob/master/release-notes/download-archives/2.1.201-sdk-download.md). Make sure to install the SDK version!
 3. Within the repository, run `dotnet restore`. This will try to set up the environment.
 4. Execute `dotnet build` to ensure that the program builds as expected.
 5. Make sure to add your bot's discord token to the `configuration.json` file. **Do NOT publish it somewhere with your token in it. Do NOT commit to this repository with the token in it.**
 6. Start the program using `dotnet run`.

Note that this repository is using C# 7.3 and thus supports features that you might not be familiar with from, say, Unity development.

Also make sure that you give the bot full administrator privileges on the server you want to use it on. This is no general requirement, but the currently implemented commands assume that they can freely delete the messages that triggered them.

# Overview
Before starting to look into this repository, you should make sure that you are familiar with `async/await` in C#. A good learning resource is [this](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/index) combined with [this](https://docs.microsoft.com/en-us/dotnet/standard/async-in-depth). If you are still unsure about it, consult your senior engineer ;)
This project uses [Discord.Net](https://github.com/RogueException/Discord.Net).

The general control flow of the program is as follows:
1. In `Startup.cs`, the program loads the configuration file to read the token that the bot uses to log in.
2. It then sets up a few *services*. These are persistent facilities that allow the program to do their job. Currently, there are facilities for logging, connecting to discord, and command handling. We will get back to them in a second.
3. It initializes the connection to Discord via the call `provider.GetRequiredService<StartupService>().StartAsync();`. This call loads all *modules* of the Discord bot.
4. After this, the `CommandHandlingService` class reacts to incoming messages and forwards them to a subsystem provided by Discord.Net that automatically maps text commands to functions in modules.

## Services and Modules
There are two central notions: *Services* and *Modules*.
Services are persistent and run for the lifetime of the program. They can be automatically distributed to other parts of the program using dependency injection (see below).
Modules are a Discord.Net specific feature to handle commands (see [documentation here](https://discord.foxbot.me/docs/guides/commands/commands.html)). A simple module could look like this:

```csharp
public class EchoModule : ModuleBase {

    [Command("echo"), Summary("Echos a message.")]
    public async Task Echo([Remainder, Summary("The text to echo")] string echo)
    {
        await ReplyAsnc(Context.Username + " says: " + echo);
    }

    [Command("square")]
    public async Task Square(int x) {
        await ReplyAsync((x * x).ToString());
    }
}
```

By using attributes, you can designate which methods should react to which commands. The arguments of commands are automatically parsed according to the parameters of the method. Each instance of a module lives only for as long as it is answering a single request. Any persistent data should be accessed via services. The `Context` member of the module is automatically set to a value that describes the message that was received plus some, well, *context*. You can use dependency injection to automatically receive references to various services in a module; simply add the corresponding services as parameters to the module's constructor like this:
```csharp
public EchoModule(LoggingService logger) { ... }
```
Alternatively, dependency injection will also set all public fields.
More information about modules and commands can be found [here](https://discord.foxbot.me/docs/guides/commands/commands.html). You might want to read about `Type Reader`s because those are used to automatically parse parameters.

## Dependency Injection
Discord.Net is written with dependency injection in mind. That is, when the program start, you register a bunch of services. These are then bundled up into a *provider*. Whenever a module or service is created, the dependency injection will fill the parameters of the constructor and all publicly writable properties with appropriate services.

## Current capabilities
The bot currently only supports asking for help using `!help` and echoing messages back to the user. The `help` functionality will surely be useful for your own bot, too. The set of features is very limited on purpose because it is more about the architecture of the bot than about its cool functions (I'm sure you have plenty ideas for that yourself!).

## MongoDB
Sooner or later, your bot will need to store persistent data (i.e., even more persistent than services, which just run for as long as the bot is running). Maybe you want to create a bot that allows you to set reminders in the future? They should be stored somewhere safely. We will be using a database for this; more specifically, we are using [MongoDB](https://www.mongodb.com/), a schemaless NoSql database with decent C# bindings that allow for fast iterations. Download it from [here](https://www.mongodb.com/download-center?jmp=nav#community) and follow the [installation instructions](https://docs.mongodb.com/manual/administration/install-community/).

The `MongoDBService` connects to the database and the `UserScoreModule` shows how to store and retrieve data for a given key. The `MongoDBModule` has some debugging commands for MongoDB.

# Credits
Inspired by [this wonderful example](https://github.com/Aux/Discord.Net-Example).