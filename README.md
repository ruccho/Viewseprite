# Viewseprite
 Utility to use Aseprite images as Unity Texture2D in realtime.

![image](https://user-images.githubusercontent.com/16096562/139103047-8df604ad-e0f5-40f3-9d30-43693d48c94d.png)

## Requirements
 - Tested on Unity 2020.3
 - **Aseprite v1.2.30  or later**

## Use Sample
1. Copy `scripts/Launch Viewseprite.lua` into Aseprite scripts folder.
2. Open Unity project of Viewseprite and play it.
3. On Aseprite, open something and use `Launch Viewseprite` from scripts menu.

## Install Viewseprite into your project
Use UPM git dependencies.
1. Click `+` > `Add package from git URL...`
2. Enter `https://github.com/ruccho/Viewseprite.git?path=/Viewseprite/Packages/io.github.ruccho.viewseprite`

## Usage
Viewseprite works with Grabber components.
 - `GrabberForSpriteRenderer`: Shows sprite through SpriteRenderer.
 - `GrabberForRenderer`: Shows sprite through generic renderer such as MeshRenderer.
 - `GrabberForImage`: Shows sprite through uGUI Image.

For customization, abstract class `GrabberBase` is provided. Derive it and implement `SetTexture(Texture2D texture)` method.

### Layer Specification
Each Grabber component has `Visible Layer` property. Specify name of the layer to grab. If there is no matched layer, whole sprite is grabbed.

Layer specification is only applied at startup of each connection, so in order to change layer, restart game or Viewseprite script on Aseprite.

## References
- [lampysprites/aseprite-interprocessing-demo](https://github.com/lampysprites/aseprite-interprocessing-demo)