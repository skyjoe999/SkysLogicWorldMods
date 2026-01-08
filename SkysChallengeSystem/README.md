# Challenge System

Adds a robust (hopefully) system for other mods to add challenges for the player to complete.

## Install Instructions

Just copy the SkysChallengeSystem folder into your GameData folder. You will also need `SkysBetterBoardLib`, `SkysGeneralLib` and `SkysLuaLib` from this repository as well as `EccsGuiBuilder` from [Ecconia's mod repository](https://github.com/Ecconia/Ecconia-LogicWorld-Mods/).

> [!IMPORTANT]

> This mod does not provide any actual challenges, for that you will need to install another mod that add their own challenges.

## How To Use In-game

1. Start by placing down a Challenge Board.

2. Select your challenge by pressing `Edit Component` (`x` by default).

3. Place down your Challenge Question and Challenge Answer pegs and configure them the same way.

4. Implement your solution.

5. Place down a Challenge Button and press it to begin!

> [!WARNING]

> Due to a bug, when you change which challenge a board has selected you need to reload the board by either deleting it and then undoing or by restarting the server. 

### Other Components

- A Display will give you relevant information about the challenge as it runs (other mods may also add unique kinds of displays for their challenges)

- A Challenge Button can also be configured into reset mode to let you stop a challenge that is still running

- For now a display can also be configured to show the challenge description (should be removed in the UI update)

## Developing Your Own Challenge Mod

Assuming you already know how to set up a mod the process is simple. To add the register your challenges both your `ServerMod` and `ClientMod`'s `Initialize()` functions you should call `ChallengeManager.RegisterChallenges(Files, Manifest);`. Then all you need is a `challenges` folder in the root folder of your mod to store the challenge jecs files.

### How To Make a Challenge

> [!CAUTION]

> This mod is still in its testing phase so challenges made now may have to be altered to be useable with the final project

> At a minimum there should be an update to make loading challenges server side only so clients do not need to install each mod unless it adds custom custom client content like components.

There are two ways to create a challenge: using an existing loader or implementing your own.

Challenges are stored in `.jecs` files just like components and are expected to be found in the `challenges` folder of a mod. (They can be stored elsewhere but doing so requires extra effort to register the challenges properly.) The top level key of the jecs is the name of the challenge (again, just like component files).

Each challenge record has the following properties:

> - `Description` the explanation to the player of what the challenge expects of them.

> - `Version` a version indicator that should be incremented any time you make a (published) change to that challenge.

> - `Folder` what folder or subfolder the challenge should be put in for the selection menu. Subfolders should be separated with `/`, no slash should be added at the start or end. (Can be blank.)

> - `QuestionNames` a list of the name of the pegs whose values are set by the challenge.

> - `AnswerNames` a list of the name of the pegs whose values are set by the challenge.

> - `Loader` the type of the `ChallengeLoader` used for this challenge (see below).

> [!NOTE]

> Question and answer names that are intended to be part of a collection should use the format `Name.0`, `Name.1`, `Name.2`, etc. to be compatible with future components.

> [!WARNING]

> `Description` is not visible in the selection menu yet. The UI is currently bare bones and needs to be updated.

## Challenge Loaders

Challenge loaders are how you customize your challenges. The loader defines how any extra data in the challenge file gets turned into an instanced `Challenge`

To implement your own challenge loader you need three main parts, a `ChallengeLoader` class, a `ChallengeRecord` class, and a `Challenge` class.

The `ChallengeLoader` itself simply exists to turn your `ChallengeRecord`s into `Challenge`s.

### Existing Loaders

Currently there is only one (non-debug) loader in the mod called `BasicChallengeLoader` which runs on a linear series of steps where it provides some question and then expects some answer. Check the `BasicChallengeLoader.BasicChallengeRecord` type for detailed information about all of its options.


### Custom Records

The `ChallengeRecord` class is responsible for defining the extra fields that should get loaded in from the challenge's jecs file. This should store all of the data that differentiates one challenge from another. (assuming you want your loader to be reusable. There is nothing stopping you from making one loader per challenge.)

### Custom Challenges

The `Challenge` class represents an instanced challenge and handles all of the logic for updating and validating its state. These only exist on the server and thus can only access server side information.

The most relevant parts of the interface to creating a custom challenge are:

- `OnBegin` - called when the challenge is started.

- `OnResume` - called when the server is restarted or a challenge board is copied (useful for setting up any information not stored in `RunningData`).

- `OnCancel` - called when a challenge is canceled in case any cleanup is required.

- `ChallengeDataAccess` - this is your link to the physical challenge board. It lets you read answers, write questions, set displays, etc.

- `OnStep` - called once every time the challenge board receives a logic update. This is where your main logic will reside.

- `ShouldWaitForAnswerChange` - called after every step. If false the a step will automatically be queued for the next logic tick even if nothing has changed. Always return true to run every single tick.

- `Succeed`/`Fail` - call these functions to mark the challenge as over. Doing so will emit the `OnSuccess` or `OnFailure` signals accordingly.

Each challenge also has an instance of `RunningData` which is the only state information that gets saved if the game restarts. You can think of it like the challenge equivalent of a component's custom data. Just like with custom data, if you want all of this to be handled for you, you can inherit from `Challenge<IData>` where `IData` is an interface with all of the relevant information.

