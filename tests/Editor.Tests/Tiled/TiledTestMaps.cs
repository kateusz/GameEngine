using Editor.Features.Tiled;

namespace Editor.Tests.Tiled;

public static class TiledTestMaps
{
    public static string NewDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "tiled-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static void WriteStandardTileset(string dir, string image = "tiles.png") =>
        File.WriteAllText(Path.Combine(dir, "tiles.tsj"),
            $$"""{"tilewidth":16,"tileheight":16,"image":"{{image}}","imagewidth":32,"imageheight":32,"columns":2,"tilecount":4}""");

    public static TiledMapData ParseRect(int id = 1, string name = "wall", float x = 0, float y = 0, bool trigger = true)
    {
        var dir = NewDir();
        WriteStandardTileset(dir);
        var triggerProp = trigger
            ? ""","properties":[{"name":"trigger","type":"bool","value":true}]"""
            : "";
        File.WriteAllText(Path.Combine(dir, "map.tmj"),
            $$"""
            {
              "orientation":"orthogonal","infinite":false,
              "width":2,"height":2,"tilewidth":16,"tileheight":16,
              "tilesets":[{"firstgid":1,"source":"tiles.tsj"}],
              "layers":[
                {"type":"tilelayer","name":"ground","data":[0,0,0,0]},
                {"type":"objectgroup","name":"obj","objects":[
                  {"id":{{id}},"name":"{{name}}","x":{{x}},"y":{{y}},"width":16,"height":16{{triggerProp}}}
                ]}
              ]
            }
            """);
        return TiledMapParser.FromFile(Path.Combine(dir, "map.tmj")).Result!;
    }

    public static (TiledMapData? Result, string? Error) ParseMapJson(string mapJson)
    {
        var dir = NewDir();
        WriteStandardTileset(dir);
        File.WriteAllText(Path.Combine(dir, "map.tmj"), mapJson);
        return TiledMapParser.FromFile(Path.Combine(dir, "map.tmj"));
    }
}
