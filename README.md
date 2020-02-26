# Unity2DNetSample
A (very) basic sample using Unity's low level Transport API for Client/Server Networking.

This POC uses the `Unity.Networking.Transport` API to create a basic server, client, and send test data. It uses [MessagePack](https://github.com/msgpack/msgpack-cli) for data serialization into a byte array, but that could easily be swapped out for other serialization schemes as needed.

This project was created as a basic example that isn't dependent upon and could potentially be used outside of Unity's new Entity Component System (ECS).

Some of the pieces (such as the basic state machine implementation and string extension methods) were taken directly from the Unity FPS Sample: https://github.com/Unity-Technologies/FPSSample

## Status and prerequisites

This is a major work in progress and I hope to continually update it with new features/changes and upgrade as Unity releases new versions of the Transport package.

Current status at a glance:
```
Unity version: 2019.3.0f6

Open Items/Issues:
- Add Basic Interactions between Local and Network GameObjects
- Add Server Authority / Client Prediction
- Add a "Lobby" Mechanic
```

## Getting the project

To get the project folder you need to clone the project.
Note, that 

> __IMPORTANT__: 
> This project uses Git Large Files Support (LFS). Downloading a zip file using the green button on Github
> **will not work**. You must clone the project with a version of git that has LFS.
> You can download Git LFS here: https://git-lfs.github.com/.
