# Wardens & Dragons (ROT playthrough mod)

A Mount & Blade II: Bannerlord mod for a Realm of Thrones campaign.

- `Module/WardensAndDragons/` is the installable module folder. Copy it into `Modules/`.
  - `README.txt` is the in-mod changelog. It currently stops at v2.1.0; the DLL is v2.2.0 ("children of your own": baseborn children).
- `src/` is the C# source, **recovered by decompiling the v2.2.0 DLL** with ILSpy. The original source wasn't available. Logic is intact, but local
  variable names are generated (`val`, `val2`, ...) and there are no comments.

## Building

```
cd src
dotnet build -c Release
```

The build uses BUTR's `Bannerlord.ReferenceAssemblies.Core` from NuGet, so you don't need the game installed. It copies the DLL into
`Module/WardensAndDragons/bin/...`. To match your game version, pass `-p:GameVersion=<version>` (default `1.3.15.110062`).

Soft dependencies (Bellum Civile, RoT Dynasty & Succession) are reached through reflection only, so no references to them are needed.
