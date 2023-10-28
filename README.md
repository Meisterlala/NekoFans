<img src="https://api.nekofans.net/count_total" align="left" width="100%" alt="Total image count">

# Neko Fans Plugin for FFXIV <img src="icon.png" align="right" width="240">

[![Build](https://img.shields.io/github/actions/workflow/status/Meisterlala/NekoFans/build.yml?branch=master&label=Build)](https://github.com/Meisterlala/Nekofans/releases/latest/)
[![Latest Version](https://img.shields.io/github/v/tag/Meisterlala/NekoFans?label=Version&sort=semver)](https://github.com/Meisterlala/Nekofans/releases/latest/)
[![Neko Server Status](https://img.shields.io/website?down_message=offline&label=Neko%20Server&up_message=online&url=http%3A%2F%2Fapi.nekofans.net)](https://status.nekofans.net)
[![Ko-fi](https://img.shields.io/badge/support_me_on_ko--fi-F16061?logo=kofi&logoColor=f5f5f5)](https://ko-fi.com/Meisterlala)


**Displays a random Neko cat girl.**
(or an image from a different source)

This is a plugin to be used with the [FFXIVQuickLauncher](https://github.com/goatcorp/FFXIVQuickLauncher) and [Dalamud](https://github.com/goatcorp/Dalamud).

Some functions of the Plugin are provided by the [Neko Server](https://github.com/Meisterlala/NekoServer).

<p align="center">
  <img src="https://cdn.discordapp.com/emojis/851544845322551347.webp?size=44&quality=lossless" align="left" alt="Total image count">
    Give this project a star, so more people will find it. 
  <img src="https://cdn.discordapp.com/emojis/851511871625756694.webp?size=44&quality=lossless" align="right" alt="Total image count">
  <br>
</p>
  <br>

> Inspired by [Nya](https://github.com/Sirspam/Nya) for Beat Saber.

## Installing

<details><summary>Click here for a video tutorial</summary>
<p>

![Tutorial](/resources/install_video.gif)

</p>
</details>

Open the Plugin installer with `/xlplugins`, search for _Neko Fans_ and click install. If you also want the NSFW Patch you have to add the custom repository below in the experimental settings of Dalamud `/xlsettings`

```url
https://raw.githubusercontent.com/Meisterlala/NekoFans/master/repo.json
```

You can also download a specific version on the [release page](https://github.com/Meisterlala/NekoFans/releases) and install it manually. Unzip the downloaded file and add the folder to "Dev Plugin Locations" in the experimental settings.

## NSFW Images <img src="icon18.png" align="left" height="160">

By default you can only view Safe for Work (SFW) images.

Installing the **Neko Fans NSFW 18+ Patch** Plugin will give you full access to all the images each API provides. This Patch is not in the offical Dalamud plugin repository and needs to be installed separately by adding a custom plugin repository (see [above](#installing)).

## Usage

To use the plugin, you must have launched the game via [XIVLauncher](https://github.com/goatcorp/FFXIVQuickLauncher).
Then, all you need to do is type `/neko` in the in game chat to open the main window.

Everytime you click on the image, a new one will appear.

You can change to look and feel of the plugin in the Configutaion Menu, which can be opened by typing `/nekocfg` in the game chat or by clicking the ⚙ in the Plugin installer. You can choose the API, which provides the Images in the Configuration Menu.

## Supported APIs

- [Catboys](https://catboys.com/) (discontinued)
- [Dog CEO](https://dog.ceo/dog-api/)
- [nekos.best](https://nekos.best/)
- [Nekos.life](https://nekos.life/)
- [Pic.re](https://pic.re/)
- [shibe.online](https://shibe.online/)
- [The Cat API](https://thecatapi.com/)
- [WAIFU.IM](https://waifu.im/)
- [Waifu.pics](https://waifu.im/)
- [Twitter](https://twitter.com/) (no longer supported after API price increase)
