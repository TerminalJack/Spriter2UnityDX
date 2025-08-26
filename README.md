# Spriter2UnityDX Reanimated 😎

>re·an·i·mate <br>
/rēˈanəˌmāt/ <br>
verb <br>
past tense: **reanimated**; past participle: **reanimated** <br>
>   * restore to life or consciousness; revive. <br>
>   * give fresh vigor or impetus to. <br>

This documentation is still a work in progress.

## Description
Spriter2UnityDX helps you integrate Spriter .scml files into Unity.  It imports Spriter .scml files and produces the following as output:

* **Prefabs** <br>
One prefab will be generated for each of the entities in the .scml file.  The prefab's preview image will be generated based on the first frame of the entity's first animation.
* **Animator controllers** <br>
One animator controller will be generated for each of the entities.  An animation state will be created for each of the entity's animations.
* **Animation clips** <br>
One animation clip will be generated for each of an entity's animations.  These are standard Unity animation clips that can be played/scrubbed in-editor using Unity's Animator window.  If the structure of the Spriter file permits it, you can use Unity animation features such as crossfade and transition blending.

>This is a custom fork of the Spriter2UnityDX project.  It adds a lot of new functionality.  The main focus of the fork is on animation fidelity.  That is, matching Spriter's animation playback as much as practical.

## Where to get Spriter2UnityDX and how to install it

Grab the latest Unity package from [here.](https://github.com/TerminalJack/Spriter2UnityDX/releases)

Unlike the Github repo (which is a complete Unity project), the Unity package will have only the files you need for integration into your own Unity project.

Drag-and-drop the package into you project's `Project` window to install it.

As an alternative, if you have already cloned the project repo then you can also install it by simply dragging and dropping the `Unity2SpriterDX` folder from your file browser into your Unity project's `Project` window.

> At this time, the project's folder name or location can't be changed. That is, it must remain in the folder Assets/Spriter2UnityDX and can't be moved to another folder such as Assets/Plugins/Spriter2UnityDX. <br>
<br>
Only Unity 2D projects are supported at this time. An import may fail when you drop a Spriter project folder into a 3D project. A second attempt via Reimport *may* work.

## Quick start!

Once Spriter2UnityDX is installed you can import a Spriter .scml file simply by dropping the folder that contains the .scml file--**and** all of the images files needed by the .scml file--into the Unity `Project` window.

This will kick-off Unity's importers (for the image files) and, once Unity is done, it will hand control to Spriter2UnityDX to import the .scml file.  A window with a few import options will pop-up at this time.  For now, simply leave the import options as-is and click the `Done` button.

This will import all of the contents from all of the .scml files that are found in the folder and its subfolders.  The importer will ignore Spriter's `autosave` files so don't worry about them being present during import.

The importer will write its generated output files (the prefabs, etc.) into the same folder as the corresponding .scml file.  Depending on import settings, animation clips can be imbedded in the prefab or written into a subfolder.

> Important!  Some Spriter .scml files contain **a lot** of information to process.  It can take several minutes (up to 15 minutes!) to do an import.  ***Be patient.***  Unity will be unresponsive at this time but be assured that Unity isn't 'locked up'.  If you're worried, you can open a file browser and monitor the importer's progress by checking for the presence of the importer's output files.  If you can then start with a small, simple Spriter file.

Once the import is complete, check the folder for newly created prefab files.  There will be one for each entity in the .scml file.  Drop one of these into the `Scene` view.  Open Unity's `Animation` window.  Select the game object (the instantiated prefab) in the `Scene` view and select a animation clip in the `Animation` window.  Hit the play button (▶️) and the animation will play.

>This assumes that that particular clip actually had an animation.  Creators wll often use one or more Spriter animations as a static guideline or template, from which they base their actual animations off of.  If the clip doesn't actually animate anything then try another one.

Assuming that all goes well, you are ready to use the generated prefabs, animation clips, etc. in your next masterpiece.

If you need to reimport a .scml file at any time, right-click the file and click `Reimport`.  This will attempt to integrate any changes that have been made to the the .scml file into the existing prefabs and animator controllers.  The reimport will overwrite the animation clips but will attempt to preserve any animation events that you have added.

>If there have been drastic changes to the .scml file since the prefab was last generated then reimporting over an existing prefab may seemingly corrupt the prefab.  At this point, the importer isn't particularly robust in this regard.  See the `Tips and Tricks` section on how to avoid (or at least minimize) putting any customizations in the hierarchy of the prefab.  If you do this then you can simply delete the prefab before reimporting to ensure that there are no issues with reimporting on top of a preexisting prefab.

>Be wary when trying to use Unity's transition blending feature.  When you create a transition from one animator state to another, Unity will, by default, blend the two animations.  The biggest factor as to whether this works or not depends on how different the bone hierarchy is between the two animations.  If they are different in any way then you will likely have sprites that go flying off in seemingly random directions during the transition.  This applies when using the `Animator.CrossFade()` method as well.

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

* **All Spriter easing curve types.**  Instant (aka constant), linear, quadratic, cubic, quartic, quintic, and bezier curves are all converted to Unity animation curves with high fidelity.
* **Dynamic reparenting.**  Spriter allows the artist to reassign a bone/sprite's parent at any time of an animation.  The importer will emulate this functionality by creating a `virtual parent` for the bones and sprites that have more than one parent (across all of the entity's animations.)  This is also known as a `parent constraint` or `child of constraint` in animation applications.
* **Non-default / Dynamic pivots.**  Spriter allows a sprite's pivot to change at any time of an animation.  The importer fully supports this via a `dynamic pivot` component.
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
...

## Tips and tricks.
...

## Caveats.
...

## License.
...

## Credits.
...

## FAQs.
...

# Spriter2UnityDX's Original Readme

Version 1.0.4

Download the Unity Package here: https://github.com/Dharengo/Spriter2UnityDX/raw/master/Packages/Spriter2UnityDX.unitypackage

!!!Requires Unity 5.x!!!

Use Instructions:

1) Import the package into your Unity project (just drag and drop it into your Project view).<br>
2) Import your entire Spriter project folder (including all the textures) into your Unity project<br>
3) The converter should automatically create a prefab (with nested animations) and an AnimatorController<br>
4) When you make any changes to the .scml file, the converter will attempt to update existing assets if they exist<br>
5) If the update causes irregular behaviour, try deleting the original assets and then reimporting the .scml file

Changelog:

v1.0.4:<br>
Fixes:<br>
-AnimationEvents are now preserved between reimports<br>
-SpriteSwapper renamed to TextureController to avoid confusion<br>
-Fixed a z-position issue with the SortingOrderUpdater<br>
v1.0.3:<br>
Fixes:<br>
-Fixed an issue where flipped (negative-scaled) bones caused child sprites to appear out of place and in odd angles<br>
Features:<br>
-Added a toggle to the Entity Renderer that allows you to apply the .scml file's Z-index to the order-in-layer property of the Sprite Renderers<br>
-Removed Spriter2UnityDX components from the Add Component menu, since they are automatically added or removed through script<br>
v1.0.2:<br>
Fixes:<br>
-Fixed an issue where sprites appeared distorted when resizing bones.<br>
-Exceptions are wrapped up nicely and no longer abort the whole process<br>
Features:<br>
-Now adds AnimationClips to existing AnimatorStates if they exist<br>
-Autosaves no longer trigger the importer<br>
v1.0.1:<br>
Fixes: -Fixed an issue where the sprite's Z orders would get messed up if the sprite is moved during animation<br>
Features: -Z order can now be mutated during animation<br>
v1.0: Initial version
