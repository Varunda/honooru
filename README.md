# honooru

honooru is an archive site that uses tags

## General Info

honooru is a .NET 8 web server

### Technologies used

Tech | Use | Link
--- | --- | ---
.NET 8 | backbone for everything | --
ASP.NET Core MVC | routing | --
SignalR | WebSocket connections | --
VueJS (v2) | frontend framework | --
Typescript | all frontend logic | --
PostgreSQL | database | --
YoutubeDLSharp | extracting youtube videos | https://github.com/Bluegrams/YoutubeDLSharp
FFMpegCore | ffmpeg wrapper for c# | https://github.com/rosenbjerg/FFMpegCore

## Setup

1. download honooru
    - `git clone https://github.com/varunda/honooru.git`
1. download and install PostgreSQL 13.3 or higher
    - earlier versions probably work but I haven't tested them
1. setup a `honooru` database for the app to create all the tables for
    - Linux:
        - log into user that has permissions in the default psql server 
            - `$ sudo -iu postgres` 
        - start a psql client
            - `$ psql`
        - create ps2 database
            - `postgres=# CREATE DATABASE ps2;`
        - ensure it exists
            - `postgres=# \c ps2;`
        - done!
    - Windows:
        - use pgAdmin or something I'm not sure exactly
1. change `syncronous_commit` to `off` instead of the default `on`.
    - https://www.postgresql.org/docs/current/wal-async-commit.html
    - Find your `postgresql.conf`, then to reload call `SELECT pg_reload_conf();`
1. install yt-dlp to PATH
    - for Ubuntu: https://github.com/yt-dlp/yt-dlp/wiki/Installation#apt
    - for Windows, download and install, update your PATH to include it
1. install ffmpeg and ffprobe to PATH
    - for Ubuntu: use apt
    - for Windows: install and include in PATH
1. compile the frontend code
    - install the node modules
        - `npm install`
    - compile the typescript into Javascript
        - `npm run watch` (you'll need to like ^C after or something)
    - done!
1. build the backend server
    - `dotnet build`
1. run honooru
    - `dotnet run`

There is (ideally) no configuration beyond this needed. All database tables will be created automatically as part of the startup process

If you want to change database options, you can either add a new object in `appsettings.json` called `DbOptions`, or add user secrets

### DbOptions in user secrets

1. initalize the user secrets
    - `dotnet user-secrets init`
1. set the options you want to change
    - `dotnet user-secrets set "$OptionName" "$Value"`

`$OptionName` can be:
- `DbOptions.ServerUrl`: Host of the server, localhost is the default which means running on the same machine
- `DbOptions.DatabaseName`: Name of the database, default is `honooru`, which is what was set in the database setup steps
- `DbOptions.Username`: Username used to connect to the database
- `DbOptions.Password`: Password of the user used to connect to the database

### Commands

honooru can be interacted with in the console by typing commands. It is a bit jank currently. If text is output while you're typing a command, the input text will be split. There is no command history or auto-complete.

Commands are processed in `/Code/Commands`, and can aid in running honooru. To list all commands available, use `.list`. To safely close honooru, use `.close`. Closing honooru thru SIGTERM can be done (such as ^C), but can prevent some non-essential cleanup processes from running (such as ending current sessions).

#### Common commands

print all hosted service:
- `service print`

disable a hosted service:
- `service disable $ServiceName`
- useful when testing on a 5 year old laptop that can't really handle the db queries 
- not all hosted services actually stop when disabled

enable a hosted service:
- `service enable $ServiceName`

### Tracing

honooru uses OpenTelemetry to do profiling with a Jaeger exporter

to run the tracing, run the command:

`docker compose -f jaeger-docker-compose.yml up`

if these Docker services aren't running, profiling will not take place
