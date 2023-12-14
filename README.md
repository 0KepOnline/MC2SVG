# ![](https://scenariopla.net/mc2svg/get?texture=a6598aaa29dfc6a2026d6f4a10c4f5c1707935ecba5f71cc168039742ece0e7a&size=24&smaller=1) [MC2SVG](https://scenariopla.net/mc2svg/)

MC2SVG is a fan-made website that provides a simple API endpoint for converting Minecraft faces to Scalable Vector Graphics. It's running on a fairly simple C# code, which is published here.

Uses [Big Gustave](https://github.com/EliotJones/BigGustave) to read PNGs & [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) to parse Minecraft API responses.

## Example SVGs
| ![ScenarioPlanet](https://scenariopla.net/mc2svg/get?nickname=ScenarioPlanet&size=128) | ![Luigifan100](https://scenariopla.net/mc2svg/get?nickname=Luigifan100&size=128) | ![SenatorApple](https://scenariopla.net/mc2svg/get?nickname=SenatorApple&size=128) | ![SlimeDiamond](https://scenariopla.net/mc2svg/get?nickname=SlimeDiamond&size=128) | ![laootr_](https://scenariopla.net/mc2svg/get?nickname=laootr_&size=128) | ![stromhurst](https://scenariopla.net/mc2svg/get?nickname=stromhurst&size=128) |
|-|-|-|-|-|-|

## Usage

Endpoint documentation is available on the site: https://scenariopla.net/mc2svg/.

## Command line arguments

Main arguments are `input data` and `input data type`, which is a number corresponding to this table:
| Input data type | Input data                                           |
|-----------------|------------------------------------------------------|
| `0`             | Player UUID                                          |
| `1`             | Player nickname                                      |
| `2`             | Skin texture SHA-256 (key on textures.minecraft.net) |

Also, you can specify these optional parameters (order is required):

`[input data] [input data type] [hat overlay] [size] [crisp edges] [no stroke]`

Example set of arguments: `ScenarioPlanet 1 0 256 1 0`

| Argument      | Description                                               |
|---------------|-----------------------------------------------------------|
| `hat overlay` | use hat overlay layer, **boolean**, *default: true*       |
| `size`        | size in pixels, **number** > 0, *default: 512*            |
| `crisp edges` | add crispEdges attribute, **boolean**, *default: true*    |
| `no stroke`   | don't add stroke attribute, **boolean**, *default: false* |
