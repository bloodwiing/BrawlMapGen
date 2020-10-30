# BrawlMapGen
BMG (Advanced Map Generator) is a generator which takes arrays of strings and tiles with vector graphics to make generated map images of any size.

## Dependencies
* [SVG](https://github.com/vvvv/SVG)
* [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
* [System.Drawing.Common](https://www.nuget.org/packages/System.Drawing.Common)

## Compiling
You can use the built versions in `/builds/` or build the program yourself using a compiler like Visual Studio.

## Using
Before opening the built app, notice the `options.json` which should be provided in the repository's root folder, to use the file make sure it's in the same folder as the executable. The provided file contains some generation data, like the map, biome, gamemode. Play around with the settings to understand it better!

#### Generation steps:
BMG/AMG first preloads every single tile found in the chosen preset file, doing a pass through every file for each different tileSize you set in `options.json`.
**! The bigger the tilesize, the longer it's going to do a pass, at really big values it might even fail !**
After loading in all assets, the generator starts to generate the images as fast as possible, your disk speed will have an impact on the speed of generation as after each map image is constructed, it's saved to a file and discarded from memory for efficiency and so that the RAM usage doesn't pile on.

For information about what gamemodes, biomes, tiles, etc. can be set in the `options.json` file, you can find [here](https://github.com/thedonciuxx/BrawlMapGen/wiki/Options.json-explained).
