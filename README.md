# MobileEditor

A simple level editor for mobile devices made in Unity.

## How To Use

Open the Project in Unity 2021.3.19, and open the Sandbox scene. Press play, and use the Simulator to move around the scene. Assets can be dragged in from the hotbar at the bottom, and placed anywhere in the scene. Once places they can be dragged around again by selecting them first, and dragging them again. To delete a placed asset, simply drag it to the hotbar again on the thrash bin icon.

If you're running it on a mobile device, or using Unity Remote you can also rotate the camera by using two finger, or zoom the camera in and out by pinching on the screen.

After manipulating your scene you can undo and redo changes using the buttons in the top left of the screen. Restarting the application will restore your previous scene. Undo and redo actions are not preserved when restarting the application.

## How It Works

The main controller of the application is the `SceneManager` class. The `SceneManager` keeps track of all changes and events in the scene, and acts accordingly. This involves things like initializing other components like the `InputHandler` and `SelectionService`, to listening to UI events and invoking additonal methods. It also tracks deleted and created objects in the scene via the `ObjectRegistry`.

### Input Handling

Input handling is done by the `InputHandler`. This class contains a simple state machine, with each `IInputState` handling a different type of input. The different states are as follows:

- `IdleState`: Does nothing, but listens to touch input. If input is detected, the appropriate state is made active.
- `SelectObjectState`: Activated if there's a single touch input. Checks if the input is a single tap, and tries to select the object at that location.
- `DragCameraState`: Activated if there's a single moving touch input. Signals the `CameraController` to move the camera according to the input that's received.
- `DragObjectState`: Activated if there's a single moving touch input on a selected object. Signals the `ObjectController` to move the object according to the input that's received.
- `RotateAndZoomState`: Activated if there's two touch inputs. Signals the `CameraController` to rotate or zoom in and out according to the input that's received.

### Asset Creation

Assets that can be created are defined in `AssetDescriptor` assets. On startup the `SceneManager` applies its descriptors to the UI to show the correct icon. Each descriptor has an `AssetId` which at the moment is simply the index of the descriptor in the array of descriptors. This ID is used when an asset needs to be created, whether its from the UI, Undo actions, or when loading a scene.

### Asset Manipulation

Once assets have been created in the scene, they can be selected again by tapping on them. Each created object in the scene has a `SelectableObject` component. This component creates a `SphereCollider` on startup on the object which is based on the `Bounds` of the entire object. The `SelectObjectState` is able to test for this collider via a `Physics.Raycast` call. To avoid issues with other colliders, the `SphereCollider` is set to a separate "Selection" layer.
Once an object is selected, the `DragObjectState` can move it around the scene. If the UI notices that the pointer/touch is released on top of the thrash bin icon, it will notify the `SceneManager` to delete the current selection.

### Persistent Scene

Whenever something is changed (created, deleted or moved) in the scene, the scene is serialized into a `SceneData` object. The `SceneData` is serialized to JSON, and stored as text in the persistent directory of the current device.

On startup the `SceneManager` attempts to deserialize the stored data, and rebuilds the previously saved scene. `SceneData` contains minimal data, with each object only having a `Vector3` position, and an `int` ID which defines which asset it should create on the position.

### Undo Changes

The `SceneManager` has an `UndoService` object. This class is responsible for managing the various `IUndoAction` that are generated when editing objects in the scene. The `UndoService` allows to go back and forward in the history via the `Undo` and `Redo` methods.

`IUndoAction` instances don't store direct references to their target `GameObject` instances, but instead retrieve them from the `SceneManager`. This is done because it's not guaranteed that the original reference is still "alive", for example if an `UndoMoveAction` is trying to move an object that was destroyed at some point, but then restored with `UndoDeleteAction`. By getting objects via the `InstanceId` they can work around this problem.

### CameraController & ObjectController

The two Controller classes, `CameraController` and `ObjectController`, are there to make it easy to smoothly manipulate the scene. For example, when dragging an object, it will gradually follow the position of the pointer, going faster the further away it is.

The one exception to this smoothing is dragging the camera itself. Right now it approximately matches the exact movement of the pointer in the world, which feels more naturally to me personally.

### UI

The UI is a self contained prefab. The `UIManager` class has several events that are used to signal when something has happened. This way the UI can be easily modified without having to worry about breaking references in other places.
