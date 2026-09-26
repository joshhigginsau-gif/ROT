# Wardens & Dragons (ROT playthrough mod)

A Game of Thrones political-layer mod for Mount & Blade II: Bannerlord, built to sit alongside Realm of Thrones, Bellum Civile and
RoT Dynasty & Succession. See `HANDOVER.md` for how it works, the rules it follows and the open items.

- `Module/WardensAndDragons/` is the installable module folder. Copy it into `Modules/`.
- `src/` is the C# source.

## Target game

Bannerlord **1.4.7 / 1.4.8**, identified by comparing the player's `TaleWorlds.*.dll` files against each release's reference
assemblies. Their DLLs contain every API change through 1.4.7 and none from the 1.5 betas.

## Building

```
cd src
dotnet build -c Release
```

The build uses BUTR's `Bannerlord.ReferenceAssemblies.Core` from NuGet, so you don't need the game installed. It copies the DLL into
both `Module/WardensAndDragons/bin/` folders. To target another game version, pass `-p:GameVersion=<version>`.

Bellum Civile, RoT Dynasty and Warsails are reached only through reflection, so the build needs no references to them.
