# Spotify Streaming Widget

## Technology

 - C#, .Net Core 3.1
 - WPF
 - [Caliburn Micro](https://caliburnmicro.com/)
 - [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET)

## Features

 - Automatically Authenticates with Spotify
 - Toggle to hide the Title bar (Shift+V to bring back)
 - Polls spotify every 5 seconds to get playback data

## To Build

```bash
  $ dotnet build
```

## To Publish

```bash
  $ dotnet publish -r win10-x64 -p:PublishSingleFile=true -c Release
```