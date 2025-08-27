# Spriter2UnityDX Reanimated 😎

>re·an·i·mate <br>
/rēˈanəˌmāt/ <br>
verb <br>
past tense: **reanimated**; past participle: **reanimated** <br>
>   * restore to life or consciousness; revive. <br>
>   * give fresh vigor or impetus to. <br>

⚠️ This document is still a work in progress.

## Description
Spriter2UnityDX helps you integrate Spriter projects into Unity.  It imports Spriter .scml files and the images that it references and produces the following as output:

* **Prefabs** <br>
One prefab will be generated for each of the entities in the .scml file.  The prefab's preview image will be generated based on the first frame of the entity's first animation.
* **Animator controllers** <br>
One animator controller will be generated for each of the entities.  An animation state will be created for each of the entity's animations.
* **Animation clips** <br>
One animation clip will be generated for each of an entity's animations.  These are standard Unity animation clips that can be played/scrubbed in-editor using Unity's Animator window.  If the structure of the Spriter file permits it, you can use Unity animation features such as crossfade and transition blending.

## Why this fork?

This is a custom fork of the Spriter2UnityDX project.  It adds a lot of new functionality.  The main focus of the fork is on animation visual fidelity.  That is, matching Spriter's animation playback as much as practical.  It aims to be lightweight and allocation free.

## Where to get Spriter2UnityDX and how to install it

You have two simple options for installation: grab the UnityPackage from Releases, or install directly from source.

1. **Install via Unity Package**<br>
    1. Grab the latest Unity package from [here.](https://github.com/TerminalJack/Spriter2UnityDX/releases) Unlike the Github repo (which is a complete Unity project), the Unity package will have only the files you need for integration into your own Unity project.<br>
    2. Drag-and-drop the package into your project's `Project` window.<br>
    3. In the import dialog, click `Import`.
2. **Install from Source**<br>
    1. Clone or download the full repo:<br>
    `git clone https://github.com/TerminalJack/Spriter2UnityDX.git`<br>
    2. In your OS file browser, locate the Spriter2UnityDX folder inside the repo.<br>
    3. Drag-and-drop that folder into your Unity `Project` window.

> At this time, the project's folder name or location can't be changed. That is, it must remain in the folder Assets/Spriter2UnityDX and can't be moved to another folder such as Assets/Plugins/Spriter2UnityDX.

## Quick start!

Once Spriter2UnityDX is installed you can import a Spriter project simply by dropping the folder that contains the .scml file--**and** all of the images files needed by the .scml file--into the Unity `Project` window.

>Only Unity 2D projects are supported at this time. An import may fail when you drop a Spriter project folder into a 3D project. A second attempt via Reimport *may* work.

Dropping a Spriter project folder into Unity's `Project` window will kick-off Unity's importers (for the image files) and, once Unity is done, it will hand control to Spriter2UnityDX to import the .scml file.  A window with a few import options will pop-up at this time.  For now, simply leave the import options as-is and click the `Done` button.

This will import all of the contents from all of the .scml files that are found in the folder and its subfolders.  The importer will ignore Spriter's `autosave` files so don't worry about them being present during import.

The importer will write its generated output files (the prefabs, etc.) into the same folder as the corresponding .scml file.  Depending on import settings, animation clips can be imbedded in the prefab or written into a subfolder.

> Important!  Some Spriter .scml files contain **a lot** of information to process.  It can take several minutes (up to 15 minutes!) to do an import.  ***Be patient.***  Unity will be unresponsive at this time but be assured that Unity isn't 'locked up'.  If you're worried, you can open a file browser and monitor the importer's progress by checking for the presence of the importer's output files.  If possible, you should start with a small, simple Spriter project.

Once the import is complete, check the folder for newly created prefab files.  There will be one for each entity in the .scml file(s).  If you click on one of these prefabs you will see that its preview image is generated from the first frame of the entity's first animation.

If you don't see a preview image then be sure that you aren't trying to import the Spriter project into a 3D Unity project.  If you *are* then right-clicking the .scml file and clicking `Reimport` *may* work but, strictly speaking, 3D projects aren't supported at this time.

Drop one of these prefabs into the `Scene` view.  Open Unity's `Animation` window.  Select the game object (the instantiated prefab) in the `Scene` view and select an animation clip in the `Animation` window.  Hit the play button (▶️) and the animation will play.

>This assumes that that particular clip actually had an animation.  Creators wll often use one or more Spriter animations as a static guideline or template, from which they base their actual animations off of.  If the clip doesn't actually animate anything then try another one.

Assuming that all goes well, you are ready to use the generated prefabs, animation clips, etc. in your next masterpiece.

If you need to reimport a .scml file at any time, right-click the file and click `Reimport`.  This will attempt to integrate any changes that have been made to the the .scml file into the existing prefabs and animator controllers.  The reimport will overwrite the animation clips but it will attempt to preserve any animation events that you have added.

If there have been drastic changes to the .scml file since the prefab was last generated then reimporting over an existing prefab may seemingly corrupt the prefab.  At this point, the importer isn't particularly robust in this regard.  See the `Tips and Tricks` section on how to avoid (or at least minimize) putting any customizations in the hierarchy of the prefab.  If you do this then you can simply delete the prefab before reimporting to ensure that there are no issues with reimporting on top of a preexisting prefab.

Finally, before you go and play with the newly generated prefabs, be wary when trying to use Unity's transition blending feature.  When you create a transition from one animator state to another, Unity will, by default, blend the two animations.

For Spriter projects, the biggest factor as to whether this works or not depends on how different the bone hierarchy is between the two animations.  If they are different in any way then you will likely have sprites that go flying off in seemingly random directions during the transition.  This applies when using the `Animator.CrossFade()` method as well.  If you run into this issue then you will either need to change the bone hierarchy in Spriter or completely disable transition blending.

## Supported versions of Unity.

The importer and the generated prefabs, animator controllers, animation clips, and the runtime library are all supported by Unity version 2019 and later.  The importer's output is **not** tied to the same version of Unity that produced it.  That is, you can produce the prefabs, animator controllers, and animation clips in Unity 2019 and use them as-is in Unity version 6.1 and vice-versa.

## Supported pipelines.

The importer and its output will work with the built-in renderer (aka BiRP) as well as the Universal Render Pipeline (URP.)

## Supported development platforms.

As of this writing the only development platform that has been tested is Windows.  If you try the importer on another development platform then please let me know how it goes!

## Supported runtime platforms.

The only runtime platforms that have been tested at this time are Windows and WebGL.  Please let me know how it goes for the other runtime platforms.

## Supported Spriter features.

The importer currently supports the following Spriter features:

* **All Spriter easing curve types.**  Instant (aka constant), linear, quadratic, cubic, quartic, quintic, and Bézier curves are all converted to Unity animation curves with high visual fidelity.
* **Dynamic reparenting.**  Spriter allows the artist to reassign a bone/sprite's parent at any time of an animation.  The importer will emulate this functionality by creating a `virtual parent` for the bones and sprites that have more than one parent (across all of the entity's animations.)  This is also known as a `parent constraint` or `child of constraint` in animation applications.
* **Non-default / dynamic pivots.**  Spriter allows a sprite's pivot to change at any time of an animation.  The importer fully supports this via a `dynamic pivot` component.
* **Sort order or z-index.**  Sprites can change their sort order frame-by-frame.  This is fully supported via the Unity `Sprite Renderer's` `Order in layer` property.

## Unsupported Spriter features.

The following Spriter features are not supported at this time:

* Character maps
* Variables
* Triggers
* Tags
* Sounds
* Collision rectangles
* Action points
* SCON files (an alternative to SCML files)
* Sub-entities
* Texture Packer atlases
* Bone alpha
* *Animated* bone scales

> Note about bone alpha: Spriter allows a bone's transparency (aka alpha) to be animated.  This affects the sprites that are children of the bone.

> Note about bone scales: Strictly speaking, the importer supports a bone changing its scale.  It will not *tween* (i.e. animate) a bone's scale, however.

## Instructions for use.
...

## Runtime components.

### `Dynamic Pivot 2D`

### `Sorting Order Updater`

### `Sprite Visibility`

### `Texture Controller`

### `Virtual Parent`

## Tips and tricks.

**Sprite Atlases**

The single biggest performance boost you can gain with the prefabs generated from Spriter is to use a `Sprite Atlas` with them.  If you create an empty scene, throw a prefab into the scene view, click run, and click the `Stats` button, you will notice that the number of batches (basically draw calls) is more-or-less the same as the number of visible sprites that the Spriter entity is composed of.  This can be dozens!

Once you have dozens of characters on-screen the number of draw calls can easily exceed 500 to 1000.

You can reduce the number of draw calls down to 1 per Spriter entity simply by creating a (single!) `Sprite Atlas` for all of the images that the entity is composed of.  Creating a sprite atlas is simple but it is beyond the scope of this document to go into details.  Just be sure Unity generates a single sprite atlas!  If all of the images don't fit into a single atlas then Unity will create as many as needed but this will defeat the purpose of using them!  The number of draw calls may be reduced but it almost certainly will not be just 1 draw call.

Many commercial Spriter projects come with images that are much bigger than you would normally use in-game.  You may need to use Spriter Pro's `Save as resized project` feature to reduce the size of the images so that you can fit them all in a single sprite atlas.

**Mip Maps**

Something to be aware of with regard to Unity's sprite renderer, is that it uses simple UV texture sampling to get a sprite's texture from GPU memory to the screen.  That is, there isn't any texture filtering done.  Basically, what this means is that if, for example, the image in the GPU's memory is twice as large as it will be rendered on-screen then only every other pixel will be sampled when generating the output.

This can result in sprites that have jagged edges.  The simple solution to this is to enable mip maps via the `Sprite Atlas`'s `Generate Mip Maps` property.  (You *are* using sprite atlases, right?)

>From Wikipedia: In computer graphics, a mipmap (mip being an acronym of the Latin phrase multum in parvo, meaning "much in little") is a pre-calculated, optimized sequence of images, each of which has an image resolution which is a factor of two smaller than the previous.  Their use is known as mipmapping.

Another option to fix this is to use Spriter Pro's `Save as resized project` feature.  This will allow you to generate images that are basically "pixel perfect" so that the images don't need to be stretched or compressed.

>This is actually a bit more complicated than it is made out to be since this assumes that you are either a) supporting just a single resolution for your game, or b) will generate separate image sets (and atlases) for each of the resolutions you intend to support.  A good 'middle ground' is to use Spriter Pro's `Save as resized project` feature to generate pixel perfect images at you game's maximum supported resolution **and** enable mip maps.

## Caveats.

While the importer strives to convert your Spriter projects into Unity animations, it doesn't necessarily focus on making those animations easily editable in Unity.  You will likely find that it is better to continue using Spriter for animation creation and editing.

## Known Issues.

During an import, having a Spriter project open in the Spriter application can (infrequently) cause the import to fail.  You will get an error regarding file access.  You may need to close the Spriter application in this case.

The project's folder name or location can't be changed.  That is, it must remain in the folder `Assets/Spriter2UnityDX` and can't be moved to another folder such as `Assets/Plugins/Spriter2UnityDX`.

Only Unity 2D projects are supported.  An import may fail when you drop a Spriter project folder into a 3D project.  A second attempt via `Reimport` *may* work.

There may be issues with key timing during parent and/or pivot changes.  This may not always be noticeable during normal playback because it happens for just one frame.  (Spriter projects have a framerate of **1000 frames per second!**)  Slowly scrubbing through the frames at the point of a parent/pivot change may reveal that the keys aren't properly synchronized.  For a single frame the affected sprite(s) will have an incorrect position, rotation and/or scale.  This can usually be corrected in the Unity `Animation` window by moving the master keyframe (the topmost key in the dopesheet) a single frame to the **right** and then back to the left.

## License.

This fork has the same license as the original project.  The text of which is as follows:

> This project is open source. Anyone can use any part of this code however they wish. Feel free to use this code in your own projects, or expand on this code. If you have any improvements to the code itself, please visit https://github.com/Dharengo/Spriter2UnityDX and share your suggestions by creating a fork
-Dengar/Dharengo

## Credits.

I, [TerminalJack](https://github.com/TerminalJack), would like to thank the original creator of the Spriter2UnityDX project, [Dharengo](https://github.com/Dharengo) as well as contributors to the project [rfadeev](https://github.com/rfadeev) and [Mazku](https://github.com/Mazku).

## FAQs.

### Why are the animation clips set to a sample rate of 1000?  Isn't that a little excessive?

The sample rate is set to 1000 due to the fact that that is Spriter's effective sample rate.  In Spriter, there is nothing stopping the creator from putting two keyframes just 1 millisecond apart.  In fact, this is quite common.  The creator will do this when they intend for an animated property to change instantly.

Using the same sample rate as Spriter also allows Unity to be frame-for-frame identical to Spriter.  You will find the keys at the exact same frame in Unity as you do in Spriter.
