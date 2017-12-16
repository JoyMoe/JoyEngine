JoyEngine
===
A scalable, distributed high-available online game server framework

[![AppVeyor](https://img.shields.io/appveyor/ci/JoyMoe/JoyEngine.svg)](https://ci.appveyor.com/project/JoyMoe/JoyEngine)
[![AppVeyor tests](https://img.shields.io/appveyor/tests/JoyMoe/JoyEngine.svg)](https://ci.appveyor.com/project/JoyMoe/JoyEngine)
[![Codecov](https://img.shields.io/codecov/c/github/JoyMoe/JoyEngine.svg)](https://codecov.io/gh/JoyMoe/JoyEngine)
[![license](https://img.shields.io/github/license/JoyMoe/JoyEngine.svg)](https://raw.githubusercontent.com/JoyMoe/JoyEngine/master/LICENSE)
[![CLA assistant](https://cla-assistant.io/readme/badge/JoyMoe/JoyEngine)](https://cla-assistant.io/JoyMoe/JoyEngine)

![System Structure](docs/System_Structure.svg)

## Build

Use Visual Studio 2017, Xamarin Studio or MonoDevelop to build it.

You can also build it with [Mono C# Compiler](http://www.mono-project.com/docs/about-mono/languages/csharp) or [.NET Core](http://dotnet.github.io/).

## Components

* [ ] Anti-Cheating Plugin (ACP)
    * [ ] Server-side Data Validation
* [x] Bussiness Logic Engine (BLE)
    * [ ] Database Support
    * [ ] Hook Support
    * [x] Lua Virtual Machine
    * [x] Tick Support
* [x] Input/Ouput Gateway (IOG)
    * [x] Group Management
    * [x] Incoming/Outgoing Messages Routing
    * [x] Websocket Connector
* [ ] Management RPC Server (RPC)
    * [ ] Management APIs
* [ ] System Monitor Daemon (MON)
    * [ ] Nodes Dispatching
    * [ ] Status Monitoring
* [ ] User Authentication Adapter (UAA)
    * [ ] Session Management
    * [ ] User Authentication
* [ ] Toolchain
    * [ ] Script Editor (for BLE)
    * [ ] Tool to create Data Schemas (for BLE and DBI)
    * [ ] Tool to create Message Schemas (for MGW)

## Contributing

Contributions are welcome and can be submitted using pull requests.

Please follow the guidelines in our [CONTRIBUTING](CONTRIBUTING.md) guide.

## License

JoyEngine is dual licensed under the GNU GENERAL PUBLIC LICENSE V3 or a commercial license. If you cannot accept GPL, you need to obtain a commercial license.

More info see [LICENSE](LICENSE).

#### There're some third-party libraries used by our project, they are:

##### [Google.Protobuf](https://github.com/google/protobuf/tree/master/csharp) (NuGet package)

the [3-Clause BSD license](https://raw.githubusercontent.com/google/protobuf/master/LICENSE)

##### [MoonSharp](https://github.com/xanathar/moonsharp) (NuGet package)

the [3-Clause BSD license](https://github.com/xanathar/moonsharp/blob/master/LICENSE)

##### [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) (NuGet package)

the [MIT License](https://raw.githubusercontent.com/JamesNK/Newtonsoft.Json/master/LICENSE.md)

##### [RabbitMQ.Client](https://github.com/rabbitmq/rabbitmq-dotnet-client) (NuGet package)

the [Apache License v2](https://raw.githubusercontent.com/rabbitmq/rabbitmq-dotnet-client/master/LICENSE-APACHE2) and the [Mozilla Public License v1.1](https://raw.githubusercontent.com/rabbitmq/rabbitmq-dotnet-client/master/LICENSE-MPL-RabbitMQ)

##### [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) (NuGet package)

the [MIT License](https://github.com/StackExchange/StackExchange.Redis/blob/master/LICENSE)
