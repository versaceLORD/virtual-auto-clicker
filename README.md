## virtual-auto-clicker - Your idle success

### If you have problems setting it up

Helpdesk discord where you can talk to me and other contributors/users https://discord.gg/P5Zr3YV

### Reason for repo's existence

There is a bunch of resources out there related to autoclickers. But on the windows platform I failed to find a good alternative for an autoclicker that allowed you to still be in control of your mouse.

This simple yet efficient console application fulfills your needs for a quick and effective clicker while still letting you using your PC freely!

### How to download

* In the [releases](https://github.com/versaceLORD/virtual-auto-clicker/releases) tab you will find a download for windows x64&x86 for each version of the application. 

* Download the correct version (x64/x86) for your system, no further configuration or additional downloads are necessary as the download is a "stand-alone" application.

### How to use

* Start the `virtual-autoclicker-console.exe` file that will be located in the folder. 

* To run any command in the application press your Enter key and a `VAC >>` text will appear. Any text you write after this will be parsed as a command. All additional arguments are space separated.

* To start the application run: `startautoclicker "P" X,Y I N`
  * P = Name of process to start autoclicker in
  * X = Coordinate on the X-axis
  * Y = Coordinate on the Y-axis
  * I = Interval in milliseconds
  * N = Name of the autoclicker instance. This is an optional parameter, and the autoclicker works just fine without a custom name.
  Giving it a custom name is useful if you plan to use multiple autoclickers simultaneously
  * Example command: `startautoclicker "Firestone" 450,450 50`
    * Coordinates are relative to the top left corner of the entered process so for reference, coordinates (0,0) is the top left corner.  

* To close the autoclicker, run: `stop`.

## Full list of commands

Please refer to the descriptions of the abbreviations in the how to use part of this document as they will be used here as well.

* To start the application run: `startautoclicker "P" X,Y I N`

* To temporarily stop the autoclicker, run `pause N`

* To temporarily resume a paused autoclicker, run `resume N`

* To list all started autoclickers, run `list`

* To stop all started autoclickers run `stop` or `picnic`

## Notes

_This repository is very minimalistic and made for a very specific use, the code should not be seen as best practise or what type of code standard I personally advocate._ 

In the source all references to stopping is refered to as `picnic`, if you watch any twitch streams you probably get the reference. :)

## License
```
Apache License
Version 2.0, January 2004
http://www.apache.org/licenses/

Copyright 2020 Emil Widehov/@versaceLORD
```
